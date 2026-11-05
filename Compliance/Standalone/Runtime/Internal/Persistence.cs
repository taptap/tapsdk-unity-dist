using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using System.IO;
using TapSDK.Core.Internal.Utils;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.Core;
using UnityEngine;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Compliance.Internal 
{
    /// <summary>
    /// 通用 JSON 序列化工具
    /// </summary>
    internal class Persistence 
    {
        private sealed class FileState
        {
            internal readonly SemaphoreSlim Operations = new SemaphoreSlim(1, 1);
            internal long Generation;
            internal volatile bool Invalidated;
        }
        private static readonly ConcurrentDictionary<string, FileState> States = new ConcurrentDictionary<string, FileState>();
        private readonly string _filePath;
        private readonly string invalidationPath;
        private readonly SharedCacheStorage storage;
        private readonly FileState state;
        private readonly long generation;
        internal Persistence(string path) 
        {
            string new_cacheDir = Path.Combine(Application.persistentDataPath, Config.ANTI_ADDICTION_DIR);
            if (!string.IsNullOrEmpty(TapTapComplianceManager.ClientId)){
                new_cacheDir = Path.Combine(Application.persistentDataPath, Config.ANTI_ADDICTION_DIR + "_" + TapTapComplianceManager.ClientId);
            }
            storage = new SharedCacheStorage(
                Path.Combine(Application.persistentDataPath, Config.ANTI_ADDICTION_DIR), new_cacheDir, true);
            _filePath = Path.Combine(new_cacheDir, path);
            // Bookkeeping lives outside the directory being migrated. It must not create that target early.
            invalidationPath = Path.Combine(new_cacheDir + ".invalidations", path + ".invalid");
            state = States.GetOrAdd(_filePath, _ => new FileState());
            lock (state) generation = state.Generation;
        }

        internal async Task<T> Load<T>() where T : class
        {
            if (state.Invalidated || File.Exists(invalidationPath)) return null;
            await state.Operations.WaitAsync().ConfigureAwait(false);
            try
            {
                await storage.PrepareAsync().ConfigureAwait(false);
                using (await SharedCacheFile.AcquireAsync(_filePath).ConfigureAwait(false))
                {
                    if (state.Invalidated || File.Exists(invalidationPath) || !File.Exists(_filePath)) return null;
                    string text = await SharedCacheFile.ReadTextUnderLockAsync(_filePath).ConfigureAwait(false);
                    try { return JsonConvert.DeserializeObject<T>(text); }
                    catch (Exception e)
                    {
                        TapLog.Error(e);
                        File.Delete(_filePath); // Still holding the read/parse/delete transaction.
                        return null;
                    }
                }
            }
            finally { state.Operations.Release(); }
        }

        internal async Task Save<T>(T obj)
        {
            if (obj == null) { TapLog.Error("Saved object is null."); return; }
            string text = JsonConvert.SerializeObject(obj);
            await state.Operations.WaitAsync().ConfigureAwait(false);
            try
            {
                if (generation != Interlocked.Read(ref state.Generation)) return;
                await storage.PrepareAsync().ConfigureAwait(false);
                await SharedCacheFile.WriteBytesAsync(_filePath, Encoding.UTF8.GetBytes(text)).ConfigureAwait(false);
                // Logout can invalidate a save while its IO is in flight. Never remove its tombstone.
                lock (state)
                {
                    if (generation == state.Generation)
                    {
                        if (File.Exists(invalidationPath)) File.Delete(invalidationPath);
                        state.Invalidated = false;
                    }
                }
            }
            finally { state.Operations.Release(); }
        }

        internal void Delete()
        {
            lock (state)
            {
                if (generation != state.Generation) return;
                Interlocked.Increment(ref state.Generation);
                state.Invalidated = true;
                try
                {
                    // Small synchronous tombstone, no lock wait. Normal single-process exit remains synchronous.
                    Directory.CreateDirectory(Path.GetDirectoryName(invalidationPath));
                    File.WriteAllText(invalidationPath, "invalid");
                }
                catch (Exception e)
                {
                    TapLog.Error("[Compliance] Cache invalidation could not be persisted: " + e.Message);
                }
                // Queue cleanup before a newly constructed writer can observe this generation.
                _ = DeleteInvalidatedAsync();
            }
        }

        private async Task DeleteInvalidatedAsync()
        {
            await state.Operations.WaitAsync().ConfigureAwait(false);
            try
            {
                await storage.PrepareAsync().ConfigureAwait(false);
                await SharedCacheFile.DeleteAsync(_filePath).ConfigureAwait(false);
                // Keep the tombstone until a new authoritative Save succeeds, including across restarts.
            }
            catch (Exception e)
            {
                TapLog.Error("[Compliance] Cache cleanup deferred; invalidation retained: " + e.Message);
            }
            finally { state.Operations.Release(); }
        }
    }
}
