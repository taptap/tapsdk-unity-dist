using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapTap.Moment.Mobile.Editor {
    public class TapMomentMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Moment/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Moment.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Moment.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
