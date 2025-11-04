using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
# if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TapSDK.Core.Editor;
using System.Diagnostics;

#if UNITY_IOS
public class BuildPostProcessor
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            var projPath = TapSDKCoreCompile.GetProjPath(path);
            var proj = TapSDKCoreCompile.ParseProjPath(projPath);
            var target = TapSDKCoreCompile.GetUnityTarget(proj);

            if (TapSDKCoreCompile.CheckTarget(target))
            {
                UnityEngine.Debug.LogError("Unity-iPhone is NUll");
                return;
            }
            if (TapSDKCoreCompile.HandlerIOSSetting(path,
                Application.dataPath,
                "TapTapRelationResource",
                "com.taptap.sdk.relation",
                "Relation",
                new[] { "TapTapRelationResource.bundle" },
                target, projPath, proj))
            {
                UnityEngine.Debug.Log("TapRelation add Bundle Success!");
            }
            if (TapSDKCoreCompile.HandlerIOSSetting(path,
                Application.dataPath,
                "TapTapProfileResource",
                "com.taptap.sdk.profile",
                "Profile",
                new[] { "TapTapProfileResource.bundle" },
                target, projPath, proj))
            {
                UnityEngine.Debug.Log("TapProfile add Bundle Success!");
                TapSDKCoreCompile.ExecutePodCommand("pod deintegrate && pod install", path);
                return;
            }

            UnityEngine.Debug.LogWarning("TapRelation add Bundle Failed!");
        }
    }
    
}
#endif
