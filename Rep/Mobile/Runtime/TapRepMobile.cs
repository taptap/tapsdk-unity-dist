using System;
using System.Collections.Generic;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Rep.Internal;

namespace TapSDK.Rep.Mobile {
    public class TapRepMobile : ITapRepBridge {
        public static string TAP_REP_SERVICE = "BridgeRepService";

        public static string TDS_REP_SERVICE_CLZ = "com.taptap.sdk.rep.enginebridge.BridgeRepService";

        public static string TDS_REP_SERVICE_IMPL = "com.taptap.sdk.rep.enginebridge.BridgeRepServiceImpl";

        public TapRepMobile() {
            EngineBridge.GetInstance().Register(TDS_REP_SERVICE_CLZ, TDS_REP_SERVICE_IMPL);
        }

        public void Init() {
            TapLog.Log("TapRep Mobile Bridge Initialized");
        }

        public void Open(string openUrl, Action<int, string> callback) {
            TapLog.Log($"TapRep::Open called with openUrl: {openUrl}");

            var command = new Command.Builder()
                .Service(TAP_REP_SERVICE)
                .Method("open")
                .Args("openUrl", openUrl)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();

            EngineBridge.GetInstance().CallHandler(command, (result) => {
                TapLog.Log($"TapRep::Open result code: {result.code}, content: {result.content}");

                try {
                    if (result.code == Result.RESULT_SUCCESS) {
                        // Parse the native result JSON
                        // Expected format: {"code": 0, "message": null} or {"code": errorCode, "message": "error"}
                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("code")) {
                            int code = SafeDictionary.GetValue<int>(dic, "code", -1);
                            string message = SafeDictionary.GetValue<string>(dic, "message", null);

                            callback?.Invoke(code, message);
                        } else {
                            // Fallback: if we can't parse, but bridge succeeded, treat as success
                            callback?.Invoke(0, null);
                        }
                    } else {
                        // Bridge call failed
                        callback?.Invoke(result.code, result.content);
                    }
                } catch (Exception e) {
                    TapLog.Error($"TapRep::Open parse result error: {e.Message}");
                    callback?.Invoke(-1, e.Message);
                }
            });
        }
    }
}
