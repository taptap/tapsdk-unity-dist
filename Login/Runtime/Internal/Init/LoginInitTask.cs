using System;
using System.Linq;
using TapSDK.Core;
using TapSDK.Core.Internal.Init;

namespace TapSDK.Login.Internal.Init {
    public class LoginInitTask : IInitTask {
        public int Order => 11;

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            TapTapLoginManager.Instance.Init(coreOption.clientId, coreOption.region);
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            TapTapLoginManager.Instance.Init(coreOption.clientId, coreOption.region);
        }
    }
}
