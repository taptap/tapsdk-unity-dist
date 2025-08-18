using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Leaderboard.Mobile.Editor {
    /// <summary>
    /// 处理 TapSDK Leaderboard 模块在移动平台的构建过程
    /// 确保正确的程序集被包含在构建中
    /// </summary>
    public class TapLeaderboardMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Leaderboard/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Leaderboard.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Leaderboard.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
