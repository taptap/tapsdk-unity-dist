using TapSDK.Core.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TapTap.Moment.Editor
{
#if UNITY_IOS
    public static class TapMomentIOSProcessor
    {
        [PostProcessBuild(101)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var projPath = TapSDKCoreCompile.GetProjPath(path);
            var proj = TapSDKCoreCompile.ParseProjPath(projPath);
            var target = TapSDKCoreCompile.GetUnityTarget(proj);
            var unityFrameworkTarget = TapSDKCoreCompile.GetUnityFrameworkTarget(proj);
            if (TapSDKCoreCompile.CheckTarget(target))
            {
                Debug.LogError("Unity-iPhone is NUll");
                return;
            }
            
            proj.AddFrameworkToProject(unityFrameworkTarget, "AVFoundation.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "CoreTelephony.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "MobileCoreServices.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "Photos.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "SystemConfiguration.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "WebKit.framework", false);
            
            if (TapSDKCoreCompile.HandlerIOSSetting(path,
                Application.dataPath,
                "TapTapMomentResource",
                "com.taptap.sdk.moment",
                "Moment",
                new[] {"TapTapMomentResource.bundle"},
                target, projPath, proj, "TapTapMomentSDK"))
            {
                Debug.Log("TapMoment add Bundle Success!");
                return;
            }

            Debug.LogError("TapMoment add Bundle Failed!");
        }
    }
#endif
}