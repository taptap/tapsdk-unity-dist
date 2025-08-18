using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Compliance.Standalone.Editor {
    public class TapComplianceStandaloneProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 3;

        public override string LinkPath => "TapSDK/Compliance/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
            new LinkedAssembly { Fullname = "TapSDK.Compliance" },
            new LinkedAssembly { Fullname = "TapSDK.Compliance.Standalone.Runtime" }
        };
        
        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportStandalone(report.summary.platform);
        };
    }
}