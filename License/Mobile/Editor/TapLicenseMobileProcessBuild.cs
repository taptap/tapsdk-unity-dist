using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.License.Mobile.Editor {
    public class TapLicenseMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/License/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.License.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.License.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
