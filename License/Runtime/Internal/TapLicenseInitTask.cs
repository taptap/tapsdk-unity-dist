using TapSDK.Core;
using TapSDK.Core.Internal.Init;

#if PLATFORM_STANDALONE_WIN 
namespace TapSDK.License.Internal
{
    internal class TapLicenseInitTask : IInitTask
    {
        public int Order => 13;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapLicense.Init();
        }

        public void Init(TapTapSdkOptions coreOption)
        {
           TapTapLicense.Init();
        }
    }
}
#endif