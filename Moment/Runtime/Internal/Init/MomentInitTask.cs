using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Moment.Internal.Init {
    public class MomentInitTask : IInitTask {
        public int Order => 103;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapMomentManager.Instance.Init(coreOption.clientId, coreOption.region);
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapMomentManager.Instance.Init(coreOption.clientId, coreOption.region);
        }
    }
}