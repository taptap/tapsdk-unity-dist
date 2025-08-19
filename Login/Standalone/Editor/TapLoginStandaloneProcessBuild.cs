using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Login.Editor
{
    public class TapLoginStandaloneProcessBuild : SDKLinkProcessBuild
    {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Login/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Login.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Login.Standalone.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportStandalone(report.summary.platform);
        };
    }
}