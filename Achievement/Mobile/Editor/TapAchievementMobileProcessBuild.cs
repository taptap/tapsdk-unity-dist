using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Achievement.Mobile.Editor
{
    public class TapAchievementMobileProcessBuild : SDKLinkProcessBuild
    {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Achievement/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Achievement.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Achievement.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) =>
        {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
