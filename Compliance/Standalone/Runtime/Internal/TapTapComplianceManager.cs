using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TapSDK.Compliance.Internal;
using TapSDK.Compliance.Localization;
using TapSDK.UI;
using TapSDK.Compliance.Model;
using TapSDK.Core;
using UnityEngine;
using TapSDK.Login;
using Network = TapSDK.Compliance.Internal.Network;

namespace TapSDK.Compliance 
{
    public class TapTapComplianceManager
    {
        public static string UserId;

        public static string ClientId { get; private set; }

        public static TapTapComplianceOption config;
        
        private static bool needResumePoll;

        public static TapTapComplianceOption ComplianceConfig => config == null ? config = new TapTapComplianceOption() : config;

        public static ComplianceLocalizationItems LocalizationItems
        {
            get
            {
                if (_ComplianceLocalizationItems == null)
                {
                    var textAsset = Resources.Load<TextAsset>(ComplianceLocalizationItems.PATH);
                    _ComplianceLocalizationItems = ComplianceLocalizationItems.FromJson(textAsset.text);
                }
                return _ComplianceLocalizationItems;
            }
        }
        private static ComplianceLocalizationItems _ComplianceLocalizationItems;
        public static bool CanPlay = false;

        private static PlayableResult _currentPlayableResult;
        internal static PlayableResult CurrentPlayableResult
        {
            get => _currentPlayableResult;
            set
            {
                if (CurrentRemainSeconds == null)
                {
                    CompliancePoll.StartCountdownRemainTime();
                }

                if (value != null)
                {
                    CurrentRemainSeconds = value.RemainTime;
                }
                _currentPlayableResult = value;
            }
        }

        private static UserComplianceConfigResult _currentUserAntiResult;
        public  static UserComplianceConfigResult CurrentUserAntiResult
        {
            get => _currentUserAntiResult;
            set
            {
                Config.userLocalConfig = value.localConfig;
                _currentUserAntiResult = value;
            }
        }

        internal static string CurrentSession = "";

        internal static int? CurrentRemainSeconds {get; set; }

        private static BaseComplianceWorker worker;

        /// <summary>
        /// 初始化, 建议使用这个接口,因为默认调用了 SetRegion,如果使用另一个 Init 接口,需要在 Init 之后,手动调用 SetRegion
        /// </summary>
        /// <param name="config"></param>
        public static void Init(string clientId, string clientToken, TapTapComplianceOption config) 
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            TapTapComplianceManager.config = config;
            if (string.IsNullOrEmpty(clientId)) 
            {
                throw new ArgumentNullException(nameof(clientId));
            }
            TapTapComplianceManager.ClientId = clientId;
            Network.SetGameInfo(clientId, clientToken);
            Network.InitSetting();
            var textAsset = Resources.Load<TextAsset>(ComplianceLocalizationItems.PATH);
            _ComplianceLocalizationItems = ComplianceLocalizationItems.FromJson(textAsset.text);
        }
        
        public static void SetTestEnvironment(bool enable) {
            Network.SetTestEnvironment(enable);
            if (enable)
                UIManager.Instance.OpenTip("当前处于防沉迷调试模式", Color.red, TextAnchor.UpperRight);
            else {
                UIManager.Instance.CloseTip();
            }
        }
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="userId">用户名</param>
        /// <returns></returns>
        public static async Task<int> StartUp(string userId) {
            if (string.IsNullOrEmpty(userId)) 
            {
                throw new ArgumentNullException(nameof(userId));
            }
            
            UserId = userId;
            UIManager.Instance.OpenLoading();
            InitWorker();
            bool isSuccess = await UpdateConfig(userId);
            if(isSuccess){
                return await Worker.StartUp(UserId);
            }else{
                UIManager.Instance.CloseLoading();
                return StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR;
            }
        }
        
        private static void InitWorker() {
            var worker = Worker;
            TapTapComplianceManager.worker = worker;
        }
        
        private static async Task<bool> UpdateConfig(string userId) {
            return await worker.FetchConfigAsync(userId);
        }

        /// <summary>
        /// 登出
        /// </summary>
        public static void Logout() 
        {
            CurrentRemainSeconds = null;
            CurrentPlayableResult = null;
            Worker.Logout();
        }

        internal static void ClearUserCache(){
            CurrentRemainSeconds = null;
            CurrentPlayableResult = null;
            Worker.Logout(false);
        }


        /// <summary>
        /// 检查支付
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static async Task<PayableResult> CheckPayLimit(long amount)
        {
            return await Worker.CheckPayableAsync(amount);
        }

        /// <summary>
        /// 上报支付
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static Task SubmitPayResult(long amount) 
        {
            return Worker.SubmitPayResult(amount);
        }
        
        /// <summary>
        /// 轮询时,检查可玩性
        /// </summary>
        /// <returns></returns>
        internal static async Task<PlayableResult> CheckPlayableOnPolling()
        {
            CurrentPlayableResult = await Worker.CheckPlayableOnPollingAsync();
            return CurrentPlayableResult;
        }
        
        public static void EnterGame()
        {
            if (CanPlay && needResumePoll)
            {
                TapLogger.Debug("enter game in antiAddiciton  " );
                CompliancePoll.StartUp();
                needResumePoll = false;
            }
        }

        public static void LeaveGame()
        {
            if (CanPlay && CompliancePoll.StartPoll)
            {
                TapLogger.Debug("leave game in antiAddiciton ");
                CompliancePoll.Logout();
                needResumePoll = true;
            }
        }

        /// <summary>
        /// 是否使用移动版 UI,否则就是用 Standalone 版本 UI
        /// </summary>
        /// <returns></returns>
        public static bool IsUseMobileUI()
        {
            return false;
        }

         /// 首次通过，弹防沉迷提示文案
        internal static void ShowAntiAddictionTip()
        {
            if (string.IsNullOrEmpty(UserId)){
                return;
            }
            var userIdEncode = "compliance_tip_" + Tool.EncryptString(UserId);
            var hasShowTip = !string.IsNullOrEmpty(DataStorage.LoadString(userIdEncode));
            if (!hasShowTip)
            {
                TapMessage.ShowMessage("已通过防沉迷校验，祝您游戏愉快！", position:TapMessage.Position.bottom, time:TapMessage.Time.threeSecond);
                DataStorage.SaveString(userIdEncode, "1");
            }
        }
        
        public static bool? useMobileUI;
        
        #region Worker
        
        private static BaseComplianceWorker currentWorker ;

        private static BaseComplianceWorker GetWorker()
        {
            if (currentWorker == null){
                currentWorker = GetChinaWorker();
            }
            return currentWorker;
            
        }

        private static BaseComplianceWorker Worker => GetWorker();

        private static BaseComplianceWorker GetChinaWorker() {
            // get ChinaComplianceWorker from TapSDK.Compliance.Standalone.Runtime dll
            Type baseWorkerType = typeof(BaseComplianceWorker);
            Type[] chinaWorkerType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK.Compliance.Standalone.Runtime"))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(clazz => baseWorkerType.IsAssignableFrom(clazz) && clazz.IsClass)
                .ToArray();
            if (chinaWorkerType != null && chinaWorkerType.Length > 0) {
                return Activator.CreateInstance(chinaWorkerType[0]) as BaseComplianceWorker;
            }

            return null;
        }

        #endregion
    }
}
