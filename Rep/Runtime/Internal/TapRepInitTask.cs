using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Rep.Internal.Init
{
    public sealed class TapRepInitTask : IInitTask
    {
        public int Order => 18;

        public void Init(TapTapSdkOptions coreOption)
        {
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
        }
    }
}
