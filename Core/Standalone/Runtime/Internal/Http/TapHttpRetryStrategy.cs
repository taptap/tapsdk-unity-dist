using System;
using System.Net;
using System.Threading;

namespace TapSDK.Core.Standalone.Internal.Http
{
    /// <summary>
    /// 重试策略接口。
    /// </summary>
    public interface ITapHttpRetryStrategy
    {
        /// <summary>
        /// 获取下次重试的时间（毫秒）。
        /// </summary>
        /// <param name="errorType">错误类型。</param>
        /// <param name="e">异常信息。</param>
        /// <returns>下次重试的时间（毫秒），如果不重试返回 -1。</returns>
        long NextRetryMillis(AbsTapHttpException e);
    }

    /// <summary>
    /// 后退策略接口。
    /// </summary>
    public interface ITapHttpBackoffStrategy
    {
        /// <summary>
        /// 获取下一个后退的时间（毫秒）。
        /// </summary>
        /// <returns>下一个后退的时间（毫秒）。</returns>
        long NextBackoffMillis();

        /// <summary>
        /// 判断是否可以重试无效时间。
        /// </summary>
        /// <returns>如果可以重试返回 true，否则返回 false。</returns>
        bool CanInvalidTimeRetry();

        /// <summary>
        /// 重置策略状态。
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// HTTP 重试策略实现。
    /// </summary>
    public class TapHttpRetryStrategy
    {
        /// <summary>
        /// 创建默认重试策略。
        /// </summary>
        /// <param name="backoffStrategy">后退策略。</param>
        /// <returns>默认重试策略。</returns>
        public static ITapHttpRetryStrategy CreateDefault(ITapHttpBackoffStrategy backoffStrategy)
        {
            return new Default(backoffStrategy);
        }

        /// <summary>
        /// 创建不重试策略。
        /// </summary>
        /// <returns>不重试策略。</returns>
        public static ITapHttpRetryStrategy CreateNone()
        {
            return new None();
        }

        private class None : ITapHttpRetryStrategy
        {
            public long NextRetryMillis(AbsTapHttpException e)
            {
                // 不重试返回 -1
                return -1L;
            }
        }

        private class Default : ITapHttpRetryStrategy
        {
            private readonly ITapHttpBackoffStrategy backoffStrategy;

            public Default(ITapHttpBackoffStrategy backoffStrategy)
            {
                this.backoffStrategy = backoffStrategy;
            }

            public long NextRetryMillis(AbsTapHttpException e)
            {
                long nextRetryMillis = -1L;
                if (e is TapHttpServerException se)
                {
                    // 处理服务器错误状态码
                    if (se.StatusCode >= HttpStatusCode.InternalServerError && se.StatusCode <= (HttpStatusCode)599)
                    {
                        nextRetryMillis = backoffStrategy.NextBackoffMillis();
                    }
                    else if (TapHttpErrorConstants.ERROR_INVALID_TIME.Equals(se.ErrorData.Error))
                    {
                        // 修复时间并判断是否可以重试。invalid_time 重试必须占用与其它重试
                        // 原因共享的同一份有限重试预算（跨端一致：最多 4 次尝试），不能绕过
                        // backoffStrategy 的次数上限单独再给一次机会，否则在预算已耗尽的
                        // 最后一次尝试上遇到 invalid_time 会多出一次总请求。
                        // NextBackoffMillis() 会消耗一次预算并在耗尽时返回 -1；这里只用它
                        // 来判断和消耗预算，实际等待时间仍然是 0（立马重试），不用它返回的
                        // 指数退避时间。
                        TapHttpTime.FixTime(se.TapHttpResponse.Now);
                        if (backoffStrategy.CanInvalidTimeRetry() && backoffStrategy.NextBackoffMillis() >= 0)
                        {
                            nextRetryMillis = 0L; // 立马重试
                        }
                    }
                    else if (TapHttpErrorConstants.ERROR_SERVER_ERROR.Equals(se.ErrorData.Error))
                    {
                        nextRetryMillis = backoffStrategy.NextBackoffMillis();
                    }
                }
                else if (e is TapHttpInvalidResponseException ie)
                {
                    if (ie.StatusCode >= HttpStatusCode.InternalServerError && ie.StatusCode <= (HttpStatusCode)599)
                    {
                        nextRetryMillis = backoffStrategy.NextBackoffMillis();
                    }
                }
                else if (e is TapHttpNetworkErrorException || e is TapHttpUnknownException)
                {
                    // 断网、DNS 解析失败、连接失败、超时等网络层错误，以及请求/解析过程中的
                    // 其它未分类异常，都属于"网络原因失败"，纳入同一套有限重试计数
                    // （跨端一致：最多 4 次尝试），而不是第一次失败就直接放弃重试。
                    nextRetryMillis = backoffStrategy.NextBackoffMillis();
                }
                return nextRetryMillis;
            }
        }
    }

    /// <summary>
    /// HTTP 后退策略实现。
    /// </summary>
    public class TapHttpBackoffStrategy
    {
        /// <summary>
        /// 创建固定后退策略。
        /// </summary>
        /// <param name="maxCount">最大重试次数。</param>
        /// <returns>固定后退策略。</returns>
        public static ITapHttpBackoffStrategy CreateFixed(int maxCount)
        {
            return new Fixed(maxCount);
        }

        /// <summary>
        /// 创建指数后退策略，不限制重试次数。
        /// </summary>
        /// <returns>指数后退策略。</returns>
        public static ITapHttpBackoffStrategy CreateExponential()
        {
            return new Exponential();
        }

        /// <summary>
        /// 创建指数后退策略，限制最大重试次数。
        ///
        /// 不直接给 <see cref="CreateExponential"/> 加一个可选参数：C# 的默认参数是编译期
        /// 特性，旧版预编译调用方仍会按无参签名调用，独立升级 tap-common/Core.Standalone
        /// 后会直接抛 MissingMethodException（Codex 审查发现）。用独立的新方法，
        /// <see cref="CreateExponential"/> 的签名完全不受影响。
        /// </summary>
        /// <param name="maxRetryCount">最大重试次数。</param>
        /// <returns>指数后退策略。</returns>
        public static ITapHttpBackoffStrategy CreateExponentialLimited(int maxRetryCount)
        {
            return new Exponential(maxRetryCount);
        }

        /// <summary>
        /// 创建不后退策略。
        /// </summary>
        /// <returns>不后退策略。</returns>
        public static ITapHttpBackoffStrategy CreateNone()
        {
            return new None();
        }

        private abstract class Base : ITapHttpBackoffStrategy
        {
            protected int CanTimeDeltaRetry = 1;

            public abstract long NextBackoffMillis();

            public abstract void Reset();

            /// <summary>
            /// 判断是否可以重试无效时间。
            /// </summary>
            /// <returns>如果可以重试返回 true，否则返回 false。</returns>
            public bool CanInvalidTimeRetry()
            {
                return Interlocked.CompareExchange(ref CanTimeDeltaRetry, 0, 1) == 1;
            }
        }

        private class Fixed : Base
        {
            private readonly int _maxCount;
            private int CurrentCount = 0;

            public Fixed(int maxCount)
            {
                _maxCount = maxCount;
            }

            public override long NextBackoffMillis()
            {
                if (++CurrentCount < _maxCount)
                {
                    return 100L; // 固定的重试时间 100ms
                }
                return -1L; // 达到最大重试次数，返回 -1
            }

            public override void Reset()
            {
                CurrentCount = 0;
                Interlocked.Exchange(ref CanTimeDeltaRetry, 1);
            }
        }

        private class Exponential : Base
        {
            private static readonly long INIT_INTERVAL_MILLIS = 2 * 1000L; // 初始时间 2 秒
            private static readonly long MAX_INTERVAL_MILLIS = 600 * 1000L; // 最大时间 600 秒
            private static readonly int MULTIPLIER = 2; // 指数倍数

            private readonly int? maxRetryCount;
            private int currentRetryCount = 0;
            private long CurrentIntervalMillis = INIT_INTERVAL_MILLIS;

            public Exponential(int? maxRetryCount = null)
            {
                this.maxRetryCount = maxRetryCount;
            }

            public override long NextBackoffMillis()
            {
                if (maxRetryCount != null && currentRetryCount >= maxRetryCount.Value)
                {
                    return -1L; // 已达到最大重试次数，不再重试
                }
                currentRetryCount++;
                // 先返回当前间隔（首次调用返回 INIT_INTERVAL_MILLIS），再为下一次调用增长间隔，
                // 避免"先增长再返回"导致首次实际等待时间比文档描述的初始值多一倍。
                long interval = CurrentIntervalMillis;
                if (CurrentIntervalMillis < MAX_INTERVAL_MILLIS)
                {
                    CurrentIntervalMillis = Math.Min(CurrentIntervalMillis * MULTIPLIER, MAX_INTERVAL_MILLIS);
                }
                return interval;
            }

            public override void Reset()
            {
                CurrentIntervalMillis = INIT_INTERVAL_MILLIS; // 重置当前时间
                currentRetryCount = 0;
                Interlocked.Exchange(ref CanTimeDeltaRetry, 1);
            }
        }

        private class None : Base
        {
            public override long NextBackoffMillis()
            {
                return -1L; // 不后退，返回 -1
            }

            public override void Reset()
            {
                Interlocked.Exchange(ref CanTimeDeltaRetry, 1);
            }
        }
    }
}
