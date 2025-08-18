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
                Debug.LogError("Unity-iPhone is NUll");
                return;
            }
            if (TapSDKCoreCompile.HandlerIOSSetting(path,
                Application.dataPath,
                "TapTapAchievementResource",
                "com.taptap.sdk.achievement",
                "Achievement",
                new[] {"TapTapAchievementResource.bundle"},
                target, projPath, proj,
                "TapTapAchievementSDK"))
            {
                Debug.Log("TapAchievement add Bundle Success!");
                return;
            }

            Debug.LogWarning("TapAchievement add Bundle Failed!");
        }
    }
}
#endif
