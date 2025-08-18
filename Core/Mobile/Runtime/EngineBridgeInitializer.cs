using UnityEngine;
using TapSDK.Core.Internal;

namespace TapSDK.Core.Mobile
{
    public static class EngineBridgeInitializer
    {
        private static bool isInitialized = false;
        private const string SERVICE_NAME = "BridgeCoreService";

        public static void Initialize()
        {
            if (!isInitialized)
            {
                Debug.Log("Initializing EngineBridge");

                // TODO: android 注册桥接
                // #if UNITY_ANDROID
                EngineBridge.GetInstance().Register(
                    "com.taptap.sdk.core.unity.BridgeCoreService",
                    "com.taptap.sdk.core.unity.BridgeCoreServiceImpl");
                // #endif

                isInitialized = true;
            }
        }

        public static Command.Builder GetBridgeServer()
        {
            return new Command.Builder().Service(SERVICE_NAME);
        }
    }
}