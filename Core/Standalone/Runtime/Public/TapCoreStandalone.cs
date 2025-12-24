using TapSDK.Core.Internal;
using UnityEngine;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Openlog;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Bean;
using System.Threading.Tasks;
using System;
using System.Threading;
using TapSDK.UI;
using System.Runtime.InteropServices;

namespace TapSDK.Core.Standalone
{
    /// <summary>
    /// Represents the standalone implementation of the TapCore SDK.
    /// </summary>
    public class TapCoreStandalone : ITapCorePlatform
    {
        internal static Prefs Prefs;
        internal static User User;
        internal static TapTapSdkOptions coreOptions;

        // client 信息是否匹配
        internal static bool isClientInfoMatched = true;

        internal static TapGatekeeper gatekeeperData = new TapGatekeeper();

        private readonly TapHttp tapHttp = TapHttp.NewBuilder("TapSDKCore", TapTapSDK.Version).Build();

        /// <summary>
        /// Initializes a new instance of the <see cref="TapCoreStandalone"/> class.
        /// </summary>
        public TapCoreStandalone()
        {
            // Instantiate modules
            User = new User();
            TapLoom.Initialize();
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified options.
        /// </summary>
        /// <param name="options">The TapCore SDK options.</param>
        public void Init(TapTapSdkOptions options)
        {
            Init(options, null);
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified core options and additional options.
        /// </summary>
        /// <param name="coreOption">The TapCore SDK core options.</param>
        /// <param name="otherOptions">Additional TapCore SDK options.</param>
        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            if (coreOption.clientId == null || coreOption.clientId.Length == 0)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientId 不能为空", "clientId 不能为空");
                return;
            }
            if (coreOption.clientToken == null || coreOption.clientToken.Length == 0)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientToken 不能为空", "clientToken 不能为空");
                return;
            }

            TapLog.Log("SDK Init Options : ", "coreOption : " + JsonConvert.SerializeObject(coreOption) + "\notherOptions : " + JsonConvert.SerializeObject(otherOptions));
            coreOptions = coreOption;
            // 设置区域与语言
            TapLocalizeManager.SetCurrentRegion(coreOption.region == TapTapRegionType.CN);
            TapLocalizeManager.SetCurrentLanguage(coreOption.preferredLanguage);
            if (Prefs == null)
            {
                Prefs = new Prefs();
            }
            TapOpenlogStandalone.Init();

            var path = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + coreOption.clientId + ".json");
            // 兼容旧版文件
            if (!File.Exists(path))
            {
                var oldPath = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + ".json");
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, path);
                }
            }
            if (File.Exists(path))
            {
                var clientSettings = File.ReadAllText(path);
                // TapLog.Log("本地 clientSettings: " + clientSettings);
                try
                {
                    TapGatekeeper tapGatekeeper = JsonConvert.DeserializeObject<TapGatekeeper>(clientSettings);
                    if (tapGatekeeper.Switch?.Heartbeat == true)
                    {
                        TapAppDurationStandalone.Enable();
                    }
                    else
                    {
                        TapAppDurationStandalone.Disable();
                    }
                    gatekeeperData = tapGatekeeper;
                }
                catch (System.Exception e)
                {
                    TapLog.Warning("TriggerEvent error: " + e.Message);
                }
            }

            requestClientSetting();
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            if (coreOptions == null)
            {
                TapLog.Log("coreOptions is null");
                return;
            }
            TapLog.Log("UpdateLanguage called with language: " + language);
            coreOptions.preferredLanguage = language;
            TapLocalizeManager.SetCurrentLanguage(language);
        }

        public static string getGatekeeperConfigUrl(string key)
        {
            if (gatekeeperData != null)
            {
                var urlsData = gatekeeperData.Urls;
                if (urlsData != null && urlsData.ContainsKey(key))
                {
                    var keyData = urlsData[key];
                    if (keyData != null)
                    {
                        return keyData.Browser;
                    }
                }
            }
            return null;
        }

        private void requestClientSetting()
        {
            // 使用 httpclient 请求 /sdk-core/v1/gatekeeper 获取配置
#if UNITY_EDITOR
            var bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
            var bundleIdentifier = Application.identifier;
#endif
            var path = "sdk-core/v1/gatekeeper";
            var body = new Dictionary<string, object> {
                { "platform", "pc" },
                { "bundle_id", bundleIdentifier }
            };

            tapHttp.PostJson<TapGatekeeper>(
               url: path,
               json: body,
               onSuccess: (data) =>
               {
                   if (data.Switch?.Heartbeat == true)
                   {
                       TapAppDurationStandalone.Enable();
                   }
                   else
                   {
                       TapAppDurationStandalone.Disable();
                   }
                   gatekeeperData = data;
                   // 把 data 存储在本地
                   saveClientSettings(data);
                   // 发通知
                   EventManager.TriggerEvent(Constants.ClientSettingsEventKey, data);
               },
               onFailure: (error) =>
               {
                   if (error is TapHttpServerException se)
                   {
                       if (TapHttpErrorConstants.ERROR_INVALID_CLIENT.Equals(se.ErrorData.Error))
                       {
                           isClientInfoMatched = false;
                           TapLog.Error("Init Failed", se.ErrorData.ErrorDescription);
                           TapMessage.ShowMessage(se.ErrorData.Msg, TapMessage.Position.bottom, TapMessage.Time.twoSecond);
                       }
                   }
               }
           );
        }

        private void saveClientSettings(TapGatekeeper settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + TapTapSDK.taptapSdkOptions.clientId + ".json"), json);
        }


        public static bool CheckInitState()
        {
            // 未初始化
            if (coreOptions == null || coreOptions.clientId == null || coreOptions.clientId.Length == 0
            || coreOptions.clientToken == null || coreOptions.clientToken.Length == 0)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("当前应用还未初始化", "当前应用还未初始化: 请在调用 SDK 业务接口前，先调用 TapTapSDK.Init  接口");
                return false;
            }
            // 应用信息不匹配
            if (isClientInfoMatched == false)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("当前应用初始化信息错误", "当前应用初始化信息错误: 请在 TapTap 开发者中心检查当前应用调用初始化接口设置的 clientId 、clientToken 是否匹配");
                return false;
            }
            return true;
        }

        // 获取当前用户设置的 DB userID
        public static string GetCurrentUserId()
        {
            return User?.Id;
        }


        // <summary>
        // 校验游戏是否通过启动器唤起，建立与启动器通讯
        //</summary>
        public async Task<bool> IsLaunchedFromTapTapPC()
        {
#if UNITY_STANDALONE_WIN
            return await TapClientStandalone.IsLaunchedFromTapTapPC();
#else
            throw new System.NotImplementedException();
#endif
        }

        public void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties
        )
        {
            TapOpenlogStandalone.LogBusiness(project, version, action, properties);
        }
 }


    public interface IOpenIDProvider
    {
        string GetOpenID();
    }
}
