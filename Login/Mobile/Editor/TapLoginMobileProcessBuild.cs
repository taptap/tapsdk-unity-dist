using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Login.Mobile.Editor {
    public class TapLoginMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Login/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Login.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Login.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
