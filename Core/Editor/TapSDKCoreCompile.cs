using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;



#if UNITY_IOS
using System;
using Google;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

#endif

namespace TapSDK.Core.Editor
{
    public static class TapSDKCoreCompile
    {
        private const string TDSInfoJsonName = "TDS-Info.json";
        private const string TDSInfoPlistName = "TDS-Info.plist";
        private static string cachedAppClientId;
        private static readonly HashSet<string> iosURLSchemesAddedByTapSDK =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        public struct TDSInfoIOSSettings
        {
            public bool autoConfigureOpenURL;
            public bool disableSceneManifest;

            public static TDSInfoIOSSettings Default => new TDSInfoIOSSettings
            {
                autoConfigureOpenURL = true,
                disableSceneManifest = false
            };
        }

        public static string FindTDSInfoJsonPath(string appDataPath)
        {
            if (string.IsNullOrEmpty(appDataPath))
            {
                return string.Empty;
            }

            var parentFolder = Directory.GetParent(appDataPath)?.FullName;
            if (string.IsNullOrEmpty(parentFolder))
            {
                return string.Empty;
            }

            var jsonPath = Path.Combine(parentFolder, "Assets", "Plugins", TDSInfoJsonName);
            return File.Exists(jsonPath) ? jsonPath : string.Empty;
        }

        public static string FindTDSInfoPlistPath(string appDataPath)
        {
            if (string.IsNullOrEmpty(appDataPath))
            {
                return string.Empty;
            }

            var parentFolder = Directory.GetParent(appDataPath)?.FullName;
            if (string.IsNullOrEmpty(parentFolder))
            {
                return string.Empty;
            }

            var plistSearchPath = Path.Combine(parentFolder, "Assets", "Plugins");
            if (!Directory.Exists(plistSearchPath))
            {
                return string.Empty;
            }

            var plistFile = TapFileHelper.RecursionFilterFile(plistSearchPath, TDSInfoPlistName);
            return plistFile != null && plistFile.Exists ? plistFile.FullName : string.Empty;
        }

        public static string GetAppClientIdFromTDSInfo(string appDataPath, string fallbackInfoPlistPath = null)
        {
            if (!string.IsNullOrEmpty(cachedAppClientId))
            {
                return cachedAppClientId;
            }

            var jsonPath = FindTDSInfoJsonPath(appDataPath);
            if (!string.IsNullOrEmpty(jsonPath))
            {
                return CacheAppClientId(GetAppClientIdFromTDSInfoJson(jsonPath));
            }

            var plistPath = !string.IsNullOrEmpty(fallbackInfoPlistPath)
                ? fallbackInfoPlistPath
                : FindTDSInfoPlistPath(appDataPath);
            return CacheAppClientId(GetTapTapClientIdFromInfoPlist(plistPath));
        }

        private static string CacheAppClientId(string clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                cachedAppClientId = clientId;
            }

            return clientId;
        }

        public static string GetAppClientIdFromTDSInfoJson(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                return string.Empty;
            }

            try
            {
                var json = JObject.Parse(File.ReadAllText(jsonPath));
                var app = json["app"] as JObject;
                return app?["client_id"]?.Value<string>() ?? string.Empty;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"TapSDK Failed to parse {jsonPath}: {e.Message}");
                return string.Empty;
            }
        }

        public static TDSInfoIOSSettings GetIOSSettingsFromTDSInfo(string appDataPath)
        {
            var jsonPath = FindTDSInfoJsonPath(appDataPath);
            return GetIOSSettingsFromTDSInfoJson(jsonPath);
        }

        public static TDSInfoIOSSettings GetIOSSettingsFromTDSInfoJson(string jsonPath)
        {
            var settings = TDSInfoIOSSettings.Default;
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                return settings;
            }

            try
            {
                var json = JObject.Parse(File.ReadAllText(jsonPath));
                var ios = json["ios"] as JObject;
                settings.autoConfigureOpenURL = ReadOptionalBool(ios, "auto_configure_open_url", settings.autoConfigureOpenURL);
                settings.disableSceneManifest = ReadOptionalBool(ios, "disable_scene_manifest", settings.disableSceneManifest);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"TapSDK Failed to parse iOS settings from {jsonPath}: {e.Message}");
            }

            return settings;
        }

        private static bool ReadOptionalBool(JObject json, string key, bool defaultValue)
        {
            if (json == null || string.IsNullOrEmpty(key) || !json.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }

            if (value.Type == JTokenType.Boolean)
            {
                return value.Value<bool>();
            }

            if (value.Type == JTokenType.String && bool.TryParse(value.Value<string>(), out var result))
            {
                return result;
            }

            return defaultValue;
        }

        public static string GetTapTapClientIdFromInfoPlist(string infoPlistPath)
        {
            if (string.IsNullOrEmpty(infoPlistPath) || !File.Exists(infoPlistPath))
            {
                return string.Empty;
            }

            var dic = (Dictionary<string, object>)Plist.readPlist(infoPlistPath);
            if (!dic.TryGetValue("taptap", out var taptapValue))
            {
                return string.Empty;
            }

            var taptapDic = taptapValue as Dictionary<string, object>;
            if (taptapDic == null || !taptapDic.TryGetValue("client_id", out var clientIdValue))
            {
                return string.Empty;
            }

            return clientIdValue as string ?? string.Empty;
        }

        public static List<string> GetIOSURLSchemesAddedByTapSDK()
        {
            return iosURLSchemesAddedByTapSDK
                .OrderBy(scheme => scheme, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal static void ClearIOSURLSchemesAddedByTapSDK()
        {
            iosURLSchemesAddedByTapSDK.Clear();
        }

        private static void RecordIOSURLSchemeAddedByTapSDK(string urlScheme)
        {
            if (!string.IsNullOrEmpty(urlScheme))
            {
                iosURLSchemesAddedByTapSDK.Add(urlScheme.Trim().ToLowerInvariant());
            }
        }

#if UNITY_IOS
        public static string GetProjPath(string path)
        {
            UnityEngine.Debug.Log($"SDX , GetProjPath path:{path}");
            return PBXProject.GetPBXProjectPath(path);
        }

        public static PBXProject ParseProjPath(string path)
        {
            UnityEngine.Debug.Log($"SDX , ParseProjPath path:{path}");
            var proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(path));
            return proj;
        }

        public static string GetUnityFrameworkTarget(PBXProject proj)
        {
#if UNITY_2019_3_OR_NEWER
            UnityEngine.Debug.Log("SDX , GetUnityFrameworkTarget UNITY_2019_3_OR_NEWER");
            string target = proj.GetUnityFrameworkTargetGuid();
            if (!string.IsNullOrEmpty(target)) return target;
            // 团结引擎（Tuanjie Engine）的 framework target 名称不同，尝试按名称查找
            target = proj.TargetGuidByName("TuanjieFramework");
            if (!string.IsNullOrEmpty(target))
            {
                UnityEngine.Debug.Log("SDX , GetUnityFrameworkTarget fallback to TuanjieFramework");
                return target;
            }
            target = proj.TargetGuidByName("UnityFramework");
            if (!string.IsNullOrEmpty(target))
            {
                UnityEngine.Debug.Log("SDX , GetUnityFrameworkTarget fallback to UnityFramework by name");
                return target;
            }
            return target;
#endif
            UnityEngine.Debug.Log("SDX , GetUnityFrameworkTarget");
            var unityPhoneTarget = proj.TargetGuidByName("Unity-iPhone");
            return unityPhoneTarget;
        }

        public static string GetUnityTarget(PBXProject proj)
        {
#if UNITY_2019_3_OR_NEWER
            UnityEngine.Debug.Log("SDX , GetUnityTarget UNITY_2019_3_OR_NEWER");
            string target = proj.GetUnityMainTargetGuid();
            if (!string.IsNullOrEmpty(target)) return target;
            // 团结引擎（Tuanjie Engine）主 target 名称为 Tuanjie-iPhone，按名称 fallback
            target = proj.TargetGuidByName("Tuanjie-iPhone");
            if (!string.IsNullOrEmpty(target))
            {
                UnityEngine.Debug.Log("SDX , GetUnityTarget fallback to Tuanjie-iPhone");
                return target;
            }
            target = proj.TargetGuidByName("Unity-iPhone");
            if (!string.IsNullOrEmpty(target))
            {
                UnityEngine.Debug.Log("SDX , GetUnityTarget fallback to Unity-iPhone by name");
                return target;
            }
            return target;
#endif
            UnityEngine.Debug.Log("SDX , GetUnityTarget");
            var unityPhoneTarget = proj.TargetGuidByName("Unity-iPhone");
            return unityPhoneTarget;
        }


        public static bool CheckTarget(string target)
        {
            return string.IsNullOrEmpty(target);
        }

        public static string GetUnityPackagePath(string parentFolder, string unityPackageName)
        {
            var request = Client.List(true);
            while (request.IsCompleted == false)
            {
                System.Threading.Thread.Sleep(100);
            }
            var pkgs = request.Result;
            if (pkgs == null)
                return "";
            foreach (var pkg in pkgs)
            {
                if (pkg.name == unityPackageName)
                {
                    if (pkg.source == PackageSource.Local)
                        return pkg.resolvedPath;
                    else if (pkg.source == PackageSource.Embedded)
                        return pkg.resolvedPath;
                    else
                    {
                        return pkg.resolvedPath;
                    }
                }
            }

            return "";
        }

        public static bool HandlerIOSSetting(string path, string appDataPath, string resourceName,
            string modulePackageName,
            string moduleName, string[] bundleNames, string target, string projPath, PBXProject proj)
        {

            var resourcePath = Path.Combine(path, resourceName);

            var parentFolder = Directory.GetParent(appDataPath).FullName;

            UnityEngine.Debug.Log($"ProjectFolder path:{parentFolder}" + " resourcePath： " + resourcePath + " parentFolder: " + parentFolder);

            if (Directory.Exists(resourcePath))
            {
                Directory.Delete(resourcePath, true);
            }

            var podSpecPath = Path.Combine(path + "/Pods", "TapTapSDK");
            //使用 cocospod 远程依赖
            if (Directory.Exists(podSpecPath))
            {
                var fwRoot = Path.Combine(path + "/Pods", "TapTapSDK/iOS/Frameworks");
                resourcePath = fwRoot;
                // 兼容新发版结构：bundle 已按版本子目录存放（podspec 形如 'iOS/Frameworks/<version>/Xxx.bundle'）。
                // 优先识别 fwRoot 下"包含全部目标 bundle"的子目录；找不到则 fallback 到平层（老 podspec）。
                if (Directory.Exists(fwRoot))
                {
                    var versioned = Directory.GetDirectories(fwRoot)
                        .FirstOrDefault(d => bundleNames.All(b => Directory.Exists(Path.Combine(d, b))));
                    if (versioned != null)
                    {
                        resourcePath = versioned;
                    }
                }
                UnityEngine.Debug.Log($"Find {moduleName} use pods resourcePath:{resourcePath}");
            }
            else
            {
                Directory.CreateDirectory(resourcePath);
                var remotePackagePath = GetUnityPackagePath(parentFolder, modulePackageName);

                var assetLocalPackagePath = TapFileHelper.FilterFileByPrefix(parentFolder + "/Assets/TapSDK/", moduleName);

                var localPackagePath = TapFileHelper.FilterFileByPrefix(parentFolder, moduleName);

                UnityEngine.Debug.Log($"Find {moduleName} path: remote = {remotePackagePath} asset = {assetLocalPackagePath} local = {localPackagePath}");
                var tdsResourcePath = "";

                if (!string.IsNullOrEmpty(remotePackagePath))
                {
                    tdsResourcePath = remotePackagePath;
                }
                else if (!string.IsNullOrEmpty(assetLocalPackagePath))
                {
                    tdsResourcePath = assetLocalPackagePath;
                }
                else if (!string.IsNullOrEmpty(localPackagePath))
                {
                    tdsResourcePath = localPackagePath;
                }

                if (string.IsNullOrEmpty(tdsResourcePath))
                {
                    throw new Exception(string.Format("Can't find tdsResourcePath with module of : {0}", modulePackageName));
                }

                tdsResourcePath = $"{tdsResourcePath}/Plugins/iOS/Resource";

                UnityEngine.Debug.Log($"Find {moduleName} path:{tdsResourcePath}");

                if (!Directory.Exists(tdsResourcePath))
                {
                    throw new Exception(string.Format("Can't Find {0}", tdsResourcePath));
                }

                TapFileHelper.CopyAndReplaceDirectory(tdsResourcePath, resourcePath);
            }
            foreach (var name in bundleNames)
            {
                var relativePath = GetRelativePath(Path.Combine(resourcePath, name), path);
                if (!proj.ContainsFileByRealPath(relativePath))
                {
                    var fileGuid = proj.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                    proj.AddFileToBuild(target, fileGuid);
                }
            }

            File.WriteAllText(projPath, proj.WriteToString());
            return true;
        }

        private static string GetRelativePath(string absolutePath, string rootPath)
        {
            if (Directory.Exists(rootPath) && !rootPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                rootPath += Path.AltDirectorySeparatorChar;
            }
            Uri aboslutePathUri = new Uri(absolutePath);
            Uri rootPathUri = new Uri(rootPath);
            var relateivePath = rootPathUri.MakeRelativeUri(aboslutePathUri).ToString();
            UnityEngine.Debug.LogFormat($"[TapSDKCoreCompile] GetRelativePath absolutePath:{absolutePath} rootPath:{rootPath} relateivePath:{relateivePath} ");
            return relateivePath;
        }

        public static bool HandlerPlist(string pathToBuildProject, string infoPlistPath, bool macos = false)
        {
            var plistPath = GetInfoPlistPath(pathToBuildProject, macos);
            UnityEngine.Debug.Log($"plist path:{plistPath}");

            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            var rootDic = plist.root;

            var items = new List<string>
            {
                "tapsdk",
                "tapiosdk",
                "taptap"
            };

            if (!(rootDic["LSApplicationQueriesSchemes"] is PlistElementArray plistElementList))
            {
                plistElementList = rootDic.CreateArray("LSApplicationQueriesSchemes");
            }

            string listData = "";
            foreach (var item in plistElementList.values)
            {
                if (item is PlistElementString)
                {
                    listData += item.AsString() + ";";
                }
            }
            foreach (var t in items)
            {
                if (!listData.Contains(t + ";"))
                {
                    plistElementList.AddString(t);
                }
            }

            var hasInfoPlist = !string.IsNullOrEmpty(infoPlistPath) && File.Exists(infoPlistPath);
            var dic = hasInfoPlist ? (Dictionary<string, object>)Plist.readPlist(infoPlistPath) : null;
            var taptapId = GetAppClientIdFromTDSInfo(Application.dataPath, hasInfoPlist ? infoPlistPath : null);
            if (string.IsNullOrEmpty(taptapId))
            {
                UnityEngine.Debug.LogError("TapSDK Can't find app.client_id in TDS-Info.json or taptap.client_id in TDS-Info.plist");
                return false;
            }

            if (dic != null)
            {
                foreach (var item in dic)
                {
                    if (!item.Key.Equals("taptap"))
                    {
                        rootDic.SetString(item.Key, item.Value.ToString());
                    }
                }
            }

            AddURLSchemeToPlist(
                rootDic,
                macos ? "TapWeb" : "TapTap",
                macos ? $"open-taptap-{taptapId}" : $"tt{taptapId}"
            );

            File.WriteAllText(plistPath, plist.WriteToString());
            UnityEngine.Debug.Log("TapSDK change plist Success");
            return true;
        }

        public static bool AddURLSchemeToPlist(string pathToBuildProject, string urlSchemeName, string urlScheme, bool macos = false)
        {
            if (string.IsNullOrEmpty(urlScheme))
            {
                return false;
            }

            var plistPath = GetInfoPlistPath(pathToBuildProject, macos);
            if (!File.Exists(plistPath))
            {
                UnityEngine.Debug.LogError($"TapSDK Can't find Info.plist at {plistPath}");
                return false;
            }

            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            var dict = plist.root.AsDict();
            if (!AddURLSchemeToPlist(dict, urlSchemeName, urlScheme))
            {
                return false;
            }

            File.WriteAllText(plistPath, plist.WriteToString());
            UnityEngine.Debug.Log($"TapSDK added URL scheme: {urlScheme}");
            return true;
        }

        private static bool AddURLSchemeToPlist(PlistElementDict dict, string urlSchemeName, string urlScheme)
        {
            if (dict == null || string.IsNullOrEmpty(urlScheme))
            {
                return false;
            }

            RecordIOSURLSchemeAddedByTapSDK(urlScheme);

            if (!(dict["CFBundleURLTypes"] is PlistElementArray array))
            {
                array = dict.CreateArray("CFBundleURLTypes");
            }

            if (ContainsURLScheme(array, urlScheme))
            {
                UnityEngine.Debug.Log($"TapSDK URL scheme already exists: {urlScheme}");
                return false;
            }

            var urlDict = array.AddDict();
            urlDict.SetString("CFBundleURLName", urlSchemeName);
            var schemes = urlDict.CreateArray("CFBundleURLSchemes");
            schemes.AddString(urlScheme);

            return true;
        }

        private static string GetInfoPlistPath(string pathToBuildProject, bool macos)
        {
            if (pathToBuildProject.EndsWith(".app"))
            {
                return $"{pathToBuildProject}/Contents/Info.plist";
            }

            var macosXCodePlistPath =
                $"{Path.GetDirectoryName(pathToBuildProject)}/{PlayerSettings.productName}/Info.plist";
            if (!File.Exists(macosXCodePlistPath))
            {
                macosXCodePlistPath = $"{pathToBuildProject}/{PlayerSettings.productName}/Info.plist";
            }

            return !macos
                ? pathToBuildProject + "/Info.plist"
                : macosXCodePlistPath;
        }

        private static bool ContainsURLScheme(PlistElementArray urlTypesArray, string urlScheme)
        {
            foreach (var urlType in urlTypesArray.values)
            {
                var urlTypeDict = urlType as PlistElementDict;
                if (urlTypeDict == null)
                {
                    continue;
                }

                if (!(urlTypeDict["CFBundleURLSchemes"] is PlistElementArray schemesArray))
                {
                    continue;
                }

                foreach (var scheme in schemesArray.values)
                {
                    var schemeString = scheme as PlistElementString;
                    if (schemeString != null && schemeString.AsString() == urlScheme)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetValueFromPlist(string infoPlistPath, string key)
        {
            if (infoPlistPath == null)
            {
                return null;
            }

            var dic = (Dictionary<string, object>)Plist.readPlist(infoPlistPath);
            return (from item in dic where item.Key.Equals(key) select (string)item.Value).FirstOrDefault();
        }

        public static void ExecutePodCommand(string command, string workingDirectory)
        {
            string podPath = FindPodPath();
            if (string.IsNullOrEmpty(podPath))
            {
                UnityEngine.Debug.LogError("[CocoaPods] search pod install path failed");
                return;
            }
            UnityEngine.Debug.Log("[CocoaPods] search pod install path :" + podPath);
            command = command.Replace("pod", podPath);
            command = "export LANG=en_US.UTF-8 && " + command;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"[CocoaPods] Output: {output}");

            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogError($"[CocoaPods] Error: {error}");

            if (process.ExitCode == 0)
                UnityEngine.Debug.Log($"[CocoaPods] Success: {command}");
            else
                UnityEngine.Debug.LogError($"[CocoaPods] Failed: {command} (Exit code: {process.ExitCode})");
        }

        private static string FindPodPath()
        {
            string whichResult = RunBashCommand("-l -c \"which pod\"");
            whichResult = whichResult.Replace("\n", "");
            if (!string.IsNullOrEmpty(whichResult) && File.Exists(whichResult))
            {
                UnityEngine.Debug.Log($"[PodFinder] Found pod at which result: {whichResult}");
                return whichResult;
            }

            string[] CommonPaths = new string[]
            {
                "/usr/local/bin",
                "/usr/bin",
                "/opt/homebrew/bin"
            };
            // 1. 先在常见路径查找 pod
            foreach (var path in CommonPaths)
            {
                string podPath = Path.Combine(path, "pod");
                if (File.Exists(podPath))
                {
                    UnityEngine.Debug.Log($"[PodFinder] Found pod at common path: {podPath}");
                    return podPath;
                }
            }
            // 2. 如果没找到，执行 gem environment 查找
            string gemEnvOutput = RunBashCommand("-l -c \"gem environment\"");

            if (string.IsNullOrEmpty(gemEnvOutput))
            {
                UnityEngine.Debug.LogWarning("[PodFinder] gem environment output is empty.");
                return null;
            }

            // 3. 解析 EXECUTABLE DIRECTORY
            string execDir = ParseGemEnvironment(gemEnvOutput, @"EXECUTABLE DIRECTORY:\s*(.+)");
            if (!string.IsNullOrEmpty(execDir))
            {
                string podPath = Path.Combine(execDir.Trim(), "pod");
                if (File.Exists(podPath))
                {
                    UnityEngine.Debug.Log($"[PodFinder] Found pod via EXECUTABLE DIRECTORY: {podPath}");
                    return podPath;
                }
            }

            // 4. 解析 GEM PATHS，尝试从每个路径下的 bin 文件夹查找 pod
            var gemPaths = ParseGemEnvironmentMultiple(gemEnvOutput, @"GEM PATHS:\s*((?:- .+\n)+)");
            if (gemPaths != null)
            {
                foreach (var gemPath in gemPaths)
                {
                    // 一般 pod 会在 bin 文件夹或同级目录中
                    string podPath1 = Path.Combine(gemPath.Trim(), "bin", "pod");
                    string podPath2 = Path.Combine(gemPath.Trim(), "pod"); // 备选路径

                    if (File.Exists(podPath1))
                    {
                        UnityEngine.Debug.Log($"[PodFinder] Found pod via GEM PATHS (bin): {podPath1}");
                        return podPath1;
                    }

                    if (File.Exists(podPath2))
                    {
                        UnityEngine.Debug.Log($"[PodFinder] Found pod via GEM PATHS: {podPath2}");
                        return podPath2;
                    }
                }
            }

            UnityEngine.Debug.LogWarning("[PodFinder] pod executable not found.");
            return null;
        }

        private static string RunBashCommand(string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string err = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(err))
                    {
                        UnityEngine.Debug.LogWarning($"[PodFinder] bash error: {err}");
                    }

                    return output;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PodFinder] Exception running bash command: {e}");
                return null;
            }
        }

        private static string ParseGemEnvironment(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }

        private static string[] ParseGemEnvironmentMultiple(string input, string pattern)
        {
            var match = Regex.Match(input, pattern, RegexOptions.Multiline);
            if (!match.Success || match.Groups.Count < 2) return null;

            string block = match.Groups[1].Value;

            // 每行格式是类似 "- /path/to/gem"
            var lines = block.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var paths = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("- "))
                {
                    paths.Add(trimmed.Substring(2).Trim());
                }
            }

            return paths.ToArray();
        }
#endif
    }

#if UNITY_IOS
    public sealed class TapSDKIOSURLSchemeBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            TapSDKCoreCompile.ClearIOSURLSchemesAddedByTapSDK();
        }
    }
#endif
}
