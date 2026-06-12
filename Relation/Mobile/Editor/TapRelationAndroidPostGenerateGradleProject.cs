using System.Collections.Generic;
using System.IO;
using System.Xml;
using TapSDK.Core.Editor;
using UnityEditor.Android;
using UnityEngine;

namespace TapSDK.Relation.Mobile.Editor
{
    public class TapRelationAndroidPostGenerateGradleProject : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespaceUri = "http://schemas.android.com/apk/res/android";
        private const string RouterActivityName = "com.taptap.sdk.relation.TapRelationRouterActivity";
        private const string RouterActivityTheme = "@android:style/Theme.Translucent.NoTitleBar";
        private static readonly string[] InviteHosts = { "invite_game", "invite_team" };

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = GetLauncherManifestPath(path);
            if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
            {
                Debug.LogWarning($"[TapSDK][Relation] AndroidManifest.xml not found under: {path}");
                return;
            }

            var clientIds = ExtractClientIds();
            if (clientIds.Count == 0)
            {
                Debug.LogWarning("[TapSDK][Relation] No clientId found, skip relation scheme intent-filter.");
                return;
            }

            EnsureRelationRouterIntentFilters(manifestPath, clientIds);
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

        private static HashSet<string> ExtractClientIds()
        {
            var clientIds = new HashSet<string>();
            var clientId = TapSDKCoreCompile.GetAppClientIdFromTDSInfo(Application.dataPath);
            if (string.IsNullOrEmpty(clientId))
            {
                Debug.LogWarning("[TapSDK][Relation] app.client_id not found in TDS-Info.json or taptap.client_id not found in fallback TDS-Info.plist, skip relation scheme intent-filter.");
                return clientIds;
            }

            clientIds.Add(clientId);
            return clientIds;
        }

        private static void EnsureRelationRouterIntentFilters(string manifestPath, HashSet<string> clientIds)
        {
            var document = new XmlDocument();
            document.Load(manifestPath);

            XmlElement manifestElement = document.DocumentElement;
            XmlElement applicationElement = manifestElement?.SelectSingleNode("application") as XmlElement;
            if (applicationElement == null)
            {
                Debug.LogWarning($"[TapSDK][Relation] <application> node not found in manifest: {manifestPath}");
                return;
            }

            XmlElement activityElement = FindActivity(applicationElement);
            bool changed = false;
            if (activityElement == null)
            {
                activityElement = document.CreateElement("activity");
                activityElement.SetAttribute("name", AndroidNamespaceUri, RouterActivityName);
                applicationElement.AppendChild(activityElement);
                changed = true;
            }

            if (activityElement.GetAttribute("exported", AndroidNamespaceUri) != "true")
            {
                activityElement.SetAttribute("exported", AndroidNamespaceUri, "true");
                changed = true;
            }

            if (activityElement.GetAttribute("noHistory", AndroidNamespaceUri) != "true")
            {
                activityElement.SetAttribute("noHistory", AndroidNamespaceUri, "true");
                changed = true;
            }

            if (activityElement.GetAttribute("theme", AndroidNamespaceUri) != RouterActivityTheme)
            {
                activityElement.SetAttribute("theme", AndroidNamespaceUri, RouterActivityTheme);
                changed = true;
            }

            foreach (string clientId in clientIds)
            {
                string scheme = "tds" + clientId;
                foreach (string host in InviteHosts)
                {
                    if (HasIntentFilter(activityElement, scheme, host))
                    {
                        continue;
                    }

                    activityElement.AppendChild(CreateIntentFilter(document, scheme, host));
                    changed = true;
                }
            }

            if (changed)
            {
                SaveXml(document, manifestPath);
            }
        }

        private static XmlElement FindActivity(XmlElement applicationElement)
        {
            foreach (XmlNode child in applicationElement.ChildNodes)
            {
                XmlElement element = child as XmlElement;
                if (element == null || element.Name != "activity")
                {
                    continue;
                }

                if (element.GetAttribute("name", AndroidNamespaceUri) == RouterActivityName)
                {
                    return element;
                }
            }

            return null;
        }

        private static bool HasIntentFilter(XmlElement activityElement, string scheme, string host)
        {
            foreach (XmlNode filterNode in activityElement.ChildNodes)
            {
                XmlElement filterElement = filterNode as XmlElement;
                if (filterElement == null || filterElement.Name != "intent-filter")
                {
                    continue;
                }

                foreach (XmlNode dataNode in filterElement.ChildNodes)
                {
                    XmlElement dataElement = dataNode as XmlElement;
                    if (dataElement == null || dataElement.Name != "data")
                    {
                        continue;
                    }

                    if (dataElement.GetAttribute("scheme", AndroidNamespaceUri) == scheme &&
                        dataElement.GetAttribute("host", AndroidNamespaceUri) == host)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static XmlElement CreateIntentFilter(XmlDocument document, string scheme, string host)
        {
            XmlElement intentFilter = document.CreateElement("intent-filter");

            XmlElement action = document.CreateElement("action");
            action.SetAttribute("name", AndroidNamespaceUri, "android.intent.action.VIEW");
            intentFilter.AppendChild(action);

            XmlElement defaultCategory = document.CreateElement("category");
            defaultCategory.SetAttribute("name", AndroidNamespaceUri, "android.intent.category.DEFAULT");
            intentFilter.AppendChild(defaultCategory);

            XmlElement browsableCategory = document.CreateElement("category");
            browsableCategory.SetAttribute("name", AndroidNamespaceUri, "android.intent.category.BROWSABLE");
            intentFilter.AppendChild(browsableCategory);

            XmlElement data = document.CreateElement("data");
            data.SetAttribute("scheme", AndroidNamespaceUri, scheme);
            data.SetAttribute("host", AndroidNamespaceUri, host);
            intentFilter.AppendChild(data);

            return intentFilter;
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
