#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Internal.Utils
{
    // All participants lock a stable sidecar; replacing the data file must not replace the lock.
    internal static class SharedCacheFile
    {
        private const int TimeoutMilliseconds = 5000;
        private const int RetryMilliseconds = 25;

        // A zero timeout makes one attempt, for optional caches read during synchronous initialization.
        internal static IDisposable Acquire(string path, int timeoutMilliseconds = TimeoutMilliseconds)
        {
            string lockPath = PrepareLockPath(path);
            var timer = Stopwatch.StartNew();
            do
            {
                IDisposable lease = TryAcquire(lockPath);
                if (lease != null) return lease;
                if (timer.ElapsedMilliseconds >= timeoutMilliseconds) break;
                Thread.Sleep(RetryMilliseconds);
            } while (timer.ElapsedMilliseconds < timeoutMilliseconds);
            throw new CacheBusyException();
        }

        internal static async Task<IDisposable> AcquireAsync(string path)
        {
            string lockPath = PrepareLockPath(path);
            var timer = Stopwatch.StartNew();
            do
            {
                IDisposable lease = TryAcquire(lockPath);
                if (lease != null) return lease;
                await Task.Delay(RetryMilliseconds).ConfigureAwait(false);
            } while (timer.ElapsedMilliseconds < TimeoutMilliseconds);
            throw new CacheBusyException();
        }

        private static string PrepareLockPath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            return fullPath + ".lock";
        }

        private static IDisposable TryAcquire(string lockPath)
        {
            try
            {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
#else
                // Unix: FileShare.None is not a portable process lock across Unity Mono versions.
                var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                int fd = stream.SafeFileHandle.DangerousGetHandle().ToInt32();
                if (Flock(fd, 6 /* LOCK_EX | LOCK_NB */) == 0) return stream;
                int error = Marshal.GetLastWin32Error();
                stream.Dispose(); // Closing the handle releases flock on any continuation thread.
                if (error == 35 || error == 11 || error == 4) return null; // EWOULDBLOCK (macOS/Linux Editor) / EINTR
                throw new IOException("[SharedCacheFile] Cannot lock cache.", new Win32Exception(error));
#endif
            }
            catch (IOException e)
            {
                // Windows sharing violation, or raw Unix EWOULDBLOCK from runtimes that lock on open.
                if ((e.HResult & 0xffff) == 32 || (e.HResult & 0xffff) == 33 || e.HResult == 35 || e.HResult == 11)
                    return null;
                throw;
            }
        }

        internal static string ReadText(string path, int lockTimeoutMilliseconds = TimeoutMilliseconds)
        {
            using (Acquire(path, lockTimeoutMilliseconds))
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
        }

        // Caller owns the file lease across this asynchronous read and any parse/cleanup.
        internal static async Task<string> ReadTextUnderLockAsync(string path)
        {
            using (var reader = File.OpenText(path))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        internal static void WriteText(string path, string text)
        {
            using (Acquire(path))
            {
                WriteTextUnderLock(path, text);
            }
        }

        // Caller must exclusively own a process-private cache, or hold Acquire(path)
        // for the complete read-modify-write transaction on a shared cache.
        internal static void WriteTextUnderLock(string path, string text)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                Replace(temporary, path);
            }
            finally
            {
                DeleteTemporary(temporary);
            }
        }

        internal static async Task WriteBytesAsync(string path, byte[] bytes)
        {
            using (await AcquireAsync(path).ConfigureAwait(false))
            {
                string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 4096, true))
                    {
                        await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }
                    Replace(temporary, path);
                }
                finally
                {
                    DeleteTemporary(temporary);
                }
            }
        }

        internal static void Delete(string path)
        {
            using (Acquire(path)) File.Delete(path);
        }

        internal static async Task DeleteAsync(string path)
        {
            using (await AcquireAsync(path).ConfigureAwait(false)) File.Delete(path);
        }

        internal static void Migrate(string source, string target, bool directory,
            int lockTimeoutMilliseconds = TimeoutMilliseconds)
        {
            if (Path.GetFullPath(source) == Path.GetFullPath(target)) return;
            // Absence of legacy data proves readiness. An existing target alone does not.
            if (directory ? !Directory.Exists(source) : !File.Exists(source)) return;
            // The lock lives alongside the old path, never inside a directory being moved.
            // Recheck below after acquiring it: another process may have completed the migration.
            using (Acquire(source + ".migration", lockTimeoutMilliseconds))
            {
                if (directory)
                {
                    if (!Directory.Exists(source)) return;
                    if (Directory.Exists(target) || File.Exists(target)) throw new CacheMigrationConflictException();
                    Directory.Move(source, target);
                }
                else
                {
                    if (!File.Exists(source)) return;
                    if (File.Exists(target) || Directory.Exists(target)) throw new CacheMigrationConflictException();
                    File.Move(source, target);
                }
            }
        }

        // No contention: completes inline, preserving the normal single-process initialization order.
        // Contention: only pure IO continuations run off-context; callers retain their own context.
        internal static async Task MigrateAsync(string source, string target, bool directory)
        {
            var timer = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    Migrate(source, target, directory, lockTimeoutMilliseconds: 0);
                    return;
                }
                catch (CacheBusyException)
                {
                    if (timer.ElapsedMilliseconds >= TimeoutMilliseconds) throw;
                    await Task.Delay(RetryMilliseconds).ConfigureAwait(false);
                }
            }
        }

        private static void Replace(string source, string target)
        {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
            if (!MoveFileEx(source, target, 1 /* MOVEFILE_REPLACE_EXISTING */)) throw NativeError("replace cache");
#else
            if (Rename(source, target) != 0) throw NativeError("replace cache");
#endif
        }

        private static IOException NativeError(string operation)
        {
            return new IOException("[SharedCacheFile] Failed to " + operation + ".",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }

        private static void DeleteTemporary(string path)
        {
            try { File.Delete(path); }
            catch (Exception e)
            {
                // Keep the original write error; an orphan temp file is never loaded as cache.
                TapLog.Warning("[SharedCacheFile] Temporary file cleanup failed: " + e.Message);
            }
        }

#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
        [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveFileEx(string source, string target, int flags);
#else
        [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
        private static extern int Flock(int fd, int operation);
        [DllImport("libc", EntryPoint = "rename", SetLastError = true)]
        private static extern int Rename(string source, string target);
#endif
    }

    internal sealed class CacheBusyException : IOException
    {
        internal CacheBusyException() : base("[SharedCacheFile] Timed out waiting for cache lock.") { }
    }

    internal sealed class CacheMigrationConflictException : IOException
    {
        internal CacheMigrationConflictException() : base("[SharedCacheFile] Legacy and current caches both exist; preserving both until recovery.") { }
    }

    // Bounded deferral for native calls while its data directory is being prepared.
    internal sealed class CacheReadyActions
    {
        private readonly object gate = new object();
        private readonly System.Collections.Generic.Queue<Action> pending = new System.Collections.Generic.Queue<Action>();
        private readonly int capacity;
        // Five fixed native state channels; overflow must not lose the final login/module state.
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<long, Action>> overflowState =
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.KeyValuePair<long, Action>>();
        private long sequence;
        private bool ready;
        private bool stopped;
        private int generation;

        internal CacheReadyActions(int capacity) { this.capacity = capacity; }

        internal int Begin()
        {
            lock (gate)
            {
                if (generation != 0) { pending.Clear(); overflowState.Clear(); }
                ready = false;
                stopped = false;
                return ++generation;
            }
        }

        internal bool IsCurrent(int version)
        {
            lock (gate) return !stopped && version == generation;
        }

        internal void Invoke(Action action, string stateChannel = null)
        {
            lock (gate)
            {
                if (stopped) return;
                if (ready) { action(); return; }
                if (pending.Count < capacity && overflowState.Count == 0) pending.Enqueue(action);
                else if (stateChannel != null)
                    overflowState[stateChannel] = new System.Collections.Generic.KeyValuePair<long, Action>(++sequence, action);
                else TapLog.Warning("[Openlog] Cache unavailable; pending buffer full, rejecting new log.");
            }
        }

        internal void Complete(int version, Action start)
        {
            lock (gate)
            {
                if (stopped || version != generation) return;
                start();
                do
                {
                    while (!stopped && version == generation && pending.Count != 0)
                    {
                        try { pending.Dequeue()(); }
                        catch (Exception e) { TapLog.Warning("[Openlog] Deferred call failed: " + e.Message); }
                    }
                    if (!stopped && version == generation)
                    {
                        var states = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<long, Action>>(overflowState.Values);
                        overflowState.Clear();
                        states.Sort((left, right) => left.Key.CompareTo(right.Key));
                        foreach (var state in states)
                        {
                            if (stopped || version != generation) break;
                            try { state.Value(); }
                            catch (Exception e) { TapLog.Warning("[Openlog] Deferred state failed: " + e.Message); }
                        }
                    }
                } while (!stopped && version == generation && (pending.Count != 0 || overflowState.Count != 0));
                ready = !stopped && version == generation;
            }
        }

        internal void Stop(Action stop)
        {
            lock (gate)
            {
                bool wasReady = ready;
                stopped = true;
                ready = false;
                ++generation;
                pending.Clear();
                overflowState.Clear();
                if (wasReady) stop();
            }
        }
    }

    // All consumers of a cache must await this before creating, reading or writing its target.
    internal sealed class SharedCacheStorage
    {
        private readonly string source;
        internal readonly string Target;
        private readonly bool directory;
        private readonly object gate = new object();
        private Task preparation;

        internal SharedCacheStorage(string source, string target, bool directory)
        {
            this.source = source;
            Target = target;
            this.directory = directory;
        }

        internal Task PrepareAsync()
        {
            lock (gate)
            {
                if (preparation == null || preparation.IsFaulted || preparation.IsCanceled)
                    preparation = SharedCacheFile.MigrateAsync(source, Target, directory);
                return preparation;
            }
        }
    }
}
#endif
