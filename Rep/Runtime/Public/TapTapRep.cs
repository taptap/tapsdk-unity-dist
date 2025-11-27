using System;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;
using TapSDK.Rep.Internal;

namespace TapSDK.Rep {
    /// <summary>
    /// TapTapRep 错误码
    /// </summary>
    public static class TapRepError {
        /// <summary>参数错误</summary>
        public const int PARAM_ERROR = 100002;
        /// <summary>客户端配置错误</summary>
        public const int CLIENT_ERROR = 100003;
        /// <summary>未知错误</summary>
        public const int UNKNOWN_ERROR = 100004;
    }

    /// <summary>
    /// TapTapRep 主类
    /// </summary>
    public class TapTapRep {
        public static readonly string Version = "4.9.1";

        static readonly ITapRepBridge repBridge;

        static TapTapRep() {
            repBridge = BridgeUtils.CreateBridgeImplementation(typeof(ITapRepBridge), "TapSDK.Rep")
                as ITapRepBridge;
            repBridge?.Init();
        }

        /// <summary>
        /// 打开指定URL
        /// </summary>
        /// <param name="openUrl">要打开的URL地址</param>
        /// <param name="callback">回调，errorCode: 0表示成功，其他值表示失败；errorMessage: 错误信息</param>
        public static void Open(string openUrl, Action<int, string> callback) {
            if (string.IsNullOrEmpty(openUrl)) {
                callback?.Invoke(TapRepError.PARAM_ERROR, "openUrl cannot be null or empty");
                return;
            }

            repBridge?.Open(openUrl, callback);
        }
    }
}
