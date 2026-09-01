using System.Collections.Generic;
using System.IO;
using System;
using System.Xml;
using UnityEngine;
using UnityEditor;

namespace TapSDK.Core.Editor {
    public class LinkedAssembly {
        public string Fullname { get; set; }
        public string[] Types { get; set; }
    }

    public class LinkXMLGenerator {
        private const int MaxDeleteAttempts = 3;
        private static readonly HashSet<string> GeneratedPaths = new HashSet<string>();
        private static readonly Dictionary<string, int> DeleteAttempts = new Dictionary<string, int>();

        public static void Generate(string path, IEnumerable<LinkedAssembly> assemblies) {
            DirectoryInfo parent = Directory.GetParent(path);
            if (!parent.Exists) {
                Directory.CreateDirectory(parent.FullName);
            }

            XmlDocument doc = new XmlDocument();

            XmlNode rootNode = doc.CreateElement("linker");
            doc.AppendChild(rootNode);

            foreach (LinkedAssembly assembly in assemblies) {
                XmlNode assemblyNode = doc.CreateElement("assembly");

                XmlAttribute fullnameAttr = doc.CreateAttribute("fullname");
                fullnameAttr.Value = assembly.Fullname;
                assemblyNode.Attributes.Append(fullnameAttr);

                if (assembly.Types?.Length > 0) {
                    foreach (string type in assembly.Types) {
                        XmlNode typeNode = doc.CreateElement("type");
                        XmlAttribute typeFullnameAttr = doc.CreateAttribute("fullname");
                        typeFullnameAttr.Value = type;
                        typeNode.Attributes.Append(typeFullnameAttr);

                        XmlAttribute typePreserveAttr = doc.CreateAttribute("preserve");
                        typePreserveAttr.Value = "all";
                        typeNode.Attributes.Append(typePreserveAttr);

                        assemblyNode.AppendChild(typeNode);
                    }
                } else {
                    XmlAttribute preserveAttr = doc.CreateAttribute("preserve");
                    preserveAttr.Value = "all";
                    assemblyNode.Attributes.Append(preserveAttr);
                }

                rootNode.AppendChild(assemblyNode);
            }

            doc.Save(path);
            GeneratedPaths.Add(path);
            DeleteAttempts[path] = 0;
            string assetPath = GetAssetPath(path);
            if (assetPath != null) {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            } else {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Generate {path} done.");
            Debug.Log(doc.OuterXml);
        }

        /// <summary>
        /// Deletes a generated link.xml immediately for compatibility with existing build scripts.
        /// </summary>
        public static void Delete(string path) {
            if (File.Exists(path)) {
                File.Delete(path);
                File.Delete(path + ".meta");
            }

            GeneratedPaths.Remove(path);
            DeleteAttempts.Remove(path);
            Debug.Log($"Delete {path} done.");
        }

        public static void ScheduleDeleteGeneratedFiles() {
            EditorApplication.delayCall -= DeleteGeneratedFiles;
            EditorApplication.delayCall += DeleteGeneratedFiles;
        }

        public static void DeleteGeneratedFiles() {
            EditorApplication.delayCall -= DeleteGeneratedFiles;
            bool deletedAny = false;
            bool shouldRetry = false;
            foreach (string path in new List<string>(GeneratedPaths)) {
                try {
                    string metaPath = path + ".meta";
                    if (!File.Exists(path) && !File.Exists(metaPath)) {
                        GeneratedPaths.Remove(path);
                        DeleteAttempts.Remove(path);
                        continue;
                    }

                    File.Delete(path);
                    File.Delete(metaPath);
                    GeneratedPaths.Remove(path);
                    DeleteAttempts.Remove(path);
                    deletedAny = true;
                    Debug.Log($"Delete {path} done.");
                } catch (Exception exception) {
                    int attempt = DeleteAttempts.TryGetValue(path, out int previousAttempts)
                        ? previousAttempts + 1
                        : 1;
                    DeleteAttempts[path] = attempt;
                    bool willRetry = attempt < MaxDeleteAttempts;
                    shouldRetry |= willRetry;
                    string recovery = willRetry
                        ? "Retry scheduled."
                        : "Retry limit reached; path remains tracked until the next build "
                            + "or explicit DeleteGeneratedFiles call.";
                    Debug.LogWarning(
                        $"Delete generated link.xml failed (attempt {attempt}/{MaxDeleteAttempts}): "
                        + $"{path}. {exception.Message} {recovery}");
                }
            }

            if (deletedAny) {
                try {
                    EditorApplication.delayCall -= RetryAssetDatabaseRefresh;
                    AssetDatabase.Refresh();
                } catch (Exception exception) {
                    Debug.LogWarning($"Refresh assets after deleting generated link.xml failed: {exception.Message}");
                    EditorApplication.delayCall -= RetryAssetDatabaseRefresh;
                    EditorApplication.delayCall += RetryAssetDatabaseRefresh;
                }
            }

            if (shouldRetry) {
                ScheduleDeleteGeneratedFiles();
            }
        }

        private static void RetryAssetDatabaseRefresh() {
            EditorApplication.delayCall -= RetryAssetDatabaseRefresh;
            try {
                AssetDatabase.Refresh();
            } catch (Exception exception) {
                Debug.LogWarning(
                    "Final asset refresh retry after deleting generated link.xml failed; "
                    + $"Unity may retain stale asset state until its next refresh. {exception.Message}");
            }
        }

        private static string GetAssetPath(string path) {
            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            string assetPrefix = dataPath + "/";
            return fullPath.StartsWith(assetPrefix, StringComparison.Ordinal)
                ? "Assets/" + fullPath.Substring(assetPrefix.Length)
                : null;
        }
    }
}
