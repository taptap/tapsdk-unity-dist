using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TapSDK.UI;
using TapSDK.Core;
using TapSDK.Login;
using TapSDK.Login.Internal;
using TapSDK.Login.Standalone;
using UnityEditor;
using TapSDK.Compliance.Internal;
using TapSDK.Compliance.Model;
using TapSDK.Core.Internal;
using UnityEngine;
using System.Linq;
using TapSDK.Compliance.Standalone.Internal;
using TapSDK.Core.Standalone.Internal.Http;


namespace TapSDK.Compliance
{
    public sealed class ComplianceWorker : BaseComplianceWorker
    {
        #region Abstract Override



        private TaptapComplianceIDInputController idInputPanel;

        private static bool showPopupInVerification;
        internal const string SCOPE_COMPLIANCE = "compliance"; 
        internal const string SCOPE_COMPLIANCE_BASIC = "compliance_basic"; 

        /// <summary>
        /// 激活手动认证
        /// </summary>
        protected override async Task VerifyManuallyAsync()
        {
            do 
            {
                try 
                {
                    // 这里处理了拿 Verification 信息时,5xx服务器错误或者网络错误的情况,策略为不停的弹出重试弹窗
                    var result = await OpenVerificationPanelCn();
                    // await Verification.Save(userId, Region.China, result);
                    if (Verification.IsVerifyFailed) {
                        // TODO@luran:本地化
                        UIManager.Instance.OpenToast("认证未通过，请重新提交实名信息", UIManager.GeneralToastLevel.Error);
                        continue;
                    }

                    break;
                } 
                catch (TaskCanceledException) 
                {
                    TapLogger.Debug(string.Format("[TapTap: ChinaCompliance] Close manual verification panel."));
                    throw;
                } 
                catch (Exception e) 
                {
                    TapLogger.Error(string.Format("[TapTap: ChinaCompliance] manual verification exception! {0}", e.ToString()));
                   
                    if (e is HttpRequestException || e is WebException)
                    {
                        UIManager.Instance.OpenToast(TapTapComplianceManager.LocalizationItems.Current.NetError, UIManager.GeneralToastLevel.Error);
                        continue;
                    }

                    if (e is ComplianceException aae)
                    {
                        if (aae.Description != null && aae.Description.Length > 0)
                        {
                            UIManager.Instance.OpenToast(aae.message, UIManager.GeneralToastLevel.Error);
                            continue;
                        }
                        if (aae.Code >= (int)HttpStatusCode.InternalServerError)
                        {
                            // TODO@luran:本地化
                            UIManager.Instance.OpenToast("请求出错", UIManager.GeneralToastLevel.Error);
                            continue;
                        }
                    }
                    if (e.Message.Contains("Interval server error."))
                    {
                        // TODO@luran:本地化
                        UIManager.Instance.OpenToast("请求出错", UIManager.GeneralToastLevel.Error);
                        continue;
                    }
                    UIManager.Instance.OpenToast(TapTapComplianceManager.LocalizationItems.Current.NoVerification, UIManager.GeneralToastLevel.Error);
                    await Task.Yield();
                    
                }
            } 
            while (true);
        }
        
        protected override PlayableResult CheckOfflineMinorPlayable()
        {
            // 未成年人
            if (IsGameTime()) 
            {
                // 可玩：节假日并且是游戏时间
                HealthReminderDesc playableTip = Config.GetMinorPlayableHealthReminderTip();

                // 计算时间
                DateTimeOffset gameEndTime = Config.StrictEndTime;
                DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetCurrentTime()).ToOffset(DateTimeOffset.Now.Offset);
                TimeSpan remain = gameEndTime.TimeOfDay - now.TimeOfDay;

                return new PlayableResult 
                {
                    Title = playableTip?.tipTitle,
                    Content = playableTip?.tipDescription,
                    RemainTime = Convert.ToInt32(remain.TotalSeconds)
                };
            }

            // 不可玩：未成年人不在可玩时间
            HealthReminderDesc unplayableTip = Config.GetMinorUnplayableHealthReminderTip();
            return new PlayableResult 
            {
                Title = unplayableTip?.tipTitle,
                Content = unplayableTip?.tipDescription,
                RemainTime = 0
            };
        }
        
        protected override async Task<int> InternalStartup(string userId) {
            try {
                UIManager.Instance.CloseLoading();
                return await GetVerificationResult(userId);
            }
            catch (Exception e) {
                TapLogger.Error("[TapTap: ChinaCompliance] " + e.ToString());
                return StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR;
            }
        
        }
        
        #endregion
        
        private static bool HaveComplianceScope(AccessToken token) {
            if (token == null ) return false;
            bool useAgeRange = TapTapComplianceManager.config.useAgeRange;
            if(useAgeRange){
               return token.scopeSet != null && token.scopeSet.Contains(SCOPE_COMPLIANCE);
            }else{
               return token.scopeSet != null && 
               (token.scopeSet.Contains(SCOPE_COMPLIANCE) || token.scopeSet.Contains(SCOPE_COMPLIANCE_BASIC));
            }
        }
        
        // private static async Task<ComplianceCodeData> GetComplianceCode(string clientId, AccessToken token) {
        //     string url = TapTapSdk.CurrentRegion.ApiHost() + "/account/compliance/v1?client_id=" + clientId;
        //     var uri = new Uri(url);
        //     var sign = GetMacToken(token, uri);
        //     var headers = new Dictionary<string, object> {
        //         { "Authorization", sign }
        //     };
        //     ComplianceCodeResponse response = await LoginService.HttpClient.Get<ComplianceCodeResponse>(url, headers: headers);
        //     return response.Data;
        // }
        
        // private static string GetMacToken(AccessToken token, Uri uri, int timestamp = 0) {
        //     var ts = timestamp;
        //     if (ts == 0) {
        //         var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
        //         ts = (int)dt.TotalSeconds;
        //     }
        //     var sign = "MAC " + LoginService.GetAuthorizationHeader(token.kid,
        //         token.macKey,
        //         token.macAlgorithm,
        //         "GET",
        //         uri.PathAndQuery,
        //         uri.Host,
        //         "443", ts);
        //     return sign;
        // }
        
        /// <summary>
        /// 使用 TapToken 获取实名信息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accessToken"></param>
        /// <returns>0-正常;1-走手动;-1-打断流程</returns>
        private static async Task<int> FetchByTapToken(string userId, AccessToken accessToken) {
            UIManager.Instance.OpenLoading();
            var tcs = new TaskCompletionSource<int>();
            // get verification
            try {
                VerificationResult result = await Verification.FetchVerificationByTapToken(userId,accessToken);
                UIManager.Instance.CloseLoading();
                if(result.Status != null && !result.IsVerifyFailed){
                    tcs.TrySetResult(0);
                }else{
                    tcs.TrySetResult(-1);
                }
            }
            catch (Exception e) {
                TapLogger.Error(e.ToString());
                UIManager.Instance.CloseLoading();
                var aae = e as TapException;
                if (aae != null && aae.code >= (int)HttpStatusCode.InternalServerError && aae.code < 600) {
                    tcs.TrySetResult(1);
                    TapLogger.Warn($"[Compliance] 通过 code 拿去实名信息网络错误,将打断认证流程");
                }
                else {
                    TapLogger.Warn($"[Compliance] 通过 code 拿去实名信息失败,将启动手动认证");
                    tcs.TrySetResult(1);
                }
            }
            UIManager.Instance.CloseLoading();
            return tcs.Task.Result;
        }
        
        
        /// <summary>
        /// 是否在允许游戏时间段
        /// </summary>
        private bool IsGameTime()
        {
            DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetCurrentTime()).ToOffset(DateTimeOffset.Now.Offset);
            string currentDate = now.ToString( "yyyy-MM-dd" );
            List<string> holidays = TapTapComplianceManager.CurrentUserAntiResult.localConfig.timeRangeConfig.holidays;
            TapLogger.Debug("current date = " + currentDate + " holidays = " + holidays.ToArray());
            if(holidays.Contains(currentDate)){
                DateTimeOffset strictStart = Config.StrictStartTime;
                DateTimeOffset strictEnd = Config.StrictEndTime;
                bool playable;
                TapLogger.Debug(" now = " + now.TimeOfDay + " ,start = " + strictStart.TimeOfDay + ", end = " + strictEnd.TimeOfDay);
                playable = now.TimeOfDay >= strictStart.TimeOfDay && now.TimeOfDay < strictEnd.TimeOfDay;
                TapLogger.Debug("check result = " + playable);
                return playable;
            }
            return false;
        }

        private long GetCurrentTime(){
            long serverTime = TapHttpTime.GetCurrentServerTime();
            TapLogger.Debug("current serverTime = " + serverTime + " localTime = " + GetCurrentLocalTime());
            if ( serverTime > 0){
                return serverTime;
            }
            return GetCurrentLocalTime();
        }

        /// <summary>
        /// 打开中国实名制窗口
        /// </summary>
        /// <returns></returns>
        private Task<VerificationResult> OpenVerificationPanelCn()
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            var path = ComplianceConst.GetPrefabPath(ComplianceConst.ID_NUMBER_INPUT_PANEL_NAME,
                false);
            idInputPanel =
                UIManager.Instance.OpenUI<TaptapComplianceIDInputController>(path);
            idInputPanel.activeManualVerification = true;;
            if (idInputPanel != null)
            {
                idInputPanel.OnVerified = (verification) => tcs.TrySetResult(verification);
                idInputPanel.OnException = (e) =>
                {
                    if (e is HttpRequestException || e is WebException)
                    {
                        tcs.TrySetException(e);
                    }
                    else
                    {
                        if (e is ComplianceException aae)
                        {
                                tcs.TrySetException(e);
                        }
                        else
                        {
                            if (e.Message.Contains("Interval server error."))
                                tcs.TrySetException(e);
                            else {
                                UIManager.Instance.OpenToast("身份证号码错误", UIManager.GeneralToastLevel.Error);
                            }
                        }
                    }
                };
                idInputPanel.OnClosed = () => tcs.TrySetCanceled();
            }

            return tcs.Task;
        }
        
        private async Task<int> GetVerificationResult(string userId) {
            var tcs = new TaskCompletionSource<int>();
            int mannualVerify = 0;
            TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
            AccessToken accessToken =  tapAccount?.accessToken;
            bool isTapUser = accessToken != null;
            TapLogger.Debug(" Verification current = " + Verification.Current );            
            //0 不使用手动验证;1 主动使用手动认证; 2 保底触发(被动)使用手动验证
            try {
                accessToken = await GetAccessToken(accessToken);
                if (accessToken != null) {
                    TapLogger.Debug("[TapTap: ChinaCompliance] 获得登录Access Token!Token结果: {0}\n是否包括Compliance: {1}",
                        accessToken.ToJson(), HaveComplianceScope(accessToken));
                    if (HaveComplianceScope(accessToken)) {
                        // 0-正常;1-异常;-1-实名失败
                        var fetchResult = await FetchByTapToken(userId, accessToken);
                        if (fetchResult != 0 ) {
                            /// 异常问题
                            if (fetchResult == 1)
                            {
                                UIManager.Instance.OpenToast("授权异常", UIManager.GeneralToastLevel.Error);
                            }
                            mannualVerify = 2;
                        }
                    }
                    else {
                        //TODO@luran:本地化
                        UIManager.Instance.OpenToast("授权错误", UIManager.GeneralToastLevel.Error);
                        mannualVerify = 2;
                    }
                }
                else {
                    TapLogger.Debug("[TapTap: ChinaCompliance] 不能登录Access Token!直接降级为手动认证");
                    mannualVerify = 2;
                }
            }
            catch (AggregateException aggregateException) {
                var cancelException = aggregateException.InnerException as TaskCanceledException;
                if (cancelException != null) {
                    TapLogger.Debug("[TapTap: ChinaCompliance] 获得登录Access Token 中主动退出! 触发 AggregateException.TaskCanceledException");
                    tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                    return tcs.Task.Result;
                }
            }
            catch (TaskCanceledException) {
                TapLogger.Debug("[TapTap: ChinaCompliance] 获得登录Access Token 中主动退出! 触发 TaskCanceledException");
                tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                return tcs.Task.Result;
            }

            catch (ComplianceException aae) {
                TapLogger.Debug(
                    $"[TapTap: ChinaCompliance] 获取快速实名制信息出错! code: {aae.Code} error: {aae.Error} errorMsg: {aae.Description}");
                //TapToken过期->重新授权实名流程
                if (aae.message.ToLower().Contains("refuse quick verify")) {
                    mannualVerify = 1;
                }
                else {
                    mannualVerify = 2;
                }
            }
            catch (Exception e) {
                TapLogger.Debug(string.Format("[TapTap: ChinaCompliance] 获得登录Access Token 碰到错误,降级为手动验证! 错误信息: {0}", e.ToString()));
                mannualVerify = 2;
            }
            
                
            // 手动认证
            if (mannualVerify != 0) {
                try {
                    await VerifyManuallyAsync();
                }
                catch (TaskCanceledException) {
                    TapLogger.Debug("[TapTap: ChinaCompliance] 手动实名制过程中主动退出! 触发 TaskCanceledException");
                    tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                    return tcs.Task.Result;
                }
            }
            return Verification.IsVerified ? 0 :  await ShowVerifingTip();
        }
        
        
        /// <summary>
        /// 获取 Token,分为几种情况:1)不是TapUser的话,需要去展示包含手动的授权界面;2)是TapUser,这时需要去展示仅包含 Tap 的授权界面;
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private static async Task<AccessToken> GetAccessToken(AccessToken accessToken) {
            showPopupInVerification = false;
            bool haveCompliance = false;
            var isTapUser = IsTapUser(accessToken);
            if (isTapUser) {
                haveCompliance = HaveComplianceScope(accessToken);
            }
            TapLogger.Debug($"[TapTap: ChinaCompliance] 启动快速实名制. 是否为Tap用户: {isTapUser} 是否包含Complaince: {haveCompliance}");
            return await InternalGetAccessToken(isTapUser, haveCompliance, accessToken);
        }

        private static async Task<AccessToken> InternalGetAccessToken(bool isTapUser, bool haveCompliance, AccessToken accessToken) {
            var tcs = new TaskCompletionSource<AccessToken>();
            Func<Task<AccessToken>> getToken = null;
            // 先弹出快速认证提示,再弹出授权页面
            showPopupInVerification = true;
            bool useAgeRange = TapTapComplianceManager.config.useAgeRange;
            string permission = useAgeRange ? SCOPE_COMPLIANCE : SCOPE_COMPLIANCE_BASIC;
            bool justShowConfirmBtn = isTapUser && !haveCompliance|| !Config.Current.useManual;
            var goQuickVerify = await ShowQuickVerifyTipWindow(justShowConfirmBtn);
            TapLogger.Debug($"[TapTap: ChinaCompliance] 因为不是Tap用户启动快速认证提示弹窗!弹窗结果: {goQuickVerify}");
            // 同意授权
            if (goQuickVerify) {
                getToken = () =>
                {
                    //https://xindong.slack.com/archives/C0271SPG77A/p1701161783480859?thread_ts=1700206625.065689&cid=C0271SPG77A
                    // TapLogin.Authorize(new string[] { permission});
                    IAuthorizationProvider provider = PlatformTypeUtils.CreatePlatformImplementationObject(typeof(IAuthorizationProvider),
                "TapSDK.Login") as IAuthorizationProvider;
                    return provider?.Authorize(new string[] { permission});
                };
            }
            //不同意授权
            else {
                TapLogger.Debug("[TapTap: ChinaCompliance] 用户拒绝快速认证!");
            }
            
            // 不同意快速认证: 需要降级为手动
            if (getToken == null) {
                var aae = new ComplianceException(-1, "refuse quick verify");
                throw aae;
            }
            else {
                try {
                    var tToken = await getToken();
                    tcs.TrySetResult(tToken);
                }
                catch (TapException te) {
                    if (te.Code == (int)TapErrorCode.ERROR_CODE_LOGIN_CANCEL && te.Message.Contains("Login Cancel")) {
                        UIManager.Instance.OpenToast("授权取消", UIManager.GeneralToastLevel.Warning);
                        return await InternalGetAccessToken(isTapUser, haveCompliance, accessToken);
                    }
                }
                catch (Exception e) {
                    TapLogger.Error(e.ToString());
                    throw;
                }
            }
            
            return tcs.Task.Result;
        }
        
        /// <summary>
        /// 显示快速认证提示弹窗
        /// </summary>
        /// <param name="justQuickVerify">是否只包括确认快速认证按钮,如果否的话,还包含不启动快速认证按钮</param>
        /// <returns>返回 bool 里表示玩家是否选择快速认证</returns>
        private static Task<bool> ShowQuickVerifyTipWindow(bool justShowConfirmBtn) {
            var tcs = new TaskCompletionSource<bool>();
            var openParams = new TapTapComplianceQuickVerifyTipController.OpenParams()
            {
                justShowConfirmBtn = justShowConfirmBtn, 
                onClicked = (isQuickVerify) => tcs.TrySetResult(isQuickVerify),
                onClose = ()=> tcs.TrySetCanceled(),
            };
            var path = ComplianceConst.GetPrefabPath(ComplianceConst.QUICK_VERIFY_TIP_PANEL_NAME,
                false);
            UIManager.Instance
                .OpenUI<TapTapComplianceQuickVerifyTipController>(path, openParams);
            return tcs.Task;
        }
        
        private static bool IsTapUser(AccessToken accessToken) {
            return accessToken != null && !string.IsNullOrEmpty(accessToken.kid);
        }
    }
}