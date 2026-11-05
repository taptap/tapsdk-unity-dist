using System.Threading.Tasks;
using TapSDK.Achievement.Internal.Util;
using System;
using System.Text;
using System.IO;
using TapSDK.Core.Internal.Utils;
using Newtonsoft.Json;
using UnityEngine;
using TapSDK.Login;
using System.Security.Cryptography;
using System.Collections.Generic;
using TapSDK.Achievement.Internal.Model;
using System.Threading;
using TapSDK.Core;

namespace TapTap.Achievement.Standalone.Internal
{
    public class TapAchievementStore
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        // Server-final operations must not be uploaded again if local cleanup fails. Guarded by semaphoreSlim.
        // This is process-local: a crash before durable cleanup still requires server-side idempotency.
        private static readonly Dictionary<string, string> completedOperations = new Dictionary<string, string>();

        private TapAchievementStore()
        {

        }

        public static async Task<string> getFilePath()
        {
            TapTapAccount tapAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
            if (tapAccount == null)
            {
                return null;
            }
            string openId = tapAccount.openId;
            if (string.IsNullOrEmpty(openId))
            {
                return null;
            }
            string fileName = EncryptString(openId) + ".json";
            
            string newDirecotryName = "TapAchievement";
            if (TapTapSDK.taptapSdkOptions != null && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)) {
                newDirecotryName = newDirecotryName + "_" + TapTapSDK.taptapSdkOptions.clientId;
            }
            string newDirectoryPath = Path.Combine(Application.persistentDataPath, newDirecotryName);
            // 兼容旧版本数据
            await SharedCacheFile.MigrateAsync(
                Path.Combine(Application.persistentDataPath, "TapAchievement"), newDirectoryPath, true);
            string path = Path.Combine(Application.persistentDataPath, newDirecotryName, "localAchievementCache", fileName);
            return path;
        }

        public async static Task<List<TapAchievementStoreBean>> getAll()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await getAllInner(await getFilePath());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        internal static async Task<List<TapAchievementStoreBean>> GetPendingAsync(string filePath)
        {
            await semaphoreSlim.WaitAsync();
            try { return await getAllInner(filePath); }
            finally { semaphoreSlim.Release(); }
        }

        internal static async Task CompleteAsync(string uuid, string filePath)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                completedOperations[uuid] = filePath;
                // Reads filter completed operations and retry their cleanup before exposing pending work.
                await getAllInner(filePath);
            }
            catch (Exception e)
            {
                TapAchievementLog.Log("Completed achievement cleanup deferred: " + e.Message);
            }
            finally { semaphoreSlim.Release(); }
        }

        public async static Task SaveAll(List<TapAchievementStoreBean> data)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await SaveAllInner(data, await getFilePath());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async static Task Save(TapAchievementStoreBean bean)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                string filePath = await getFilePath();
                List<TapAchievementStoreBean> currentAll = await getAllInner(filePath) ?? new List<TapAchievementStoreBean>();
                currentAll.Add(bean);
                await SaveAllInner(currentAll, filePath);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async static Task Delete(string uuid)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                TapAchievementLog.Log("Delete uuid = " + uuid);
                string filePath = await getFilePath();
                List<TapAchievementStoreBean> currentAll = await getAllInner(filePath) ?? new List<TapAchievementStoreBean>();
                currentAll.RemoveAll(x => x.UUID == uuid);
                await SaveAllInner(currentAll, filePath);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async static Task<List<TapAchievementStoreBean>> getAllInner(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            List<TapAchievementStoreBean> pending;
            using (await SharedCacheFile.AcquireAsync(filePath))
            {
                if (!File.Exists(filePath)) return null;
                // Pending operations are not a disposable cache. A read/parse failure must not become an empty queue.
                string text = await SharedCacheFile.ReadTextUnderLockAsync(filePath).ConfigureAwait(false);
                pending = JsonConvert.DeserializeObject<List<TapAchievementStoreBean>>(text)
                    ?? throw new InvalidDataException("[Achievement] Invalid pending-operation cache.");
            }
            if (pending.RemoveAll(x => completedOperations.TryGetValue(x.UUID, out string path) && path == filePath) > 0)
            {
                try { await SaveAllInner(pending, filePath); }
                catch (Exception e) { TapAchievementLog.Log("Completed achievement cleanup deferred: " + e.Message); }
            }
            return pending;
        }

        private async static Task SaveAllInner(List<TapAchievementStoreBean> data, string filePath)
        {
            if (data == null) return;
            string text = JsonConvert.SerializeObject(data);
            if (string.IsNullOrEmpty(filePath))
                throw new InvalidOperationException("[Achievement] Account unavailable while saving pending operations.");
            await SharedCacheFile.WriteBytesAsync(filePath, Encoding.UTF8.GetBytes(text));
            foreach (string uuid in new List<string>(completedOperations.Keys))
                if (completedOperations[uuid] == filePath && !data.Exists(x => x.UUID == uuid))
                    completedOperations.Remove(uuid);
        }
        private static string EncryptString(string str)
        {
            var md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            var sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }

            // 返回加密的字符串
            return sb.ToString();
        }
    }
}
