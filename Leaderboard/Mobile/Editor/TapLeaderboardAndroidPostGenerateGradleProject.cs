using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace TapSDK.Leaderboard.Mobile.Editor
{
    /// <summary>
    /// 为 Leaderboard 分享能力自动补充 Android FileProvider 配置。
    /// </summary>
    public class TapLeaderboardAndroidPostGenerateGradleProject : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespaceUri = "http://schemas.android.com/apk/res/android";
        private const string FileProviderClassName = "com.taptap.sdk.leaderboard.androidx.provider.TapLeaderboardFileProvider";
        private const string FileProviderAuthorities = "${applicationId}.tapsdk.leaderboard.fileprovider";
        private const string FileProviderPathsMetaName = "android.support.FILE_PROVIDER_PATHS";
        private const string FileProviderPathsResource = "@xml/tap_leaderboard_file_paths";
        private const string FilePathsFileName = "tap_leaderboard_file_paths.xml";
        private const string DefaultSharedImagesPathName = "shared_images";
        private const string SharedImagesRelativePath = "shared_images/";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = GetLauncherManifestPath(path);
            if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
            {
                Debug.LogWarning($"[TapSDK][Leaderboard] AndroidManifest.xml not found under: {path}");
                return;
            }

            string resXmlDirectory = Path.Combine(Path.GetDirectoryName(manifestPath) ?? string.Empty, "res", "xml");
            EnsureFileProvider(manifestPath);
            EnsureFilePathsXml(resXmlDirectory);
        }

        private static string GetLauncherManifestPath(string exportPath)
        {
            string[] candidates =
            {
                Path.Combine(exportPath, "launcher", "src", "main", "AndroidManifest.xml"),
                Path.Combine(exportPath, "src", "main", "AndroidManifest.xml")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void EnsureFilePathsXml(string resXmlDirectory)
        {
            Directory.CreateDirectory(resXmlDirectory);
            string filePathsPath = Path.Combine(resXmlDirectory, FilePathsFileName);

            if (!File.Exists(filePathsPath))
            {
                File.WriteAllText(filePathsPath, CreateFilePathsContent());
                return;
            }

            var document = new XmlDocument();
            document.Load(filePathsPath);

            XmlElement pathsElement = document.DocumentElement;
            if (pathsElement == null || pathsElement.Name != "paths")
            {
                File.WriteAllText(filePathsPath, CreateFilePathsContent());
                return;
            }

            if (HasSharedImagesPath(pathsElement))
            {
                return;
            }

            XmlElement filesPathElement = document.CreateElement("files-path");
            filesPathElement.SetAttribute("name", DefaultSharedImagesPathName);
            filesPathElement.SetAttribute("path", SharedImagesRelativePath);
            pathsElement.AppendChild(filesPathElement);
            SaveXml(document, filePathsPath);
        }

        private static string CreateFilePathsContent()
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                   "<paths>\n" +
                   "    <!-- Allow leaderboard share images stored in files/shared_images/. -->\n" +
                   "    <files-path name=\"" + DefaultSharedImagesPathName + "\" path=\"" + SharedImagesRelativePath + "\" />\n" +
                   "</paths>\n";
        }

        private static bool HasSharedImagesPath(XmlElement pathsElement)
        {
            foreach (XmlNode child in pathsElement.ChildNodes)
            {
                XmlElement element = child as XmlElement;
                if (element == null || element.Name != "files-path")
                {
                    continue;
                }

                if (element.GetAttribute("path") == SharedImagesRelativePath)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFileProvider(string manifestPath)
        {
            var document = new XmlDocument();
            document.Load(manifestPath);

            XmlElement manifestElement = document.DocumentElement;
            XmlNode applicationNode = manifestElement != null ? manifestElement.SelectSingleNode("application") : null;
            XmlElement applicationElement = applicationNode as XmlElement;
            if (applicationElement == null)
            {
                Debug.LogWarning($"[TapSDK][Leaderboard] <application> node not found in manifest: {manifestPath}");
                return;
            }

            XmlElement providerElement = FindFileProvider(applicationElement);
            bool manifestChanged = false;
            if (providerElement == null)
            {
                providerElement = document.CreateElement("provider");
                providerElement.SetAttribute("name", AndroidNamespaceUri, FileProviderClassName);
                providerElement.SetAttribute("authorities", AndroidNamespaceUri, FileProviderAuthorities);
                providerElement.SetAttribute("exported", AndroidNamespaceUri, "false");
                providerElement.SetAttribute("grantUriPermissions", AndroidNamespaceUri, "true");
                applicationElement.AppendChild(providerElement);
                manifestChanged = true;
            }

            if (EnsureFileProviderMetaData(document, providerElement))
            {
                manifestChanged = true;
            }

            if (manifestChanged)
            {
                SaveXml(document, manifestPath);
            }
        }

        private static XmlElement FindFileProvider(XmlElement applicationElement)
        {
            foreach (XmlNode child in applicationElement.ChildNodes)
            {
                XmlElement element = child as XmlElement;
                if (element == null || element.Name != "provider")
                {
                    continue;
                }

                string authorities = element.GetAttribute("authorities", AndroidNamespaceUri);
                if (authorities == FileProviderAuthorities)
                {
                    return element;
                }
            }

            return null;
        }

        private static bool EnsureFileProviderMetaData(XmlDocument document, XmlElement providerElement)
        {
            foreach (XmlNode child in providerElement.ChildNodes)
            {
                XmlElement element = child as XmlElement;
                if (element == null || element.Name != "meta-data")
                {
                    continue;
                }

                string metaName = element.GetAttribute("name", AndroidNamespaceUri);
                if (metaName != FileProviderPathsMetaName)
                {
                    continue;
                }

                string filePathsResource = element.GetAttribute("resource", AndroidNamespaceUri);
                if (filePathsResource != FileProviderPathsResource)
                {
                    element.SetAttribute("resource", AndroidNamespaceUri, FileProviderPathsResource);
                    return true;
                }

                return false;
            }

            XmlElement metaDataElement = document.CreateElement("meta-data");
            metaDataElement.SetAttribute("name", AndroidNamespaceUri, FileProviderPathsMetaName);
            metaDataElement.SetAttribute("resource", AndroidNamespaceUri, FileProviderPathsResource);
            providerElement.AppendChild(metaDataElement);
            return true;
        }

        private static void SaveXml(XmlDocument document, string path)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                document.Save(writer);
            }
        }
    }
}
