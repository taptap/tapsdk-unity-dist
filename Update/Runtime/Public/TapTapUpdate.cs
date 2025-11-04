using TapSDK.Update.Internal;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core;
using System;

namespace TapSDK.Update {
    public class TapTapUpdate {

        public static readonly string Version = "4.8.4";
        
        static readonly ITapUpdateBridge update;

        static TapTapUpdate() {
            update = BridgeUtils.CreateBridgeImplementation(typeof(ITapUpdateBridge), "TapSDK.Update")
                as ITapUpdateBridge;
        }

        internal static void Init(string clientId, string clientToken)
        {
            update.Init(clientId, clientToken);
        }

        public static void UpdateGame(Action onCancel) {
            update.UpdateGame(onCancel);
        }

        public static void CheckForceUpdate() {
            update.CheckForceUpdate();
        }
    }
}
