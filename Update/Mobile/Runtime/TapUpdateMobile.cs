using System.Collections.Generic;
using TapSDK.Core;
using System;
using TapSDK.Update.Internal;

namespace TapSDK.Update.Mobile {
    public class TapUpdateMobile : ITapUpdateBridge {
        public static string TAP_UPDATE_SERVICE = "BridgeUpdateService";

        public static string TDS_UPDATE_SERVICE_CLZ = "com.taptap.sdk.update.enginebridge.BridgeUpdateService";

        public static string TDS_UPDATE_SERVICE_IMPL = "com.taptap.sdk.update.enginebridge.BridgeUpdateServiceImpl";
        
        public TapUpdateMobile() {
            EngineBridge.GetInstance().Register(TDS_UPDATE_SERVICE_CLZ, TDS_UPDATE_SERVICE_IMPL);
        }

        public void Init(string clientId, string clientToken) {}
        
        public void UpdateGame(Action onCancel) {
            #if UNITY_ANDROID
            var command = new Command.Builder()
                .Service(TAP_UPDATE_SERVICE)
                .Method("updateGame")
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, (result) => {
                UnityEngine.Debug.LogFormat("TapUpdate::UpdateGame result:{0}", result.ToJSON());
                if (result.code == Result.RESULT_SUCCESS && result.content.ToLower().Contains("cancel")) {
                    onCancel?.Invoke();
                    return;
                }
            });
            #else
            throw new NotImplementedException("TapUpdate::UpdateGame Only Support On Android");
            #endif
        }

        public void CheckForceUpdate() {
            #if UNITY_ANDROID
            var command = new Command.Builder()
                .Service(TAP_UPDATE_SERVICE)
                .Method("checkForceUpdate")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
            #else
            throw new NotImplementedException("TapUpdate::CheckUpdate Only Support On Android");
            #endif
        }
    }
}