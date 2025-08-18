using System;

namespace TapSDK.Update.Internal {
    public interface ITapUpdateBridge {
        void Init(string clientId, string clientToken);
        void UpdateGame(Action onCancel);
        void CheckForceUpdate();
    }
}