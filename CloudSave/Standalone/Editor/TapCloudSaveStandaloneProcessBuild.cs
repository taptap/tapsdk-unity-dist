using System;
using TapSDK.Core.Editor;
using UnityEditor.Build.Reporting;

namespace TapSDK.CloudSave.Standalone.Editor
{
    public class TapCloudSaveStandaloneProcessBuild : SDKLinkProcessBuild
    {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/CloudSave/link.xml";

        public override LinkedAssembly[] LinkedAssemblies =>
            new LinkedAssembly[]
            {
                new LinkedAssembly { Fullname = "TapSDK.CloudSave.Runtime" },
                new LinkedAssembly { Fullname = "TapSDK.CloudSave.Standalone.Runtime" },
            };

        public override Func<BuildReport, bool> IsTargetPlatform =>
            (report) =>
            {
                return BuildTargetUtils.IsSupportStandalone(report.summary.platform);
            };
    }
}
