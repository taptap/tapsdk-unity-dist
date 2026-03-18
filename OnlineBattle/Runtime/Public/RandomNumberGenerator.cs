using TapSDK.Core;
using TapSDK.Core.Internal.Log;

namespace TapSDK.OnlineBattle
{
    /// <summary>
    /// 随机数生成器
    /// </summary>
    public class RandomNumberGenerator
    {
        // c++ 层对象句柄
        private long nativeObjId = 0;

        private readonly object _lockObj = new object();

        /// <summary>
        /// 使用种子创建生成器
        /// </summary>
        /// <param name="seed"> 种子值，使用 BattleStartNotification 里返回的 seed </param>
        public RandomNumberGenerator(int seed)
        {
            nativeObjId = TapOnlineBattleWrapper.TapSdkOnlineBattleNewRandomNumberGenerator(seed);
            TapLog.Log("RandomNumberGenerator createObj " + nativeObjId);
        }

        /// <summary>
        /// 生成0 ~ 0x7fffffff之间的随机整数
        /// </summary>
        /// <returns>0 ~ 0x7fffffff之间的随机整数</returns>
        public int RandomInt()
        {
            lock (_lockObj)
            {
                if (nativeObjId == 0)
                {
                    throw new TapException(
                        TapOnlineBattleConstant.ERROR_INVALID_REQUEST,
                        "generator has freed"
                    );
                }
                return TapOnlineBattleWrapper.TapSdkOnlineBattleRandomInt(nativeObjId);
            }
        }

        /// <summary>
        /// 销毁随机数生成器对象，释放资源。当不再需要使用该随机数生成器时（比如对战结束后），必须调用此函数释放资源。
        /// 只需调用一次此函数
        /// </summary>
        public void Free()
        {
            lock (_lockObj)
            {
                if (nativeObjId != 0)
                {
                    TapOnlineBattleWrapper.TapSdkOnlineBattleFreeRandomNumberGenerator(nativeObjId);
                    nativeObjId = 0;
                }
            }
        }
    }
}
