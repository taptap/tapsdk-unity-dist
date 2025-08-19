using System;
using System.Threading.Tasks;
using TapSDK.Login;
using TapSDK.Compliance.Internal;
using TapSDK.Compliance.Model;
using System.Collections.Generic;
using TapSDK.Compliance.Standalone.Internal;
using TapSDK.Core;
using TapSDK.Core.Standalone;
using TapSDK.Core.Internal.Utils;
using Newtonsoft.Json;
using TapSDK.UI;

namespace TapSDK.Compliance
{
    public sealed class ComplianceNewJob : IComplianceJob
    {
        internal bool UseAgeRange = true;

        internal TapTapRegionType currentRegionType = TapTapRegionType.CN;

         // 是否正在处理用户信息，当调用 startup 接口后设置为 true， 当通知游戏回调时设置为 false
        internal volatile bool isCheckingUser = false;
        private List<Action<int, string>> _externalCallbackList;

        public List<Action<int, string>> ExternalCallbackList
        {
            get => _externalCallbackList;
        }
        
        public Task<int> GetAgeRange()
        {
            if(!CheckInitState()){
                var defaultTcs = new TaskCompletionSource<int>();
                defaultTcs.TrySetResult(-1);
                return defaultTcs.Task;
            }
            var tcs = new TaskCompletionSource<int>();
            if (!Verification.IsVerified || !UseAgeRange){
                tcs.TrySetResult(-1);
            } 
            if(Verification.AgeLimit < Verification.UNKNOWN_AGE){
                tcs.TrySetResult(Verification.AgeLimit);
            }else{
                tcs.TrySetResult(-1);
            }
            return tcs.Task;
            
        }

        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        public Task<int> GetRemainingTime()
        {
             if(!CheckInitState()){
                var defaultTcs = new TaskCompletionSource<int>();
                defaultTcs.TrySetResult(0);
                return defaultTcs.Task;
            }
            int time = 0;
            if (TapTapComplianceManager.CurrentRemainSeconds == null){
                time = 0;
            }else{
                if (Verification.IsAdult){
                    time = 9999;
                }else{
                    time =  TapTapComplianceManager.CurrentRemainSeconds.Value;
                }
            }
            var tcs = new TaskCompletionSource<int>();
            tcs.TrySetResult(time);
            return tcs.Task;
            
        }


        
        public Task<string> GetCurrentToken()
        {
            if(!CheckInitState()){
                var defaultTcs = new TaskCompletionSource<string>();
                defaultTcs.TrySetResult("");
                return defaultTcs.Task;
            }
            var tcs = new TaskCompletionSource<string>();
            if (!Verification.IsVerified){
                tcs.TrySetResult("");
            } else{
                tcs.TrySetResult(Verification.GetCurrentToken());
            }
            return tcs.Task;
        }
        
        public void Init(string clientId, string clientToken, TapTapRegionType reginType, TapTapComplianceOption config) {
            UseAgeRange = config.useAgeRange;
            currentRegionType = reginType;
            TapTapComplianceManager.Init(clientId, clientToken, config);
            TapComplianceTracker.Instance.TrackInit();
        }

        public void RegisterComplianceCallback(Action<int, string> callback){
            if(!CheckInitState()){
                return;
            }
            if(_externalCallbackList == null){
                _externalCallbackList = new List<Action<int, string>>();
            }
            if(!_externalCallbackList.Contains(callback)){
                _externalCallbackList.Add(callback);
            }
        }
        
        public async void Startup(string userId)
        {
            if(!CheckInitState()){
                return;
            }
            if (string.IsNullOrEmpty(userId)) 
            {
                TapLogger.Error(" current user is invalid:" + userId);
                return;
            }
            // 如果正在处理中，直接返回
            if (isCheckingUser) {
                TapLogger.Debug(" current user is checking so return");
                return;
            }
            isCheckingUser = true;
            string sessionId = Guid.NewGuid().ToString();
            TapComplianceTracker.Instance.TrackStart("startup", sessionId);
            if(TapTapComplianceManager.UserId != null){
                TapTapComplianceManager.ClearUserCache();
            }
            var code = await TapTapComplianceManager.StartUp(userId);
            switch(code){ 
                case StartUpResult.LOGIN_SUCCESS:
                case StartUpResult.PERIOD_RESTRICT:
                case StartUpResult.DURATION_LIMIT:
                case StartUpResult.AGE_LIMIT:
                    TapComplianceTracker.Instance.TrackSuccess("startup", sessionId);
                    break;
                case StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR:
                    TapComplianceTracker.Instance.TrackFailure("startup", sessionId, code, "invalid client or network error");
                    break;
                case StartUpResult.EXITED:
                case StartUpResult.SWITCH_ACCOUNT:
                    break;
                case StartUpResult.REAL_NAME_STOP:
                    TapComplianceTracker.Instance.TrackCancel("startup", sessionId);
                    break;
            }
            OnInvokeExternalCallback(code,null);
        }
        

        // ReSharper disable Unity.PerformanceAnalysis

        public void Exit()
        {
            if(!CheckInitState()){
                return;
            }
            TapTapComplianceManager.Logout();
            OnInvokeExternalCallback(StartUpResult.EXITED, null);
        }


        public async void CheckPaymentLimit(long amount, Action<CheckPayResult> handleCheckPayLimit, Action<string> handleCheckPayLimitException)
        {
            if(!CheckInitState()){
                return;
            }
            try
            {
                var payResult = await TapTapComplianceManager.CheckPayLimit(amount);
                handleCheckPayLimit?.Invoke(new CheckPayResult()
                {
                    // status 为 1 时可以支付
                    status = payResult.Status ? 1 : 0,
                    title = payResult.Title,
                    description = payResult.Content
                });
            }
            catch (Exception e)
            {
                handleCheckPayLimitException?.Invoke(e.Message);
                if(e is ComplianceException aee && aee.IsTokenExpired()){
                    Exit();
                }
            }
        }

        public async void SubmitPayment(long amount, Action handleSubmitPayResult, Action<string> handleSubmitPayResultException)
        {
            if(!CheckInitState()){
                return;
            }
            try
            {
                await TapTapComplianceManager.SubmitPayResult(amount);
                handleSubmitPayResult?.Invoke();
            }
            catch (Exception e)
            {
                handleSubmitPayResultException?.Invoke(e.Message);
                if(e is ComplianceException aee && aee.IsTokenExpired()){
                    Exit();
                }
            }
        }

        
        public void SetTestEnvironment(bool enable) {
            if(!CheckInitState()){
                return;
            }
            TapTapComplianceManager.SetTestEnvironment(enable);
        }

        public void OnInvokeExternalCallback(int code, string msg)
        {
            switch (code)
            {
                case StartUpResult.LOGIN_SUCCESS:
                    TapTapComplianceManager.CanPlay = true;
                    break;
                case StartUpResult.AGE_LIMIT:
                case StartUpResult.PERIOD_RESTRICT:
                case StartUpResult.DURATION_LIMIT:
                case StartUpResult.EXITED:
                case StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR:
                case StartUpResult.SWITCH_ACCOUNT:
                    TapTapComplianceManager.CanPlay = false;
                    break;
            }
            if (code == StartUpResult.LOGIN_SUCCESS // 通过校验
                || code == StartUpResult.REAL_NAME_STOP // 取消校验
                || code == StartUpResult.EXITED // 登出用户
                || code == StartUpResult.AGE_LIMIT // 年龄限制
                || code == StartUpResult.SWITCH_ACCOUNT // 切换账号
                || code == StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR)
            { // 网络异常
              // 用户结束校验流程
                isCheckingUser = false;
            }
            // 在 openlog 中设置当前用户信息
            if (code == StartUpResult.LOGIN_SUCCESS || code == StartUpResult.EXITED || code == StartUpResult.SWITCH_ACCOUNT)
            {
                string userIdentifier = "";
                string userSessionId = "";
                if (code == StartUpResult.LOGIN_SUCCESS)
                {
                    userIdentifier = TapTapComplianceManager.UserId ?? "";
                    userSessionId = TapTapComplianceManager.CurrentSession ?? "";
                }
                Dictionary<string, string> userData = new Dictionary<string, string>()
                {
                    ["anti_addict_user_identifier"] = userIdentifier,
                    ["anti_addict_session_id"] = userSessionId
                };
                string userInfo = JsonConvert.SerializeObject(userData);
                EventManager.TriggerEvent(EventManager.OnComplianceUserChanged, userInfo);
            }

            if (StartUpResult.Contains(code))
            {
                if (_externalCallbackList != null)
                {
                    foreach (Action<int, string> callback in _externalCallbackList)
                    {
                        callback?.Invoke(code, msg);
                    }
                }
            }

            if (code == StartUpResult.LOGIN_SUCCESS)
            {
                TapTapComplianceManager.ShowAntiAddictionTip();
            }
        }

         /// <summary>
        /// 校验初始化参数及区域
        /// </summary>
        /// <returns>是否校验通过</returns>
        private bool CheckInitState()
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return false;
            }
            if (currentRegionType == TapTapRegionType.Overseas)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("海外不支持使用合规认证服务", "海外不支持使用合规认证服务");
                return false;
            }
            return true;
        }

    }
}
