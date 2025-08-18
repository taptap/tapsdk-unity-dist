using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using TapSDK.Compliance.Model;
using TapSDK.Core;
using TapSDK.Login;

namespace TapSDK.Compliance.Internal 
{
    public static class Verification
    {
        internal const int AGE_LIMIT_UNKNOWN = -1;
        internal const int AGE_LIMIT_CHILD = 0;
        internal const int AGE_LIMIT_TEEN = 8;
        internal const int AGE_LIMIT_YOUNG = 16;
        internal const int AGE_LIMIT_ADULT = 18;
        // 已实名，但未同意年龄段信息授权且未成年
        internal const int UNKNOWN_AGE = 100;
        // 已实名，但未同意年龄段信息授权且成年
        internal const int UNKNOWN_AGE_ADULT = 110;

        static readonly string VERIFICATION_FILENAME = "verification";

        static readonly string VERIFICATION_V2_FILENAME = "verification_v2";
        static readonly bool DEFAULT_VERIFIED = false;
        static readonly bool DEFAULT_VERIFING = false;
        static readonly bool DEFAULT_ADULT = false;

        static LocalVerification current;

        public static LocalVerification Current => current;

        static Persistence persistence;

        static Persistence persistenceV2;


        /// <summary>
        /// 快速获取身份信息
        /// </summary>
        /// <param name="userId"></param>
        public static async Task Fetch(string userId)
        {
            if (current != null && !current.UserId.Equals(userId))
            {
                Logout(false);
            }
            string filename = Tool.EncryptString(userId);
            // 存储 v2 token 数据   
            if (persistenceV2 == null)
            {
                persistenceV2 = new Persistence(Path.Combine(
                    VERIFICATION_V2_FILENAME,
                    filename));
            }
            VerificationResult result;
            try
            {
                LocalVerification localVerification;
                TapLogger.Debug("start check v2 token in local");
                //先检查本地是否有 v2 缓存
                localVerification = await persistenceV2.Load<LocalVerification>();
                if (localVerification != null && localVerification.ComplianceToken != null
                    && localVerification.ComplianceToken.Length > 0)
                {
                    current = localVerification;
                    return;
                }
                TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                AccessToken accessToken = tapAccount?.accessToken;
                if (HasComplianceAuthInTapToken(accessToken))
                {
                    await FetchByTapToken(userId);
                }
                else
                {
                    TapLogger.Debug("try get v2 token with userId");
                    result = await Network.FetchVerification(userId);
                    await Save(userId, result);
                }

            }
            catch (Exception e)
            {
                //首次异常报错
                TapLogger.Debug("try get v2 token failed  error: " + e.Message);
                TapLogger.Error(e.ToString());
                throw;
            }
        }

        internal static async Task FetchByTapToken(string userId, AccessToken accessToken = null)
        {
            TapLogger.Debug("start check tapToken in local");
            long timestamp = 0;
            int retryTimes = 1;
            VerificationResult result;
            while (true)
            {
                if (accessToken == null)
                {
                    ///判断本地是否有包含 compliance 的 token
                    TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                    accessToken = tapAccount?.accessToken;
                }
                try
                {
                    if (HasComplianceAuthInTapToken(accessToken))
                    {
                        TapLogger.Debug("try use tapToken to get v2 token");
                        result = await Network.FetchVerificationByTapToken(userId, accessToken, timestamp);
                        await Save(userId, result);
                        return;
                    }
                }
                catch (Exception e)
                {
                    //时间戳异常
                    if (e is ComplianceException aee && aee.Error != null
                        && aee.Error.Equals(ComplianceConst.SERVER_ERROR_TYPE_INVALID_TIME) && retryTimes > 0)
                    {
                        timestamp = aee.Now;
                        retryTimes--;
                        continue;
                    }
                    else
                    {
                        throw e;
                    }
                }
                throw new Exception();
            }

        }

        internal static bool HasComplianceAuthInTapToken(AccessToken accessToken)
        {
            if (accessToken == null)
            {
                return false;
            }
            bool useAgeRange = TapTapComplianceManager.config.useAgeRange;
            if (useAgeRange)
            {
                return accessToken.scopeSet != null && accessToken.scopeSet.Contains(ComplianceWorker.SCOPE_COMPLIANCE);
            }
            else
            {
                return accessToken.scopeSet != null &&
                (accessToken.scopeSet.Contains(ComplianceWorker.SCOPE_COMPLIANCE) || accessToken.scopeSet.Contains(ComplianceWorker.SCOPE_COMPLIANCE_BASIC));
            }
        }


        /// <summary>
        /// 手动输入身份信息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName">身份证姓名</param>
        /// <param name="idCard">身份证号</param>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerificationManual(string userId, string userName, string idCard)
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            try
            {
                VerificationResult result = await Network.FetchVerificationManual(userName, idCard);
                tcs.SetResult(result);
                await Save(userId, result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                TapLogger.Error(e.ToString());
                throw;
            }

            return await tcs.Task;
        }

        /// <summary>
        /// TapToken 获取实名
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="token">TapToken</param>
        /// <returns></returns>
        public static async Task<VerificationResult> FetchVerificationByTapToken(string userId, AccessToken token)
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            try
            {
                await FetchByTapToken(userId, token);
                if (persistenceV2 == null)
                {
                    var filename = Tool.EncryptString(userId);
                    persistenceV2 = new Persistence(Path.Combine(
                    VERIFICATION_V2_FILENAME,
                    filename));
                }
                VerificationResult result = await persistenceV2.Load<VerificationResult>();
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                TapLogger.Error(e.ToString());
                throw;
            }

            return await tcs.Task;
        }

        public static async Task Save(string userId, VerificationResult verification)
        {
            current = new LocalVerification(verification)
            {
                UserId = userId
            };
            TapLogger.Debug("try save  v2 token to local");
            if (persistenceV2 == null)
            {
                var filename = Tool.EncryptString(userId);
                persistenceV2 = new Persistence(Path.Combine(
                VERIFICATION_V2_FILENAME,
                filename));
            }
            await persistenceV2.Save(current);
        }

        public static async Task setAgeState(int ageLimit, bool isAdult)
        {
            if (current != null)
            {
                current.AgeLimit = ageLimit < 0 ?
                (isAdult ? UNKNOWN_AGE_ADULT : UNKNOWN_AGE) : ageLimit;
                current.IsAdult = isAdult;
                if (persistenceV2 == null)
                {
                    var filename = Tool.EncryptString(current.UserId);
                    persistenceV2 = new Persistence(Path.Combine(
                    VERIFICATION_V2_FILENAME,
                    filename));
                }
                await persistenceV2.Save(current);
            }
        }

        public static void ClearCacheVerfiction()
        {
            persistence?.Delete();
            persistenceV2?.Delete();
        }

        internal static void Logout(bool needClearCache = true)
        {
            // if (IsVerified && !IsAdult)
            // {
            // #pragma warning disable CS4014
            //                 Network.CheckPlayable();
            // #pragma warning restore CS4014
            // }
            if (needClearCache)
            {
                ClearCacheVerfiction();
            }
            persistence = null;
            persistenceV2 = null;
            current = null;
        }

        internal static string GetCurrentToken()
        {
            return current?.ComplianceToken;
        }

        /// <summary>
        /// 是否已认证
        /// </summary>
        public static bool IsVerified => current?.IsVerified ?? DEFAULT_VERIFIED;

        /// <summary>
        /// 是否在认证中
        /// </summary>
        public static bool IsVerifing => current?.IsVerifing ?? DEFAULT_VERIFING;

        /// <summary>
        /// 是否认证失败
        /// </summary>
        public static bool IsVerifyFailed => current?.IsVerifyFailed ?? DEFAULT_VERIFING;

        /// <summary>
        /// 是否是成年人
        /// </summary>
        internal static bool IsAdult => current?.IsAdult ?? DEFAULT_ADULT;

        /// <summary>
        /// 年龄级别
        /// </summary>
        internal static int AgeLimit => current?.AgeLimit ?? AGE_LIMIT_CHILD;
        
    }
}
