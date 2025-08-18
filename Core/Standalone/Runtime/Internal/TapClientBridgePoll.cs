
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal
{
    
    internal class TapClientBridgePoll : MonoBehaviour 
    {
        static readonly string TAP_CLIENT_POLL_NAME = "TapClientBridgePoll";

        static TapClientBridgePoll current;

    
        internal static void StartUp() 
        {
            TapLogger.Debug("TapClientBridgePoll StartUp " );
            if (current == null) 
            {
                GameObject pollGo = new GameObject(TAP_CLIENT_POLL_NAME);
                DontDestroyOnLoad(pollGo);
                current = pollGo.AddComponent<TapClientBridgePoll>();
            }
        }
        
        
        private void Update()
        {
#if UNITY_STANDALONE_WIN
           TapClientBridge.TapSDK_RunCallbacks();
#endif           
        }
    }

}
