using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal
{
    internal static class TapEventStorage
    {
        internal const string PrefsFile = "prefs.json", EventsFile = "events.json";
        internal static string DataPath { get; private set; }
        internal static bool IsIsolated { get; private set; }
        internal static bool IsReady { get; private set; } = true;
        internal static string[] SharedIdentity { get; private set; }
        private static string storageRoot, configuredRoot, scope, client, region;
        private static Task recovery;
        private static readonly object gate = new object();

        internal static void Prepare()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            lock (gate)
            {
                IsReady = false;
                try
                {
                    string root = Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\', '/');
                    bool primary = StandaloneProcessIdentity.Register(Hash(root.ToUpperInvariant()));
                    Initialize(root, TapCoreStandalone.coreOptions.clientId,
                        TapCoreStandalone.coreOptions.region.ToString(), primary);
                    IsReady = true;
                }
                catch (Exception e)
                {
                    // Do not fall back to shared statistics files after isolation failed.
                    TapLog.Warning("[TapEvent] Storage unavailable; statistics startup skipped: " + e.Message);
                }
            }
#endif
        }

        // 独立测试宿主复用此入口，验证不依赖 Unity 的存储准备逻辑。
        internal static void Initialize(string root, string clientId, string regionName, bool primary)
        {
            string key = Hash(clientId + "\n" + regionName);
            if (DataPath != null)
            {
                if (root != configuredRoot || scope != key || IsIsolated == primary)
                    throw new InvalidOperationException("TapSDK storage configuration changed.");
                return;
            }
            storageRoot = Path.Combine(root, "tapdb_process");
            // 路径检查包含原子发布使用的临时文件后缀。
            CheckPath(Path.Combine(storageRoot, "recover", new string('0', 32), "owner.json") + "." + new string('0', 32) + ".tmp");
            CheckPath(Path.Combine(storageRoot, "identity-" + key + ".json") + "." + new string('0', 32) + ".tmp");
            Directory.CreateDirectory(storageRoot);
            if (!SafeDirectory(storageRoot)) throw new IOException("TapSDK storage link is not supported.");
            string legacyPrefs = CheckLegacyRoute(root, clientId, regionName, primary);
            string identityPath = Path.Combine(storageRoot, "identity-" + key + ".json");
            if (!File.Exists(identityPath))
            {
                try
                {
                    Publish(identityPath, JsonConvert.SerializeObject(ReadLegacyIdentity(legacyPrefs)));
                }
                catch (FileNotFoundException)
                {
                    if (!File.Exists(identityPath)) throw;
                    // The primary publishes identity before migrating legacy Prefs.
                    // Read and validate that published identity below instead of rebuilding it.
                    TapLog.Warning("TapSDK legacy Prefs moved concurrently; reading published installation identity.");
                }
            }
            var identity = JsonConvert.DeserializeObject<string[]>(ReadFile(identityPath));
            if (identity == null || identity.Length != 3 || Array.Exists(identity, string.IsNullOrWhiteSpace))
                throw new IOException("Invalid TapSDK installation identity; source retained.");
            string path = root;
            if (!primary)
            {
                string runs = Path.Combine(storageRoot, "runs");
                Directory.CreateDirectory(runs);
                if (!SafeDirectory(runs)) throw new IOException("TapSDK run directory is a link.");
                path = Path.Combine(storageRoot, "runs", Guid.NewGuid().ToString("N"));
                if (Path.Combine(path, PrefsFile).Length >= 260) throw new PathTooLongException("TapSDK run path is too long.");
                Directory.CreateDirectory(path);
                Publish(Path.Combine(path, "owner.json"), JsonConvert.SerializeObject(NewOwner(key)));
            }
            configuredRoot = root;
            client = clientId;
            region = regionName;
            scope = key;
            SharedIdentity = identity;
            IsIsolated = !primary;
            DataPath = path;
        }

        private static string CheckLegacyRoute(string root, string clientId, string regionName, bool primary)
        {
            // 旧文件只有 client 标识，不允许将其他 region 的数据重新归属。
            var paths = new List<string> { Path.Combine(storageRoot, "legacy-" + Hash(clientId) + ".json") };
            string prefs = Path.Combine(root, "tapdb_storage_v2_" + clientId);
            string oldPrefs = Path.Combine(root, "tapdb_storage_v2");
            bool migratesPrefs = !File.Exists(prefs) && File.Exists(oldPrefs);
            bool migratesEvents = !File.Exists(Path.Combine(root, "events_" + clientId + ".json")) && File.Exists(Path.Combine(root, "events.json"));
            // 额外进程不读取旧事件队列；仅在继承旧 Prefs 安装身份时检查其归属。
            if (migratesPrefs || (primary && migratesEvents))
                paths.Add(Path.Combine(storageRoot, "legacy-default.json"));
            string route = JsonConvert.SerializeObject(new[] { clientId, regionName });
            foreach (string path in paths)
                if (File.Exists(path) && ReadFile(path) != route)
                {
                    if (primary) throw new IOException("TapSDK legacy storage belongs to another client/region.");
                    return null; // 保留其他范围的旧数据，使用当前范围的安装身份。
                }
            foreach (string path in paths)
            {
                Publish(path, route);
                if (ReadFile(path) != route)
                {
                    if (primary) throw new IOException("TapSDK legacy storage route changed concurrently.");
                    return null;
                }
            }
            return migratesPrefs ? oldPrefs : File.Exists(prefs) ? prefs : null;
        }

        private static string[] ReadLegacyIdentity(string path)
        {
            var values = new JObject();
            if (path != null)
            {
                string json = ReadFile(path); // 读取或权限失败不能导致安装身份重建。
                try
                {
                    values = JObject.Parse(json);
                }
                catch (JsonException)
                {
                    // 与旧 Prefs 的损坏恢复保持一致；由首进程重置，额外进程不删除旧文件。
                    TapLog.Warning("TapSDK legacy Prefs is malformed; rebuilding installation identity.");
                }
            }
            string[] keys = { Identity.DEVICE_ID_KEY, Identity.PERSISTENT_ID_KEY, Identity.INSTALLATION_ID_KEY };
            return keys.Select((key, i) => {
                string value = values[key]?.Type == JTokenType.String ? (string)values[key] : null;
                return !string.IsNullOrWhiteSpace(value) ? value : i == 0 ? SystemInfo.deviceUniqueIdentifier : Guid.NewGuid().ToString();
            }).ToArray();
        }

        internal static void SeedIdentity()
        {
            if (SharedIdentity == null || IsIsolated) return;
            InstallationValue(0, Identity.DEVICE_ID_KEY);
            InstallationValue(1, Identity.PERSISTENT_ID_KEY);
            InstallationValue(2, Identity.INSTALLATION_ID_KEY);
        }

        internal static string InstallationValue(int index, string key)
        {
            string value = SharedIdentity[index];
            if (!IsIsolated && TapCoreStandalone.Prefs.Get<string>(key) != value) TapCoreStandalone.Prefs.Set(key, value);
            return value;
        }

        internal static Task RecoverAsync()
        {
            if (DataPath == null) return Task.CompletedTask;
            // 仅在统计开启且 Tracker 初始化后调用；历史文件读写和解析在后台执行。
            return recovery ?? (recovery = Task.Run(RecoverAllAsync));
        }

        private static async Task RecoverAllAsync()
        {
            try
            {
                // Coordinate consumers separately from each live process's private files.
                // Keep the lease through upload and cleanup; competing launches never wait.
                using (SharedCacheFile.Acquire(Path.Combine(storageRoot, "recovery"), 0))
                {
                    await RecoverDirectoriesAsync();
                }
            }
            catch (CacheBusyException)
            {
                TapLog.Warning("[TapEvent] History recovery is owned by another process.");
            }
            catch (Exception e)
            {
                TapLog.Warning("TapSDK history discovery failed: " + e.Message);
            }
        }

        private static async Task RecoverDirectoriesAsync()
        {
            string claims = Path.Combine(storageRoot, "recover");
            if (Directory.Exists(claims) && SafeDirectory(claims))
                foreach (string claim in Directory.GetDirectories(claims))
                {
                    try
                    {
                        if (!SafeDirectory(claim) || !ExpiredOwner(claim)) continue;
                        string data = Path.Combine(claim, "data");
                        if (Directory.Exists(data)) await RecoverPathAsync(data);
                        DeleteEmptyOwnerDirectory(claim);
                    }
                    catch (Exception e)
                    {
                        TapLog.Warning("TapSDK claimed history retained: " + e.Message);
                    }
                }
            string runs = Path.Combine(storageRoot, "runs");
            if (Directory.Exists(runs) && SafeDirectory(runs))
                foreach (string run in Directory.GetDirectories(runs)) await RecoverPathAsync(run);
        }

        private static async Task RecoverPathAsync(string source)
        {
            try
            {
                if (source == DataPath || !SafeDirectory(source) || !ExpiredOwner(source)) return;
                // 不递归删除，保留未知文件、目录和链接。
                if (!KnownContents(source)) return;
                string claims = Path.Combine(storageRoot, "recover");
                Directory.CreateDirectory(claims);
                if (!SafeDirectory(claims)) return;
                string claim = Path.Combine(claims, Guid.NewGuid().ToString("N"));
                string destination = Path.Combine(claim, "data");
                if (Path.Combine(destination, PrefsFile).Length >= 260) throw new PathTooLongException();
                Directory.CreateDirectory(claim);
                Publish(Path.Combine(claim, "owner.json"), JsonConvert.SerializeObject(NewOwner(scope)));
                try
                {
                    Directory.Move(source, destination);
                }
                catch (IOException)
                {
                    DeleteEmptyOwnerDirectory(claim); return;
                }
                var events = ReadHistory(destination);
                if (!await EventSender.SendHistoryAsync(events, region)) return;
                // 领取目录仅由当前消费者写入；部分清理失败可能重放，但不会改变数据归属。
                if (!KnownContents(destination)) return;
                foreach (string file in Directory.GetFiles(destination)) File.Delete(file);
                Directory.Delete(destination);
                DeleteEmptyOwnerDirectory(claim);
            }
            catch (Exception e)
            {
                TapLog.Warning("TapSDK history retained for next launch: " + e.Message);
            }
        }

        internal static List<Dictionary<string, object>> ReadHistory(string path)
        {
            var events = new List<Dictionary<string, object>>();
            string queue = Path.Combine(path, EventsFile);
            if (File.Exists(queue))
            {
                // 先严格校验结构，再用 MiniJSON 保留嵌套数组的原始类型。
                string json = ReadFile(queue);
                var array = JArray.Parse(json);
                if (array.Any(item => item.Type != JTokenType.Object)) throw new IOException("Invalid TapSDK history queue.");
                events.AddRange(((List<object>)Json.Deserialize(json)).Cast<Dictionary<string, object>>());
            }
            string prefs = Path.Combine(path, PrefsFile);
            if (File.Exists(prefs))
            {
                // 只读历史文件，不创建会启动写入任务及删除损坏文件的 Prefs。
                var values = JObject.Parse(ReadFile(prefs));
                var snapshot = values[PlayRecorder.SnapshotKey];
                if (snapshot != null)
                {
                    if (snapshot.Type != JTokenType.String) throw new IOException("Invalid TapSDK duration snapshot.");
                    var payload = JObject.Parse((string)snapshot);
                    var duration = payload["properties"]?["duration"];
                    if (duration == null || duration.Type != JTokenType.Integer || (long)duration < 0 ||
                        payload["name"]?.ToString() != "play_game" || string.IsNullOrWhiteSpace((string)payload["user_id"]))
                        throw new IOException("Incomplete TapSDK duration snapshot.");
                    if ((long)duration > 0) events.Add((Dictionary<string, object>)Json.Deserialize((string)snapshot));
                }
                // 新格式目录中的旧时长无法安全确定归属。
                if (values[PlayRecorder.PLAYED_DURATION_KEY] != null)
                    throw new IOException("Unexpected legacy duration; source retained.");
            }
            foreach (var item in events)
                if (!item.TryGetValue("client_id", out object id) || !Equals(id, client))
                    throw new IOException("TapSDK historical client does not match its owner.");
            return events;
        }

        private static Owner NewOwner(string ownerScope)
        {
            int pid = StandaloneProcessIdentity.CurrentPid;
            return new Owner { Version = 1, Scope = ownerScope, Pid = pid, StartedUtc = StandaloneProcessIdentity.StartedUtc(pid) };
        }

        private static bool ExpiredOwner(string directory)
        {
            var owner = JsonConvert.DeserializeObject<Owner>(ReadFile(Path.Combine(directory, "owner.json")));
            if (owner == null || owner.Version != 1 || owner.Scope != scope || owner.Pid <= 0 || owner.StartedUtc <= 0) return false;
            try
            {
                return StandaloneProcessIdentity.StartedUtc(owner.Pid) != owner.StartedUtc;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool SafeDirectory(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }

        private static bool KnownContents(string path)
        {
            return Directory.GetFileSystemEntries(path).All(file =>
                (Path.GetFileName(file) == "owner.json" || Path.GetFileName(file) == PrefsFile || Path.GetFileName(file) == EventsFile ||
                 Path.GetFileName(file) == PrefsFile + ".lock" || Path.GetFileName(file) == EventsFile + ".lock" ||
                 IsCacheTemporaryFile(Path.GetFileName(file))) &&
                (File.GetAttributes(file) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) == 0);
        }

        private static bool IsCacheTemporaryFile(string name)
        {
            string prefix;
            if (name.StartsWith(PrefsFile + ".", StringComparison.Ordinal)) prefix = PrefsFile + ".";
            else if (name.StartsWith(EventsFile + ".", StringComparison.Ordinal)) prefix = EventsFile + ".";
            else return false;

            // ReadHistory only consumes published files. Incomplete writes are removed
            // with their exited owner's directory only after a successful upload.
            return name.Length == prefix.Length + 32 + ".tmp".Length &&
                name.EndsWith(".tmp", StringComparison.Ordinal) &&
                Guid.TryParseExact(name.Substring(prefix.Length, 32), "N", out _);
        }

        private static void DeleteEmptyOwnerDirectory(string path)
        {
            string owner = Path.Combine(path, "owner.json");
            var entries = Directory.GetFileSystemEntries(path);
            if (entries.Length != 1 || entries[0] != owner || !SafeDirectory(owner)) return;
            File.Delete(owner);
            Directory.Delete(path);
        }

        private static string ReadFile(string path)
        {
            if (!SafeDirectory(path)) throw new IOException("TapSDK storage link is not supported.");
            return File.ReadAllText(path);
        }

        private static void CheckPath(string path)
        {
            if (path.Length >= 260) throw new PathTooLongException("TapSDK storage exceeds the Windows path budget.");
        }

        // 首次完整写入者发布成功；独立临时文件不会作为历史数据消费。
        private static void Publish(string path, string content)
        {
            if (File.Exists(path)) return;
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            CheckPath(temporary);
            try
            {
                File.WriteAllText(temporary, content);
                try
                {
                    File.Move(temporary, path);
                }
                catch (IOException)
                {
                    if (!File.Exists(path)) throw;
                }
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)), 0, 16).Replace("-", "").ToLowerInvariant();
        }

        private sealed class Owner
        {
            public int Version;
            public string Scope;
            public int Pid;
            public long StartedUtc;
        }
    }
}
