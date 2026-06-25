#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace TapSDK.Core.Editor
{
    public static class TapSDKIOSOpenURLProcessor
    {
        private const int PostProcessOrder = 1200;
        private const string SceneManifestKey = "UIApplicationSceneManifest";
        private const string GeneratedFolder = "Classes/TapSDK";
        private const string GeneratedFileName = "TapSDKUnitySceneOpenURL.mm";
        private const string TemplateFileName = "TapSDKUnitySceneOpenURL.mm.template";
        private const string SchemesPlaceholder = "__TAPSDK_OPEN_URL_SCHEMES__";
        private const string SceneDelegateClassNamesPlaceholder = "__TAPSDK_SCENE_DELEGATE_CLASS_NAMES__";
        private const string SceneOpenURLMarker = "TapSDKUnitySceneOpenURL";
        private static readonly string[] ProjectSourceExcludedDirectories =
        {
            GeneratedFolder,
            "Pods",
            "Libraries",
            "Frameworks",
            "Data",
            "Build",
            "DerivedData"
        };
        private static readonly Regex BuildSettingReferenceRegex =
            new Regex(@"\$\(([^):]+)(?::([^)]+))?\)|\$\{([^}:]+)(?::([^}]+))?\}");
        private static readonly Regex SceneOpenURLMarkerRegex =
            new Regex(@"(?<![A-Za-z0-9_])" + SceneOpenURLMarker + @"(?![A-Za-z0-9_])");

        [PostProcessBuild(PostProcessOrder)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            var buildPath = Path.GetFullPath(path);
            var plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning($"[TapSDK][iOS] Info.plist not found, skip openURL auto config: {plistPath}");
                return;
            }

            var settings = TapSDKCoreCompile.GetIOSSettingsFromTDSInfo(Application.dataPath);
            if (settings.disableSceneManifest)
            {
                RemoveSceneManifest(plistPath);
                RemoveGeneratedSceneOpenURLHook(buildPath);
                Debug.Log("[TapSDK][iOS] SceneManifest disabled by TDS-Info.json, use AppDelegate openURL flow.");
                return;
            }

            if (!settings.autoConfigureOpenURL)
            {
                RemoveGeneratedSceneOpenURLHook(buildPath);
                Debug.Log("[TapSDK][iOS] OpenURL auto config disabled by TDS-Info.json.");
                return;
            }

            var plist = ReadPlist(plistPath);
            if (!HasSceneManifest(plist))
            {
                RemoveGeneratedSceneOpenURLHook(buildPath);
                Debug.Log("[TapSDK][iOS] SceneManifest not found, use UnityRegisterAppDelegateListener openURL flow.");
                return;
            }

            var schemes = GetTrackedOpenURLSchemes(plist);
            if (schemes.Count == 0)
            {
                RemoveGeneratedSceneOpenURLHook(buildPath);
                Debug.LogWarning("[TapSDK][iOS] SceneManifest detected but no TapSDK URL schemes added by build scripts were found in Info.plist.");
                return;
            }

            var projPath = TapSDKCoreCompile.GetProjPath(buildPath);
            var proj = File.Exists(projPath) ? TapSDKCoreCompile.ParseProjPath(projPath) : null;
            var sceneDelegateClassNames = GetSceneDelegateClassNamesRequiringHook(buildPath, plist, proj);
            if (sceneDelegateClassNames.Count == 0)
            {
                RemoveGeneratedSceneOpenURLHook(buildPath);
                Debug.Log("[TapSDK][iOS] No eligible scene delegate found for TapSDK Scene openURL hook.");
                return;
            }

            InstallSceneOpenURLHook(
                buildPath,
                projPath,
                proj,
                schemes,
                sceneDelegateClassNames
            );
        }

        private static PlistDocument ReadPlist(string plistPath)
        {
            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            return plist;
        }

        private static bool HasSceneManifest(PlistDocument plist)
        {
            return plist.root.values.ContainsKey(SceneManifestKey);
        }

        private static void RemoveSceneManifest(string plistPath)
        {
            var plist = ReadPlist(plistPath);
            if (!plist.root.values.Remove(SceneManifestKey))
            {
                Debug.Log("[TapSDK][iOS] SceneManifest not found, nothing to remove.");
                return;
            }

            File.WriteAllText(plistPath, plist.WriteToString());
        }

        private static List<string> GetSceneDelegateClassNamesRequiringHook(
            string buildPath,
            PlistDocument plist,
            PBXProject proj)
        {
            var result = new List<string>();
            var buildSettings = CreateBuildSettingContext(proj);
            var sceneDelegateClassNames = GetSceneDelegateClassNames(plist);
            if (sceneDelegateClassNames.Count == 0)
            {
                Debug.LogWarning("[TapSDK][iOS] SceneManifest has no UISceneDelegateClassName, skip TapSDK Scene openURL hook.");
                return result;
            }

            if (ProjectSourceHasTapSDKOpenURLMarker(buildPath))
            {
                Debug.Log("[TapSDK][iOS] Project source already has TapSDK Scene openURL marker, skip TapSDK hook.");
                return result;
            }

            foreach (var className in sceneDelegateClassNames)
            {
                var runtimeClassNames = GetRuntimeSceneDelegateClassNames(className, buildSettings);
                if (runtimeClassNames.Count == 0)
                {
                    Debug.LogWarning($"[TapSDK][iOS] Scene delegate class name could not be resolved for {className}, skip TapSDK Scene openURL hook for this delegate.");
                    continue;
                }

                foreach (var runtimeClassName in runtimeClassNames)
                {
                    if (!result.Contains(runtimeClassName))
                    {
                        result.Add(runtimeClassName);
                    }
                }
            }

            return result;
        }

        private static List<string> GetSceneDelegateClassNames(PlistDocument plist)
        {
            var classNames = new List<string>();
            if (!(plist.root[SceneManifestKey] is PlistElementDict sceneManifest) ||
                !(sceneManifest["UISceneConfigurations"] is PlistElementDict sceneConfigurations))
            {
                return classNames;
            }

            foreach (var sceneConfiguration in sceneConfigurations.values.Values)
            {
                if (!(sceneConfiguration is PlistElementArray configurations))
                {
                    continue;
                }

                foreach (var item in configurations.values)
                {
                    if (!(item is PlistElementDict configuration) ||
                        !(configuration["UISceneDelegateClassName"] is PlistElementString classNameElement))
                    {
                        continue;
                    }

                    var className = classNameElement.AsString()?.Trim();
                    if (!string.IsNullOrEmpty(className) && !classNames.Contains(className))
                    {
                        classNames.Add(className);
                    }
                }
            }

            return classNames;
        }

        private static string NormalizeSceneDelegateClassName(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return string.Empty;
            }

            className = className.Trim();
            var lastDot = className.LastIndexOf('.');
            return lastDot >= 0 && lastDot < className.Length - 1
                ? className.Substring(lastDot + 1)
                : className;
        }

        private static bool ProjectSourceHasTapSDKOpenURLMarker(string buildPath)
        {
            return FindProjectSourceFiles(buildPath).Any(SourceFileHasTapSDKOpenURLMarker);
        }

        private static IEnumerable<string> FindProjectSourceFiles(string buildPath)
        {
            if (string.IsNullOrEmpty(buildPath) || !Directory.Exists(buildPath))
            {
                yield break;
            }

            buildPath = Path.GetFullPath(buildPath);
            var sourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".h",
                ".m",
                ".mm",
                ".swift"
            };
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sourcePath in EnumerateSourceFiles(buildPath, sourceExtensions, visited))
            {
                yield return sourcePath;
            }
        }

        private static IEnumerable<string> EnumerateSourceFiles(
            string buildPath,
            HashSet<string> sourceExtensions,
            HashSet<string> visited)
        {
            var pendingDirectories = new Stack<string>();
            var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pendingDirectories.Push(buildPath);

            while (pendingDirectories.Count > 0)
            {
                var currentDirectory = Path.GetFullPath(pendingDirectories.Pop());
                if (IsExcludedProjectSourcePath(buildPath, currentDirectory) ||
                    !visitedDirectories.Add(currentDirectory))
                {
                    continue;
                }

                string[] sourcePaths;
                try
                {
                    sourcePaths = Directory.GetFiles(currentDirectory, "*.*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TapSDK][iOS] Failed to enumerate source files for TapSDK marker scan: {currentDirectory}, {e.Message}");
                    sourcePaths = new string[0];
                }

                foreach (var sourcePath in sourcePaths)
                {
                    var fullPath = Path.GetFullPath(sourcePath);
                    if (!sourceExtensions.Contains(Path.GetExtension(sourcePath)) ||
                        !visited.Add(fullPath))
                    {
                        continue;
                    }

                    yield return fullPath;
                }

                string[] childDirectories;
                try
                {
                    childDirectories = Directory.GetDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TapSDK][iOS] Failed to enumerate source directories for TapSDK marker scan: {currentDirectory}, {e.Message}");
                    childDirectories = new string[0];
                }

                foreach (var childDirectory in childDirectories)
                {
                    var fullPath = Path.GetFullPath(childDirectory);
                    if (!IsExcludedProjectSourcePath(buildPath, fullPath))
                    {
                        pendingDirectories.Push(fullPath);
                    }
                }
            }
        }

        private static bool IsExcludedProjectSourcePath(string buildPath, string fullPath)
        {
            foreach (var relativePath in ProjectSourceExcludedDirectories)
            {
                var excludedPath = Path.GetFullPath(Path.Combine(buildPath, relativePath));
                if (IsSameOrChildPath(fullPath, excludedPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameOrChildPath(string path, string parentPath)
        {
            path = NormalizePath(path);
            parentPath = NormalizePath(parentPath);
            return string.Equals(path, parentPath, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(parentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static bool SourceFileHasTapSDKOpenURLMarker(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return false;
            }

            try
            {
                var content = File.ReadAllText(sourcePath);
                return SceneOpenURLMarkerRegex.IsMatch(content);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to read source file for TapSDK marker: {sourcePath}, {e.Message}");
                return false;
            }
        }

        private static List<string> GetRuntimeSceneDelegateClassNames(
            string className,
            XcodeBuildSettingContext buildSettings)
        {
            var result = new List<string>();
            var expandedClassName = ExpandBuildSettingReferences(className, buildSettings);
            if (!HasUnresolvedBuildSettingReference(expandedClassName))
            {
                AddIfNotEmpty(result, expandedClassName);
            }

            var normalizedClassName = NormalizeSceneDelegateClassName(expandedClassName);
            if (!string.IsNullOrEmpty(normalizedClassName) &&
                !HasUnresolvedBuildSettingReference(expandedClassName) &&
                !HasUnresolvedBuildSettingReference(normalizedClassName) &&
                !expandedClassName.Contains("."))
            {
                var productModuleName = GetProductModuleName(buildSettings);
                if (!string.IsNullOrEmpty(productModuleName))
                {
                    AddIfNotEmpty(result, $"{productModuleName}.{normalizedClassName}");
                }
            }

            return result;
        }

        private static void AddIfNotEmpty(List<string> items, string value)
        {
            if (!string.IsNullOrEmpty(value) && !items.Contains(value))
            {
                items.Add(value);
            }
        }

        private static string ExpandBuildSettingReferences(string value, XcodeBuildSettingContext context)
        {
            if (string.IsNullOrEmpty(value) || context == null)
            {
                return value;
            }

            var expanded = value.Trim();
            for (var depth = 0; depth < 8 && ContainsBuildSettingReference(expanded); depth++)
            {
                var changed = false;
                expanded = BuildSettingReferenceRegex.Replace(expanded, match =>
                {
                    var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
                    var modifier = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
                    var replacement = ResolveBuildSetting(name, context);
                    if (string.IsNullOrEmpty(replacement))
                    {
                        return match.Value;
                    }

                    if (ContainsBuildSettingReference(replacement) && replacement != match.Value)
                    {
                        replacement = ExpandBuildSettingReferences(replacement, context);
                    }

                    if (ContainsBuildSettingReference(replacement))
                    {
                        return match.Value;
                    }

                    if (string.Equals(modifier, "c99extidentifier", StringComparison.OrdinalIgnoreCase))
                    {
                        replacement = ToC99ExtIdentifier(replacement);
                    }

                    changed = changed || replacement != match.Value;
                    return replacement;
                });

                if (!changed)
                {
                    break;
                }
            }

            return expanded;
        }

        private static bool ContainsBuildSettingReference(string value)
        {
            return !string.IsNullOrEmpty(value) && BuildSettingReferenceRegex.IsMatch(value);
        }

        private static bool HasUnresolvedBuildSettingReference(string value)
        {
            return ContainsBuildSettingReference(value) ||
                   (!string.IsNullOrEmpty(value) && (value.Contains("$(") || value.Contains("${")));
        }

        private static string ResolveBuildSetting(string name, XcodeBuildSettingContext context)
        {
            if (string.IsNullOrEmpty(name) || context == null)
            {
                return string.Empty;
            }

            if (string.Equals(name, "TARGET_NAME", StringComparison.Ordinal))
            {
                return UnquoteBuildSettingValue(context.targetName);
            }

            var value = UnquoteBuildSettingValue(GetBuildPropertyForAnyConfig(context.project, context.targetGuid, name));
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (string.Equals(name, "PRODUCT_MODULE_NAME", StringComparison.Ordinal) ||
                string.Equals(name, "SWIFT_MODULE_NAME", StringComparison.Ordinal))
            {
                return ToC99ExtIdentifier(UnquoteBuildSettingValue(context.targetName));
            }

            if (string.Equals(name, "PRODUCT_NAME", StringComparison.Ordinal))
            {
                return UnquoteBuildSettingValue(context.targetName);
            }

            return string.Empty;
        }

        private static string GetProductModuleName(XcodeBuildSettingContext context)
        {
            var productModuleName = ExpandBuildSettingReferences("$(PRODUCT_MODULE_NAME)", context);
            return ContainsBuildSettingReference(productModuleName)
                ? string.Empty
                : productModuleName;
        }

        private static XcodeBuildSettingContext CreateBuildSettingContext(PBXProject proj)
        {
            if (proj == null)
            {
                return null;
            }

            var targetGuid = TapSDKCoreCompile.GetUnityTarget(proj);
            if (TapSDKCoreCompile.CheckTarget(targetGuid))
            {
                return null;
            }

            return new XcodeBuildSettingContext
            {
                project = proj,
                targetGuid = targetGuid,
                targetName = GetTargetName(proj, targetGuid)
            };
        }

        private static string GetTargetName(PBXProject proj, string targetGuid)
        {
            var buildSettingTargetName = GetBuildPropertyForAnyConfig(proj, targetGuid, "TARGET_NAME");
            if (!string.IsNullOrEmpty(buildSettingTargetName))
            {
                return UnquoteBuildSettingValue(buildSettingTargetName);
            }

            return GetFallbackTargetName(proj, targetGuid);
        }

        private static string GetFallbackTargetName(PBXProject proj, string targetGuid)
        {
            if (proj == null || string.IsNullOrEmpty(targetGuid))
            {
                return string.Empty;
            }

#if UNITY_2019_3_OR_NEWER
            if (string.Equals(GetUnityMainTargetGuid(proj), targetGuid, StringComparison.Ordinal))
            {
                var mainTargetName = GetFallbackTargetNameByGuid(proj, targetGuid, "Tuanjie-iPhone", "Unity-iPhone");
                if (!string.IsNullOrEmpty(mainTargetName))
                {
                    return mainTargetName;
                }
            }

            if (string.Equals(GetUnityFrameworkTargetGuid(proj), targetGuid, StringComparison.Ordinal))
            {
                var frameworkTargetName = GetFallbackTargetNameByGuid(proj, targetGuid, "TuanjieFramework", "UnityFramework");
                if (!string.IsNullOrEmpty(frameworkTargetName))
                {
                    return frameworkTargetName;
                }
            }
#else
            var mainTargetName = GetFallbackTargetNameByGuid(proj, targetGuid, "Unity-iPhone");
            if (!string.IsNullOrEmpty(mainTargetName))
            {
                return mainTargetName;
            }
#endif

            return string.Empty;
        }

        private static string GetFallbackTargetNameByGuid(PBXProject proj, string targetGuid, params string[] targetNames)
        {
            foreach (var targetName in targetNames)
            {
                if (string.Equals(GetTargetGuidByName(proj, targetName), targetGuid, StringComparison.Ordinal))
                {
                    return targetName;
                }
            }

            return string.Empty;
        }

#if UNITY_2019_3_OR_NEWER
        private static string GetUnityMainTargetGuid(PBXProject proj)
        {
            try
            {
                return proj.GetUnityMainTargetGuid();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to resolve Unity main target guid: {e.Message}");
                return string.Empty;
            }
        }

        private static string GetUnityFrameworkTargetGuid(PBXProject proj)
        {
            try
            {
                return proj.GetUnityFrameworkTargetGuid();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to resolve Unity framework target guid: {e.Message}");
                return string.Empty;
            }
        }
#endif

        private static string GetTargetGuidByName(PBXProject proj, string targetName)
        {
            try
            {
#if UNITY_2019_3_OR_NEWER
                var findTargetGuidByName = typeof(PBXProject).GetMethod("FindTargetGuidByName", new[] { typeof(string) });
                if (findTargetGuidByName != null)
                {
                    return findTargetGuidByName.Invoke(proj, new object[] { targetName }) as string ?? string.Empty;
                }
#endif

                return proj.TargetGuidByName(targetName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to resolve Xcode target by name {targetName}: {e.Message}");
                return string.Empty;
            }
        }

        private static string GetBuildPropertyForAnyConfig(PBXProject proj, string targetGuid, string name)
        {
            if (proj == null || string.IsNullOrEmpty(targetGuid) || string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            try
            {
                return proj.GetBuildPropertyForAnyConfig(targetGuid, name) ?? string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to read Xcode build setting {name}: {e.Message}");
                return string.Empty;
            }
        }

        private static string ToC99ExtIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().Select(ch =>
                (ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= '0' && ch <= '9') ||
                ch == '_'
                    ? ch
                    : '_'
            ).ToArray();
            var result = new string(chars);
            return result.Length > 0 && result[0] >= '0' && result[0] <= '9'
                ? "_" + result
                : result;
        }

        private static string UnquoteBuildSettingValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            return value.Length >= 2 &&
                   ((value[0] == '"' && value[value.Length - 1] == '"') ||
                    (value[0] == '\'' && value[value.Length - 1] == '\''))
                ? value.Substring(1, value.Length - 2)
                : value;
        }

        private sealed class XcodeBuildSettingContext
        {
            public PBXProject project;
            public string targetGuid;
            public string targetName;
        }

        private static List<string> GetTrackedOpenURLSchemes(PlistDocument plist)
        {
            var plistSchemes = GetURLSchemes(plist.root);
            if (plistSchemes.Count == 0)
            {
                return new List<string>();
            }

            return TapSDKCoreCompile.GetIOSURLSchemesAddedByTapSDK()
                .Select(NormalizeURLScheme)
                .Where(scheme => !string.IsNullOrEmpty(scheme) && plistSchemes.Contains(scheme))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(scheme => scheme, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static HashSet<string> GetURLSchemes(PlistElementDict root)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!(root["CFBundleURLTypes"] is PlistElementArray urlTypes))
            {
                return result;
            }

            foreach (var urlType in urlTypes.values)
            {
                if (!(urlType is PlistElementDict urlTypeDict))
                {
                    continue;
                }

                if (!(urlTypeDict["CFBundleURLSchemes"] is PlistElementArray schemes))
                {
                    continue;
                }

                foreach (var item in schemes.values)
                {
                    var value = item as PlistElementString;
                    var scheme = NormalizeURLScheme(value?.AsString());
                    if (!string.IsNullOrEmpty(scheme))
                    {
                        result.Add(scheme);
                    }
                }
            }

            return result;
        }

        private static string NormalizeURLScheme(string scheme)
        {
            return string.IsNullOrEmpty(scheme)
                ? string.Empty
                : scheme.Trim().ToLowerInvariant();
        }

        private static void InstallSceneOpenURLHook(
            string buildPath,
            string projPath,
            PBXProject proj,
            IReadOnlyList<string> schemes,
            IReadOnlyList<string> sceneDelegateClassNames)
        {
            if (proj == null)
            {
                Debug.LogError("[TapSDK][iOS] Xcode project not found, cannot add Scene openURL hook.");
                return;
            }

            var target = TapSDKCoreCompile.GetUnityFrameworkTarget(proj);
            if (TapSDKCoreCompile.CheckTarget(target))
            {
                target = TapSDKCoreCompile.GetUnityTarget(proj);
            }

            if (TapSDKCoreCompile.CheckTarget(target))
            {
                Debug.LogError("[TapSDK][iOS] UnityFramework/Unity-iPhone target not found, cannot add Scene openURL hook.");
                return;
            }

            var generatedDir = Path.Combine(buildPath, GeneratedFolder);
            Directory.CreateDirectory(generatedDir);

            var generatedRelativePath = Path.Combine(GeneratedFolder, GeneratedFileName);
            var generatedPath = Path.Combine(buildPath, generatedRelativePath);
            File.WriteAllText(generatedPath, CreateSceneOpenURLHookSource(schemes, sceneDelegateClassNames));

            var fileGuid = proj.FindFileGuidByRealPath(generatedRelativePath);
            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = proj.FindFileGuidByProjectPath(generatedRelativePath);
            }

            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = proj.AddFile(generatedRelativePath, generatedRelativePath, PBXSourceTree.Source);
            }

            RemoveFileFromKnownTargets(proj, fileGuid);
            proj.AddFileToBuild(target, fileGuid);
            proj.WriteToFile(projPath);

            Debug.Log(
                $"[TapSDK][iOS] Installed Scene openURL hook with schemes: {string.Join(", ", schemes)}, " +
                $"sceneDelegates={string.Join(", ", sceneDelegateClassNames)}"
            );
        }

        private static void RemoveFileFromKnownTargets(PBXProject proj, string fileGuid)
        {
            var unityFrameworkTarget = TapSDKCoreCompile.GetUnityFrameworkTarget(proj);
            if (!TapSDKCoreCompile.CheckTarget(unityFrameworkTarget))
            {
                proj.RemoveFileFromBuild(unityFrameworkTarget, fileGuid);
            }

            var unityTarget = TapSDKCoreCompile.GetUnityTarget(proj);
            if (!TapSDKCoreCompile.CheckTarget(unityTarget))
            {
                proj.RemoveFileFromBuild(unityTarget, fileGuid);
            }
        }

        private static void RemoveGeneratedSceneOpenURLHook(string buildPath)
        {
            var generatedRelativePath = Path.Combine(GeneratedFolder, GeneratedFileName);
            var generatedPath = Path.Combine(buildPath, generatedRelativePath);

            // Step 1: remove PBX references first; only delete the file after PBX is consistent.
            // If PBX is missing or parse fails we keep the file and warn — a dangling .mm is
            // recoverable, a dangling build-phase reference is not.
            var projPath = TapSDKCoreCompile.GetProjPath(buildPath);
            if (!File.Exists(projPath))
            {
                Debug.LogWarning("[TapSDK][iOS] pbxproj not found; skipping Scene openURL hook removal to avoid inconsistent state.");
                return;
            }

            try
            {
                var proj = TapSDKCoreCompile.ParseProjPath(projPath);
                var fileGuid = proj.FindFileGuidByRealPath(generatedRelativePath);
                if (string.IsNullOrEmpty(fileGuid))
                {
                    fileGuid = proj.FindFileGuidByProjectPath(generatedRelativePath);
                }

                if (!string.IsNullOrEmpty(fileGuid))
                {
                    var unityFrameworkTarget = TapSDKCoreCompile.GetUnityFrameworkTarget(proj);
                    if (!TapSDKCoreCompile.CheckTarget(unityFrameworkTarget))
                    {
                        proj.RemoveFileFromBuild(unityFrameworkTarget, fileGuid);
                    }

                    var unityTarget = TapSDKCoreCompile.GetUnityTarget(proj);
                    if (!TapSDKCoreCompile.CheckTarget(unityTarget))
                    {
                        proj.RemoveFileFromBuild(unityTarget, fileGuid);
                    }

                    proj.RemoveFile(fileGuid);
                    proj.WriteToFile(projPath);
                    Debug.Log("[TapSDK][iOS] Removed generated Scene openURL hook from Xcode project.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapSDK][iOS] Failed to update pbxproj ({e.Message}); keeping source file to avoid dangling reference.");
                return;
            }

            // Step 2: PBX is clean; now safe to delete the source file.
            if (File.Exists(generatedPath))
            {
                File.Delete(generatedPath);
            }

            var generatedDir = Path.GetDirectoryName(generatedPath);
            if (!string.IsNullOrEmpty(generatedDir) &&
                Directory.Exists(generatedDir) &&
                Directory.GetFileSystemEntries(generatedDir).Length == 0)
            {
                Directory.Delete(generatedDir);
            }

            Debug.Log("[TapSDK][iOS] Removed generated Scene openURL hook source.");
        }

        private static string CreateSceneOpenURLHookSource(
            IReadOnlyList<string> schemes,
            IReadOnlyList<string> sceneDelegateClassNames)
        {
            var templatePath = FindSceneOpenURLHookTemplatePath();
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new FileNotFoundException($"TapSDK Scene openURL hook template not found: {TemplateFileName}");
            }

            var template = File.ReadAllText(templatePath);
            var schemeItems = string.Join(",\n        ", schemes.Select(scheme => "@\"" + EscapeObjCString(scheme) + "\""));
            var classNameItems = string.Join(",\n        ", sceneDelegateClassNames.Select(className => "@\"" + EscapeObjCString(className) + "\""));

            template = ReplaceRequiredPlaceholder(template, SchemesPlaceholder, schemeItems);
            template = ReplaceRequiredPlaceholder(template, SceneDelegateClassNamesPlaceholder, classNameItems);
            return template;
        }

        private static string ReplaceRequiredPlaceholder(string template, string placeholder, string value)
        {
            if (!template.Contains(placeholder))
            {
                throw new InvalidOperationException($"TapSDK Scene openURL hook template missing placeholder: {placeholder}");
            }

            return template.Replace(placeholder, value);
        }

        private static string FindSceneOpenURLHookTemplatePath()
        {
            var directPath = Path.Combine(Application.dataPath, "TapSDK/Core/Editor/Templates", TemplateFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            foreach (var guid in AssetDatabase.FindAssets("TapSDKUnitySceneOpenURL"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(TemplateFileName, StringComparison.Ordinal))
                {
                    continue;
                }

                return string.IsNullOrEmpty(projectRoot)
                    ? assetPath
                    : Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            }

            return string.Empty;
        }

        private static string EscapeObjCString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
#endif
