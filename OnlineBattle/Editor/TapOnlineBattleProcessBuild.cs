using System;
using TapSDK.Core.Editor;
using UnityEditor.Build.Reporting;

namespace TapSDK.OnlineBattle.Editor
{
    public class TapOnlineBattleProcessBuild : SDKLinkProcessBuild
    {
        public override int callbackOrder => 0;

        public override string LinkPath => "TapSDK/OnlineBattle/link.xml";

        public override LinkedAssembly[] LinkedAssemblies =>
            new LinkedAssembly[]
            {
                new LinkedAssembly { Fullname = "TapSDK.OnlineBattle.Runtime" },
            };

        public override Func<BuildReport, bool> IsTargetPlatform =>
            (report) =>
            {
                return true;
            };
    }
}
