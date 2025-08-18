using System;
using UnityEditor.Build.Reporting;
using TapSDK.Core.Editor;

namespace TapSDK.Relation.Mobile.Editor {
    /// <summary>
    /// 处理 TapSDK Relation 模块在移动平台的构建过程
    /// 确保正确的程序集被包含在构建中
    /// </summary>
    public class TapRelationMobileProcessBuild : SDKLinkProcessBuild {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/Relation/link.xml";

        public override LinkedAssembly[] LinkedAssemblies => new LinkedAssembly[] {
                    new LinkedAssembly { Fullname = "TapSDK.Relation.Runtime" },
                    new LinkedAssembly { Fullname = "TapSDK.Relation.Mobile.Runtime" }
                };

        public override Func<BuildReport, bool> IsTargetPlatform => (report) => {
            return BuildTargetUtils.IsSupportMobile(report.summary.platform);
        };
    }
}
