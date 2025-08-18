using TapSDK.Core;
using TapSDK.Core.Internal.Init;
using TapSDK.Update;

namespace TapSDK.Update.Internal.Init {
    public sealed class TapUpdateInitTask : IInitTask {
        public int Order => 14;

        public void Init(TapTapSdkOptions coreOption){
#if !UNITY_EDITOR && UNITY_ANDROID
            TapTapUpdate.Init(coreOption.clientId, coreOption.clientToken);
#endif

        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            TapTapUpdate.Init(coreOption.clientId, coreOption.clientToken);
#endif
        }

    
    }
}