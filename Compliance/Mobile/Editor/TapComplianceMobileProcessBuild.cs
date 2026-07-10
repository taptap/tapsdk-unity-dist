using System;
using TapSDK.Core.Editor;
using UnityEditor.Build.Reporting;

namespace TapSDK.Compliance.Mobile.Editor {
    public class TapComplianceMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 4;

        public override string LinkPath => "TapSDK/Compliance/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
            new LinkedAssembly { Fullname = "TapSDK.Compliance" },
            new LinkedAssembly { Fullname = "TapSDK.Compliance.Runtime" },
            new LinkedAssembly { Fullname = "TapSDK.Compliance.Mobile.Runtime" }
        };
        
        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}