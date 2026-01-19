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
    /// <summary>
    /// TapTap Login iOS/macOS 平台构建后处理器
    /// 用于在 Unity 构建完成后自动配置 Xcode 项目：
    /// 1. 合并 TDS-Info.plist 配置到应用的 Info.plist
    /// 2. 添加 TapTapLoginResource.bundle 资源包到 Xcode 项目
    /// </summary>
    public static class TapLoginIOSProcessor
    {
        #region Constants

        /// <summary>TapSDK 配置文件名</summary>
        private const string TDS_INFO_PLIST_NAME = "TDS-Info.plist";

        /// <summary>TapSDK 配置文件搜索路径（相对于项目根目录）</summary>
        private const string TDS_INFO_SEARCH_PATH = "/Assets/Plugins/";

        /// <summary>TapTap Login 资源包名称</summary>
        private const string LOGIN_RESOURCE_BUNDLE_NAME = "TapTapLoginResource";

        /// <summary>TapTap Login 包标识符</summary>
        private const string LOGIN_PACKAGE_ID = "com.taptap.sdk.login";

        /// <summary>TapTap Login 模块名称</summary>
        private const string LOGIN_MODULE_NAME = "Login";

        /// <summary>TapTap Login 资源包文件名</summary>
        private const string LOGIN_RESOURCE_BUNDLE_FILE = "TapTapLoginResource.bundle";

        /// <summary>Xcode 项目文件扩展名</summary>
        private const string XCODE_PROJECT_EXTENSION = ".xcodeproj";

        /// <summary>Xcode 项目配置文件名</summary>
        private const string XCODE_PROJECT_FILE = "project.pbxproj";

        #endregion

        /// <summary>
        /// Unity 构建后处理回调
        /// 在 iOS 或 macOS 平台构建完成后自动执行，配置 Xcode 项目
        /// </summary>
        /// <param name="buildTarget">构建目标平台</param>
        /// <param name="path">构建输出路径（iOS 为 Xcode 项目目录，macOS 为 .app 路径）</param>
        [PostProcessBuild(103)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            // 只处理 iOS 和 macOS 平台
            if (buildTarget != BuildTarget.iOS && buildTarget != BuildTarget.StandaloneOSX)
            {
                return;
            }

            // 查找 TDS-Info.plist 配置文件
            var parentFolder = Directory.GetParent(Application.dataPath)?.FullName;
            var plistSearchPath = parentFolder + TDS_INFO_SEARCH_PATH;
            var plistFile = TapFileHelper.RecursionFilterFile(plistSearchPath, TDS_INFO_PLIST_NAME);

            if (plistFile == null || !plistFile.Exists)
            {
                Debug.LogError($"TapSDK Can't find {TDS_INFO_PLIST_NAME} in {plistSearchPath}!");
                return;
            }

            // 处理 iOS 平台
            if (buildTarget == BuildTarget.iOS)
            {
#if UNITY_IOS
                // 合并 TDS-Info.plist 到 iOS 应用的 Info.plist
                TapSDKCoreCompile.HandlerPlist(Path.GetFullPath(path), plistFile.FullName);

                // 添加 TapTapLoginResource.bundle 到 Xcode 项目
                AddLoginResourceBundle(path);
#endif
            }
            // 处理 macOS 平台
            else if (buildTarget == BuildTarget.StandaloneOSX)
            {
#if UNITY_IOS
                // 合并 TDS-Info.plist 到 macOS 应用的 Info.plist
                TapSDKCoreCompile.HandlerPlist(Path.GetFullPath(path), plistFile.FullName, true);
#endif
            }
        }

#if UNITY_IOS
        /// <summary>
        /// 将 TapTapLoginResource.bundle 添加到 iOS Xcode 项目
        /// 包含登录界面所需的图片、文本等资源
        /// </summary>
        /// <param name="buildPath">Xcode 项目构建路径</param>
        private static void AddLoginResourceBundle(string buildPath)
        {
            try
            {
                // 获取 Xcode 项目文件路径
                var projPath = TapSDKCoreCompile.GetProjPath(buildPath);
                var proj = TapSDKCoreCompile.ParseProjPath(projPath);

                // 获取 Unity-iPhone target
                var target = TapSDKCoreCompile.GetUnityTarget(proj);
                if (TapSDKCoreCompile.CheckTarget(target))
                {
                    Debug.LogError("TapLogin: Unity-iPhone target is null, cannot add resource bundle");
                    return;
                }

                // 添加资源包到 Xcode 项目
                bool success = TapSDKCoreCompile.HandlerIOSSetting(
                    buildPath,
                    Application.dataPath,
                    LOGIN_RESOURCE_BUNDLE_NAME,
                    LOGIN_PACKAGE_ID,
                    LOGIN_MODULE_NAME,
                    new[] { LOGIN_RESOURCE_BUNDLE_FILE },
                    target,
                    projPath,
                    proj
                );

                if (success)
                {
                    Debug.Log($"TapLogin: Successfully added {LOGIN_RESOURCE_BUNDLE_FILE} to Xcode project");
                }
                else
                {
                    Debug.LogWarning($"TapLogin: Failed to add {LOGIN_RESOURCE_BUNDLE_FILE} to Xcode project");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TapLogin: Exception while adding resource bundle: {ex.Message}");
            }
        }
#endif
    }
#endif
}