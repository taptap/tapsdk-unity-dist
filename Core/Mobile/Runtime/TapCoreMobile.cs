using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using UnityEngine;

namespace TapSDK.Core.Mobile
{
    public class TapCoreMobile : ITapCorePlatform
    {
        private EngineBridge Bridge = EngineBridge.GetInstance();

        public TapCoreMobile()
        {
            TapLog.Log("TapCoreMobile constructor");
            TapLoom.Initialize();
            EngineBridgeInitializer.Initialize();
            // 由于当通过 Application.Quit 退出时，iOS 端不会收到 applicationWillTerminate 的通知，
            // 所以不会调用 C++ 的 OnAppStop 方法，导致小概率会因 C++ 资源未正确释放触发崩溃，所以添加监听
#if UNITY_IOS
            EventManager.AddListener(
                EventManager.OnApplicationQuit,
                (quit) =>
                {
                    TapLog.Log("TapSDK Unity OnApplicationQuit");
                    Bridge.CallHandler(
                        EngineBridgeInitializer
                            .GetBridgeServer()
                            .Method("handleEngineQuitEvent")
                            .CommandBuilder()
                    );
                }
            );
#endif
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapLog.Log("TapCoreMobile SDK inited");
            SetPlatformAndVersion(TapTapSDK.SDKPlatform, TapTapSDK.Version);
            string coreOptionsJson = JsonUtility.ToJson(coreOption);
            string[] otherOptionsJson = otherOptions
                .Select(option => JsonConvert.SerializeObject(option))
                .ToArray();
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("init")
                    .Args("coreOption", coreOptionsJson)
                    .Args("otherOptions", otherOptionsJson)
                    .CommandBuilder()
            );
        }

        private void SetPlatformAndVersion(string platform, string version)
        {
            TapLog.Log(
                "TapCoreMobile SetPlatformAndVersion called with platform: "
                    + platform
                    + " and version: "
                    + version
            );
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("setPlatformAndVersion")
                    .Args("platform", TapTapSDK.SDKPlatform)
                    .Args("version", TapTapSDK.Version)
                    .CommandBuilder()
            );
            SetSDKArtifact("Unity");
        }

        private void SetSDKArtifact(string value)
        {
            TapLog.Log("TapCoreMobile SetSDKArtifact called with value: " + value);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("setSDKArtifact")
                    .Args("artifact", "Unity")
                    .CommandBuilder()
            );
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            Init(coreOption, new TapTapSdkBaseOptions[0]);
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            TapLog.Log("TapCoreMobile UpdateLanguage language: " + language);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("updateLanguage")
                    .Args("language", (int)language)
                    .CommandBuilder()
            );
        }

        public Task<bool> IsLaunchedFromTapTapPC()
        {
            return Task.FromResult(false);
        }

        public void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties
        )
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            string propertiesJson = JsonConvert.SerializeObject(properties);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("sendOpenLog")
                    .Args("project", project)
                    .Args("version", version)
                    .Args("action", action)
                    .Args("properties", propertiesJson)
                    .CommandBuilder()
            );
        }
    }
}
