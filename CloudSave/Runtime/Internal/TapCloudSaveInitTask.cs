using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.CloudSave.Internal.Init
{
    public sealed class TapCloudSaveInitTask : IInitTask
    {
        public int Order => 18;

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapCloudSaveInternal.Init(coreOption);
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapCloudSaveInternal.Init(coreOption);
        }
    }
}