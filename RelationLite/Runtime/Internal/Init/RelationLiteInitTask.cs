using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.RelationLite.Internal.Init 
{
    public class RelationLiteInitTask : IInitTask 
    {
        public int Order => 105;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapRelationLiteManager.Instance.Init(coreOption.clientId, coreOption.region);
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapRelationLiteManager.Instance.Init(coreOption.clientId, coreOption.region);
        }
    }
} 