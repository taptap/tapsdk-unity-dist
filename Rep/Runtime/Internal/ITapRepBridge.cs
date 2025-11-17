using System;

namespace TapSDK.Rep.Internal {
    public interface ITapRepBridge {
        void Init();
        void Open(string openUrl, Action<int, string> callback);
    }
}
