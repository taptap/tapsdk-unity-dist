using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Diagnostics;
using System.Text.RegularExpressions;



#if UNITY_IOS
using System;
using Google;
using UnityEditor.iOS.Xcode;

#endif

namespace TapSDK.Core.Editor
{
    public static class TapSDKCoreCompile
    {
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
            string moduleName, string[] bundleNames, string target, string projPath, PBXProject proj, string podSpecName = "")
        {

            var resourcePath = Path.Combine(path, resourceName);

            var parentFolder = Directory.GetParent(appDataPath).FullName;

            UnityEngine.Debug.Log($"ProjectFolder path:{parentFolder}" + " resourcePath： " + resourcePath + " parentFolder: " + parentFolder);

            if (Directory.Exists(resourcePath))
            {
                Directory.Delete(resourcePath, true);
            }

            var podSpecPath = Path.Combine(path + "/Pods", podSpecName);
            //使用 cocospod 远程依赖
            if (podSpecName != null && podSpecName.Length > 0 && Directory.Exists(podSpecPath))
            {
                resourcePath = Path.Combine(path + "/Pods", podSpecName + "/Frameworks");
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
            // #if UNITY_2020_1_OR_NEWER
            //             var macosXCodePlistPath =
            //                 $"{pathToBuildProject}/{PlayerSettings.productName}/Info.plist";
            // #elif UNITY_2019_1_OR_NEWER
            //             var macosXCodePlistPath =
            //                 $"{Path.GetDirectoryName(pathToBuildProject)}/{PlayerSettings.productName}/Info.plist";
            // #endif

            string plistPath;

            if (pathToBuildProject.EndsWith(".app"))
            {
                plistPath = $"{pathToBuildProject}/Contents/Info.plist";
            }
            else
            {
                var macosXCodePlistPath =
                    $"{Path.GetDirectoryName(pathToBuildProject)}/{PlayerSettings.productName}/Info.plist";
                if (!File.Exists(macosXCodePlistPath))
                {
                    macosXCodePlistPath = $"{pathToBuildProject}/{PlayerSettings.productName}/Info.plist";
                }

                plistPath = !macos
                    ? pathToBuildProject + "/Info.plist"
                    : macosXCodePlistPath;
            }

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

            if (string.IsNullOrEmpty(infoPlistPath)) return false;
            var dic = (Dictionary<string, object>)Plist.readPlist(infoPlistPath);
            var taptapId = "";

            foreach (var item in dic)
            {
                if (item.Key.Equals("taptap"))
                {
                    var taptapDic = (Dictionary<string, object>)item.Value;
                    foreach (var taptapItem in taptapDic.Where(taptapItem => taptapItem.Key.Equals("client_id")))
                    {
                        taptapId = (string)taptapItem.Value;
                    }
                }
                else
                {
                    rootDic.SetString(item.Key, item.Value.ToString());
                }
            }

            //添加url
            var dict = plist.root.AsDict();
            if (!(dict["CFBundleURLTypes"] is PlistElementArray array))
            {
                array = dict.CreateArray("CFBundleURLTypes");
            }

            if (!macos)
            {
                var dict2 = array.AddDict();
                dict2.SetString("CFBundleURLName", "TapTap");
                var array2 = dict2.CreateArray("CFBundleURLSchemes");
                array2.AddString($"tt{taptapId}");
            }
            else
            {
                var dict2 = array.AddDict();
                dict2.SetString("CFBundleURLName", "TapWeb");
                var array2 = dict2.CreateArray("CFBundleURLSchemes");
                array2.AddString($"open-taptap-{taptapId}");
            }

            UnityEngine.Debug.Log("TapSDK change plist Success");
            File.WriteAllText(plistPath, plist.WriteToString());
            return true;
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
}