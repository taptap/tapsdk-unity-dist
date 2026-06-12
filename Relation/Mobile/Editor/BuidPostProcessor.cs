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
    private const string RELATION_URL_SCHEME_PREFIX = "tds";
    private const string RELATION_URL_SCHEME_NAME = "TapTapRelation";

    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            AddRelationURLScheme(path);

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

    private static void AddRelationURLScheme(string path)
    {
        var clientId = TapSDKCoreCompile.GetAppClientIdFromTDSInfo(Application.dataPath);
        if (string.IsNullOrEmpty(clientId))
        {
            UnityEngine.Debug.LogError("TapRelation Can't find app.client_id in TDS-Info.json or taptap.client_id in fallback TDS-Info.plist!");
            return;
        }

        TapSDKCoreCompile.AddURLSchemeToPlist(
            Path.GetFullPath(path),
            RELATION_URL_SCHEME_NAME,
            RELATION_URL_SCHEME_PREFIX + clientId
        );
    }

}
#endif
