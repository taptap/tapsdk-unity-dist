using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TapSDK.Core.Standalone.Internal
{
    // TapSDK 独立登记进程，映射保留至进程退出，避免后台写入仍存活时其他进程接管。
    internal static class StandaloneProcessIdentity
    {
        private static IntPtr registration;
        private static string registeredRoot;
        private static bool primary;

        internal static bool Register(string rootHash)
        {
            if (registration != IntPtr.Zero)
            {
                if (registeredRoot != rootHash) throw new InvalidOperationException("TapSDK storage root changed.");
                return primary;
            }
            IntPtr handle = CreateFileMappingW(new IntPtr(-1), IntPtr.Zero, 4, 0, 1,
                "Local\\TapSDK.Storage." + rootHash);
            int error = Marshal.GetLastWin32Error();
            if (handle == IntPtr.Zero) throw new Win32Exception(error);
            registration = handle;
            registeredRoot = rootHash;
            primary = error != 183;
            return primary;
        }

        internal static int CurrentPid
        {
            get
            {
                using (var process = Process.GetCurrentProcess())
                {
                    return process.Id;
                }
            }
        }

        // ArgumentException 表示进程已退出；权限拒绝及其他异常视为存活状态未知。
        internal static long StartedUtc(int pid)
        {
            if (pid <= 0) throw new ArgumentOutOfRangeException(nameof(pid));
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                using (var process = Process.GetProcessById(pid))
                {
                    if (process.HasExited) throw new ArgumentException("Process exited.");
                    return process.StartTime.ToUniversalTime().Ticks;
                }
            }
            // Unity 2019 Windows IL2CPP 未实现托管 Process.StartTime，改用原生查询。
            IntPtr handle = OpenProcess(0x1000, false, pid);
            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 87) throw new ArgumentException("Process exited.");
                throw new Win32Exception(error);
            }
            try
            {
                long created, exited, kernel, user;
                if (!GetProcessTimes(handle, out created, out exited, out kernel, out user))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (exited != 0) throw new ArgumentException("Process exited.");
                return DateTime.FromFileTimeUtc(created).Ticks;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateFileMappingW(IntPtr file, IntPtr security, uint protect, uint high, uint low, string name);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessTimes(IntPtr process, out long created, out long exited, out long kernel, out long user);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
