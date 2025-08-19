using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TapSDK.UI;
using TapSDK.Compliance.Internal;
using TapSDK.Compliance.Model;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;
using TapSDK.Login;
using UnityEngine;
using Network = TapSDK.Compliance.Internal.Network;
using Random = System.Random;
using System.Text;

namespace TapSDK.Compliance
{
    public abstract class BaseComplianceWorker {
        protected TapTapComplianceOption config => TapTapComplianceManager.ComplianceConfig;
        
        #region Abstract
        
        static readonly string USER_ANTI_FILENAME = "user_anti_config";

        // 本地基准时间戳
        private long BaseLocalTime = 0;
        //获取本地时间戳时，应用运行的时间
        private long BaseLocalStartUpTime = 0;
        
        
        /// <summary>
        /// 激活手动认证
        /// </summary>
        protected abstract Task VerifyManuallyAsync();
        
        /// <summary>
        /// 检查未成年人可玩性
        /// </summary>
        /// <returns></returns>
        protected abstract PlayableResult CheckOfflineMinorPlayable();
        

        /// <summary>
        /// 直接通过 Tap 账号获取实名制信息
        /// </summary>
        /// <returns></returns>
        protected abstract Task<int> InternalStartup(string userId);

        #endregion

        #region Virutal
        
        /// <summary>
        /// 检查可玩性后,已知成年人时的处理
        /// </summary>
        /// </summary>
        /// <param name="playable"></param>
        protected virtual void OnCheckedPlayableWithAdult(PlayableResult playable)
        {
            TryStartPoll();
        }
        
        /// <summary>
        /// 检查可玩性后,已知未成年人时的处理
        /// </summary>
        /// <param name="playable"></param>
        /// <returns></returns>
        protected virtual async Task<int> OnCheckedPlayableWithMinorAsync(PlayableResult playable)
        {
            if (playable.RemainTime > 0)
            {
                TapTapComplianceManager.ShowAntiAddictionTip();
            }
            var tcs = new TaskCompletionSource<int>();
            Action onOk;
            if (playable.RemainTime > 0) {
                onOk = () => {
                    tcs.TrySetResult(StartUpResult.LOGIN_SUCCESS);
                    TryStartPoll();
                };
            } 
            else {
                tcs.TrySetResult(StartUpResult.PERIOD_RESTRICT);
                onOk = () => {
                    Application.Quit();
                };
            }
            Action onSwitchAccount = null;
            if (config.showSwitchAccount && playable.RemainTime <= 0) {
                onSwitchAccount = () => {
                    Logout(false);
                    TapTapCompliance.OnInvokeExternalCallback(StartUpResult.SWITCH_ACCOUNT, null);
                };
            }
            
            TapComplianceUI.OpenHealthReminderPanel(playable, onOk, onSwitchAccount);
            return await tcs.Task;
        }
        
        /// <summary>
        /// 检查可玩性,如果出现异常就会用本地的计算方式
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<PlayableResult> CheckPlayableAsyncWithFallback()
        {
            try 
            {
                var playableResult = await CheckPlayableAsync();
                return playableResult;
            } 
            catch (ComplianceException e) 
            {
                if (e.IsTokenExpired() 
                    || e.Code < 500 || !TapTapComplianceManager.CurrentUserAntiResult.policy.active.Equals(ComplianceConst.POLICY_ACTIVE_TIME_RANGE))
                {
                    throw;
                }
                return CheckOfflinePlayable();
            }
            catch (Exception e) 
            {
                TapLogger.Error(e);
                // 单机判断是否可玩
                return CheckOfflinePlayable();
            }
        }
        /// <summary>
        /// 获取用户配置，失败时，会尝试获取本地配置，如果都无效，抛出异常
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<UserComplianceConfigResult> CheckUserConfigAsyncWithFallback()
        {
            string filename = Tool.EncryptString(Verification.Current.UserId);
            Persistence persistence = new Persistence(Path.Combine(
                USER_ANTI_FILENAME,
                filename));
            UserComplianceConfigResult result = await persistence.Load<UserComplianceConfigResult>();
            try 
            {
                UserComplianceConfigResult serverResult =  await Network.CheckUserConfig();
                if(serverResult != null){
                    await persistence.Save(serverResult);
                    TapTapComplianceManager.CurrentSession = GenerateSession();
                }
                return serverResult;
            } 
            catch (ComplianceException e) 
            {
                if (e.IsTokenExpired() || e.Code < 500)
                {
                   throw;
                }else{
                    if(result != null){
                        ResetBaseLocalTime();
                        TapTapComplianceManager.CurrentSession = GenerateSession();
                        return result; 
                    }else{
                        throw;
                    }
                }
            }
            catch (Exception e) 
            {
                TapLogger.Error(e);
                if(result == null){
                    throw;
                }else{
                    ResetBaseLocalTime();
                    TapTapComplianceManager.CurrentSession = GenerateSession();
                    return result;
                }
            }
            
        }

        private void ResetBaseLocalTime(){
            if(BaseLocalTime > 0){
                return;
            }
            BaseLocalTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            BaseLocalStartUpTime = (long)Time.realtimeSinceStartup;
            TapLogger.Debug(" current local time " + DateTimeOffset.Now);
        }

        /// <summary>
        /// 获取基于本地时间的当前时间戳，用于离线心跳计算
        /// </summary>
        /// <returns></returns>
        protected long GetCurrentLocalTime(){
            long currentStartUpTime = (long) Time.realtimeSinceStartup;
            return BaseLocalTime + currentStartUpTime - BaseLocalStartUpTime;
        }

         private string GenerateSession()
        {
            const string baseStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var builder = new StringBuilder();
            int l = baseStr.Length;
            for (var i = 0; i < 32; i++)
            {
                var number = random.Next(l);
                builder.Append(baseStr[number]);
            }

            return builder.ToString();
        }

    
        /// <summary>
        /// 检查可玩性
        /// </summary>
        /// <param name="shouldThrowException">当内部发生错误的时候,是否抛出异常.默认不抛出异常,就会按照本地规则计算playable</param>
        /// <returns></returns>
        protected virtual async Task<PlayableResult> CheckPlayableAsync()
        {
            try 
            {
                var playableResult = await Network.CheckPlayable();
                return playableResult;
            }
            catch (Exception e) 
            {
                TapLogger.Error(e);
                throw;
            }
        }
        
        /// <summary>
        /// 本地判断可玩性
        /// </summary>
        /// <returns></returns>
        protected virtual PlayableResult CheckOfflinePlayable()
        {
            TapLogger.Debug("CheckOfflinePlayable");
            // 成年人
            if (Verification.IsAdult) 
            {
                // 可玩
                return new PlayableResult 
                {
                    RemainTime = 9999
                };
            }
            
            return CheckOfflineMinorPlayable();
        }
        
        /// <summary>
        /// 是否需要开启轮询检查可玩性
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsNeedStartPoll()
        {
            return !Verification.IsAdult || Config.NeedUploadUserAction;
        }
        
        /// <summary>
        /// 轮询时,检查可玩性判断
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PlayableResult> CheckPlayableOnPollingAsync()
        {
            try 
            {
                var playable = await CheckPlayableAsyncWithFallback();

                if (playable.RemainTime <= 0)
                {
                    OnUnplayablePostPoll(playable);
                }

                return playable;
            } 
            catch (Exception e) 
            {
                if (e is ComplianceException aae && aae.IsTokenExpired())
                {
                    Logout();
                    TapTapCompliance.OnInvokeExternalCallback(StartUpResult.EXITED, null);
                    return new PlayableResult { RemainTime = 0 };
                }
                else
                {
                    CompliancePoll.Logout();
                    TapTapCompliance.OnInvokeExternalCallback(StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR, null);
                    return new PlayableResult { RemainTime = 0 };
                }

            }
        }
        
        /// <summary>
        /// 轮询时,发现不可玩处理
        /// </summary>
        /// <param name="playable"></param>
        protected virtual void OnUnplayablePostPoll(PlayableResult playable)
        {
            
            Action onExitGame = Application.Quit;
            Action onSwitch = null;
            if (config.showSwitchAccount && playable.RemainTime <= 0) 
            {
                onSwitch = () => {
                    Logout(false);
                    TapTapCompliance.OnInvokeExternalCallback(StartUpResult.SWITCH_ACCOUNT, null);
                };
            }
            TapTapCompliance.OnInvokeExternalCallback(StartUpResult.PERIOD_RESTRICT, null);
            TapComplianceUI.OpenHealthReminderPanel(playable, onExitGame, onSwitch);
        }
        
        /// <summary>
        /// 通过检查,发现不可支付时的处理
        /// </summary>
        /// <param name="payable"></param>
        protected virtual void OnCheckedUnpayable(PayableResult payable)
        {
            //当服务端返回提示文案无效时，不显示
            if(payable.Title != null && payable.Title.Length > 0 &&
                payable.Content != null && payable.Content.Length > 0){
                TapComplianceUI.OpenHealthPaymentPanel(payable);
            }
        }
        
        /// <summary>
        /// 登出处理
        /// </summary>
        public virtual void Logout(bool needClearCache = true)
        {
            if(needClearCache){
                string filename = Tool.EncryptString(Verification.Current.UserId);
                Persistence persistence = new Persistence(Path.Combine(
                                USER_ANTI_FILENAME,
                                filename));
                persistence.Delete();
            }
                        
            Verification.Logout(needClearCache);
            CompliancePoll.Logout();
            TapTapComplianceManager.UserId = null;
        }
        
        #endregion
        
        #region Internal

        public async Task<int> StartUp(string userId) {
            await FetchVerificationAsync(userId);
            if (Verification.Current == null ||  Verification.IsVerifyFailed)
            {
                TapLogger.Debug("try get token internal by UI");
                var result = await InternalStartup(userId);
                // 目前只会返回 9002 和 0-认证成功
                // 9002 是 StartUpResult.REAL_NAME_STOP
                if (result != 0)
                {
                    return result;
                }
            }
            else if (Verification.IsVerifing)
            {
                return await ShowVerifingTip();
            }
            else
            {
                UIManager.Instance.CloseLoading();
            }
            return await OnVerificationFetched();
        }

        /// <summary>
        /// 显示实名中提示
        /// </summary>
        internal async Task<int> ShowVerifingTip()
        {
            var task = new TaskCompletionSource<int>();
            var tip = Config.GetInputIdentifyBlockingTip();
            Action onOk = () =>
            {
                TapLogger.Debug("[TapTap: ChinaCompliance] 认证中,实名取消!");
                task.TrySetResult(StartUpResult.REAL_NAME_STOP);
            };
            TapComplianceUI.OpenHealthPaymentPanel(tip.Title, tip.Content, tip.PositiveButtonText, onOk);
            return await task.Task;
        }
        /// <summary>
        /// 获得实名信息
        /// </summary>
        public async Task FetchVerificationAsync(string userId)
        {
            // 拉取服务端实名信息
            try
            {
                await Verification.Fetch(userId);
                UIManager.Instance.CloseLoading();
            }
            catch (Exception e)
            {
                TapLogger.Error(e);
                UIManager.Instance.CloseLoading();
                //所有错误跳过，执行实名
            }
        }
        
        /// <summary>
        /// 获得实名信息后的处理
        /// </summary>
        /// <returns></returns>
        public async Task<int> OnVerificationFetched() {
            return await ValidateUserConfigAsync();
        }

        private async Task<int> ValidateUserConfigAsync(){
            do {
                try{
                    UserComplianceConfigResult result = await CheckUserConfigAsyncWithFallback();
                    TapTapComplianceManager.CurrentUserAntiResult = result;
                    await Verification.setAgeState(result.userState.ageLimit, result.userState.isAdult);
                    // 适龄限制判断
                    if (!result.ageCheckResult.allow) {
                        return StartUpResult.AGE_LIMIT;
                    }
                    // 检查时长
                    else{
                        return await ValidatePlayableAsync();
                    }
                }
                catch (Exception e){
                    if (e is ComplianceException aae && aae.IsTokenExpired())
                    {
                        //用户登出
                        Logout();
                        return StartUpResult.EXITED;
                    }
                    else
                    {
                        return StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR;
                    }
                }
            }while (true);
        }
        
        /// <summary>
        /// 检查可玩性
        /// </summary>
        /// <returns></returns>
        private async Task<int> ValidatePlayableAsync()
        {
            int tryCount = 0;
            do {
                try{
                    tryCount++;
                    PlayableResult playable = await CheckPlayableAsyncWithFallback();
                    TapTapComplianceManager.CurrentPlayableResult = playable;
                    // 2.1  成年人-后处理
                    if (Verification.Current.CheckIsAdult) {
                        OnCheckedPlayableWithAdult(playable);
                        return StartUpResult.LOGIN_SUCCESS;
                    }
                    // 2.2  未成年人-后处理
                    else{
                        return await OnCheckedPlayableWithMinorAsync(playable);
                    }
                }
                catch (Exception e){
                    Debug.Log("ValidatePlayableAsync ERROR = " + e.Message + " stack = " + e.StackTrace);
                    if (e is ComplianceException aae && aae.IsTokenExpired())
                    {
                        //用户登出
                        Logout();
                        return StartUpResult.EXITED;
                    }else{
                        return StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR;
                    }
                    
                }
            }while (true);
        }
        
        /// <summary>
        /// 尝试开启轮询检查
        /// </summary>
        protected void TryStartPoll()
        {
            TapLogger.Debug("TryStartPoll ");
            if (IsNeedStartPoll())
            {
                TapLogger.Debug("TryStartPoll interval = " + TapTapComplianceManager.CurrentUserAntiResult.policy.heartbeatInterval);
                CompliancePoll.StartUp(TapTapComplianceManager.CurrentUserAntiResult.policy.heartbeatInterval);
            }
        }
        
        /// <summary>
        /// 检查是否可以支付
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async virtual Task<PayableResult> CheckPayableAsync(long amount)
        {
            PayableResult payable = await Network.CheckPayable(amount);
            if (!payable.Status)
            {
                OnCheckedUnpayable(payable);
            }
            return payable;
        }
        
        /// <summary>
        /// 提交充值结果
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual Task SubmitPayResult(long amount)
        {
            return Network.SubmitPayment(amount);
        }
        
        /// <summary>
        /// 获取配置
        /// </summary>
        public async Task<bool> FetchConfigAsync(string userId)
        {
            return await Config.Fetch(userId);
        }

        protected void ShowVerifiedToast() {
            Texture avatar = TapSDK.UI.UIManager.TapTapToastIcon;
            string str = "您已在 TapTap 实名！";
            TapSDK.UI.UIManager.Instance.OpenToast(false, str, 3, icon: avatar);
        }
        
        #endregion
    }
}