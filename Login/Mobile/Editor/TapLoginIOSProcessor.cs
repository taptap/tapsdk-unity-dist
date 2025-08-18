using System;
using System.IO;
using TapSDK.Core.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS || UNITY_STANDALONE_OSX
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

namespace TapSDK.Login.Editor
{
#if UNITY_IOS || UNITY_STANDALONE_OSX
    public static class TapLoginIOSProcessor
    {
        // 添加标签，unity导出工程后自动执行该函数
        [PostProcessBuild(103)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            var parentFolder = Directory.GetParent(Application.dataPath)?.FullName;

            var plistFile = TapFileHelper.RecursionFilterFile(parentFolder + "/Assets/Plugins/", "TDS-Info.plist");

            if (plistFile == null || !plistFile.Exists)
            {
                Debug.LogError("TapSDK Can't find TDS-Info.plist in Project/Assets/Plugins/!");
            }


            if (buildTarget is BuildTarget.iOS)
            {
#if UNITY_IOS
                TapSDKCoreCompile.HandlerPlist(Path.GetFullPath(path), plistFile?.FullName ?? null);
#endif
            }
            else if (buildTarget is BuildTarget.StandaloneOSX)
            {
                Debug.Log($"path:{path}");
                Debug.Log($"path:{Path.GetFullPath(path)}");
                Debug.Log($"dir:{Path.GetDirectoryName(path)}");
                Debug.Log($"dir:{Path.GetFileName(path)}");
                // 获得工程路径
#if UNITY_2020_1_OR_NEWER
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = "";
                }

                var fileName = Path.GetFileName(path);
                if (!fileName.EndsWith(".xcodeproj"))
                {
                    fileName += ".xcodeproj";
                }

                var projPath = Path.Combine(directory, $"{fileName}/project.pbxproj");
#elif UNITY_2019_1_OR_NEWER
                var projPath = Path.Combine(path, "project.pbxproj");
#else
#endif
#if UNITY_IOS
                TapSDKCoreCompile.HandlerPlist(Path.GetFullPath(path), plistFile.FullName, true);
#endif
            }
            
#if UNITY_IOS
            var projPath1 = TapSDKCoreCompile.GetProjPath(path);
            var proj = TapSDKCoreCompile.ParseProjPath(projPath1);
            var target = TapSDKCoreCompile.GetUnityTarget(proj);
            if (TapSDKCoreCompile.CheckTarget(target))
            {
                Debug.LogError("Unity-iPhone is NUll");
                return;
            }
            if (TapSDKCoreCompile.HandlerIOSSetting(path,
                    Application.dataPath,
                    "TapTapLoginResource",
                    "com.taptap.sdk.login",
                    "Login",
                    new[] {"TapTapLoginResource.bundle"},
                    target, projPath1, proj, "TapTapLoginSDK"))
            {
                Debug.Log("TapLogin add Bundle Success!");
                return;
            }
            Debug.LogWarning("TapLogin add Bundle Failed!");
#endif
        }
    }
#endif
}