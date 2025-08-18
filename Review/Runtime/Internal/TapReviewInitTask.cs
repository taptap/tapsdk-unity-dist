using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Review.Internal.Init
{
    public sealed class TapReviewInitTask : IInitTask
    {
        public int Order => 17;

        public void Init(TapTapSdkOptions coreOption)
        {
        }

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
        }
    }
}