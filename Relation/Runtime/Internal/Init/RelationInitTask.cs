using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Relation.Internal.Init {
    public class RelationInitTask : IInitTask {
        public int Order => 104;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapRelationManager.Instance.Init(coreOption.clientId, coreOption.region, coreOption.screenOrientation);
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapRelationManager.Instance.Init(coreOption.clientId, coreOption.region, coreOption.screenOrientation);
        }
    }
}