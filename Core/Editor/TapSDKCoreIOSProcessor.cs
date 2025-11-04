using System.IO;
using System.Linq;
using UnityEditor;
# if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

namespace TapSDK.Core.Editor
{
# if UNITY_IOS
    public static class TapCommonIOSProcessor
    {
        // 添加标签，unity导出工程后自动执行该函数
        [PostProcessBuild(99)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;

            // 获得工程路径
            var projPath = TapSDKCoreCompile.GetProjPath(path);
            var proj = TapSDKCoreCompile.ParseProjPath(projPath);
            var target = TapSDKCoreCompile.GetUnityTarget(proj);
            var unityFrameworkTarget = TapSDKCoreCompile.GetUnityFrameworkTarget(proj);

            if (TapSDKCoreCompile.CheckTarget(target))
            {
                Debug.LogError("Unity-iPhone is NUll");
                return;
            }

            // proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
            // proj.AddBuildProperty(unityFrameworkTarget, "OTHER_LDFLAGS", "-ObjC");

            proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            proj.SetBuildProperty(target, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(target, "CLANG_ENABLE_MODULES", "YES");

            proj.SetBuildProperty(unityFrameworkTarget, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(unityFrameworkTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            proj.SetBuildProperty(unityFrameworkTarget, "BUILD_LIBRARY_FOR_DISTRIBUTION", "YES");

            proj.SetBuildProperty(unityFrameworkTarget, "SWIFT_VERSION", "5.0");
            proj.SetBuildProperty(unityFrameworkTarget, "CLANG_ENABLE_MODULES", "YES");

            proj.AddFrameworkToProject(unityFrameworkTarget, "MobileCoreServices.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "WebKit.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "Security.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "SystemConfiguration.framework", false);
            proj.AddFrameworkToProject(unityFrameworkTarget, "CoreTelephony.framework", false);

            proj.AddFileToBuild(unityFrameworkTarget,
                proj.AddFile("usr/lib/libc++.tbd", "libc++.tbd", PBXSourceTree.Sdk));

            proj.AddFileToBuild(unityFrameworkTarget,
                proj.AddFile("usr/lib/libsqlite3.tbd", "libsqlite3.tbd", PBXSourceTree.Sdk));

            proj.WriteToFile(projPath);
            string podfilePath = Path.Combine(path, "Podfile");
            if (!File.Exists(podfilePath))
            {
                Debug.LogWarning("Podfile not found.");
                return;
            }

            string podfileContent = File.ReadAllText(podfilePath);
            podfileContent += "\ninstall! 'cocoapods', :warn_for_unused_master_specs_repo => false";

            File.WriteAllText(podfilePath, podfileContent);
        }
    }
#endif
}
