using System;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Utils;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace TapSDK.Core.Mobile
{
    public class TapCoreMobile : ITapCorePlatform
    {
        private EngineBridge Bridge = EngineBridge.GetInstance();

        public TapCoreMobile()
        {
            Debug.Log("TapCoreMobile constructor");
            TapLoom.Initialize();
            EngineBridgeInitializer.Initialize();
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            Debug.Log("TapCoreMobile SDK inited");
            SetPlatformAndVersion(TapTapSDK.SDKPlatform, TapTapSDK.Version);
            string coreOptionsJson = JsonUtility.ToJson(coreOption);
            string[] otherOptionsJson = otherOptions.Select(option => JsonConvert.SerializeObject(option)).ToArray();
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("init")
                .Args("coreOption", coreOptionsJson)
                .Args("otherOptions", otherOptionsJson)
                .CommandBuilder());
        }

        private void SetPlatformAndVersion(string platform, string version)
        {
            Debug.Log("TapCoreMobile SetPlatformAndVersion called with platform: " + platform + " and version: " + version);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("setPlatformAndVersion")
                .Args("platform", TapTapSDK.SDKPlatform)
                .Args("version", TapTapSDK.Version)
                .CommandBuilder());
            SetSDKArtifact("Unity");
        }

        private void SetSDKArtifact(string value)
        {
            Debug.Log("TapCoreMobile SetSDKArtifact called with value: " + value);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("setSDKArtifact")
                .Args("artifact", "Unity")
                .CommandBuilder());
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            Init(coreOption, new TapTapSdkBaseOptions[0]);
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            Debug.Log("TapCoreMobile UpdateLanguage language: " + language);
            Bridge.CallHandler(EngineBridgeInitializer.GetBridgeServer()
                .Method("updateLanguage")
                .Args("language", (int)language)
                .CommandBuilder());
        }

        public Task<bool> IsLaunchedFromTapTapPC()
        {
            return Task.FromResult(false);
        }
    }
}