using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using TapSDK.Core.Internal.Log;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    internal class TapOpenlogWrapper
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        internal const string DllName = "tapsdkcore";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        internal const string DllName = "libtapsdkcorecpp";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        internal const string DllName = "libtapsdkcorecpp";
#endif

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TapSdkCppLogWriterDelegate(
            int logLevel,
            IntPtr codeLocation,
            IntPtr logTag,
            IntPtr logMessage
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkCppInitLogger(int logLevel, TapSdkCppLogWriterDelegate logWriter);

        private static readonly TapSdkCppLogWriterDelegate logWriterDelegate = LogWriterCallback;

        [MonoPInvokeCallback(typeof(TapSdkCppLogWriterDelegate))]
        private static void LogWriterCallback(int logLevel, IntPtr codeLocation, IntPtr logTag, IntPtr logMessage)
        {
            // Native 初始化可能重设日志级别，回调仍需遵循 SDK 日志开关。
            if (!TapLog.Enabled)
            {
                return;
            }

            try
            {
                string codeLocationStr = PtrToUtf8StringSafe(codeLocation);
                string logTagStr = PtrToUtf8StringSafe(logTag);
                string logMessageStr = PtrToUtf8StringSafe(logMessage);
                var msg = $"[TapSDK-{logTagStr}] [{codeLocationStr}] {logMessageStr}";
                switch (logLevel)
                {
                    case 1: // trace
                    case 2: // debug
                    case 3: // info
                        Debug.Log(msg);
                        break;
                    case 4: // warn
                        Debug.LogWarning(msg);
                        break;
                    default: // error
                        Debug.LogError(msg);
                        break;
                }
            }
            catch (Exception e)
            {
                // 回调由 native 调用，绝不能向上抛异常
                Debug.LogError($"[TapSDK-Core] LogWriterCallback exception: {e}");
            }
        }

        private static string PtrToUtf8StringSafe(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }
            byte[] buffer = GetUTF8Byte(ptr);
            // Encoding.UTF8 默认使用 replacement fallback，非法字节会被替换为 U+FFFD，不会抛异常
            return Encoding.UTF8.GetString(buffer);
        }

        private static byte[] GetUTF8Byte(IntPtr ptr)
        {
            // 1. 获取字符串长度（假设以 null 结尾）
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
            {
                len++;
            }

            // 2. 复制字节到托管数组
            byte[] buffer = new byte[len];
            Marshal.Copy(ptr, buffer, 0, len);
            return buffer;
        }

        internal static void InitLogger(int logLevel)
        {
            TapSdkCppInitLogger(logLevel, logWriterDelegate);
        }

        /**
         * 初始化接口，只需要调用一次。
         *
         *   cfg 初始化配置，JSON 格式：
         *
        {
            "region": 2,
            "log_to_console": 1,
            "log_level": 1,
            "data_dir": "/tmp",
            "env": "local",
            "platform": "abc",
            "ua": "TapSDK-Android/3.28.0",
            "client_id": "***",
            "client_token": "***",
            "modules": [
                "app_duration"
            ],
            "common": {
                "pn": "TapSDK",
                "app_version_code": "123",
                "app_version": "1.2.3",
                "app_package_name": "",
                "install_uuid": "",
                "device_id": "123456",
                "caid": "",
                "dv": "",
                "md": "",
                "hardware": "",
                "cpu": "",
                "cpu_abis": "",
                "os": "android",
                "sv": "",
                "width": "",
                "height": "",
                "total_rom": "",
                "total_ram": "",
                "open_id": "",
                "tds_user_id": "",
                "sdk_locale": ""
            }
            "app_duration": {
                "tapsdk_version": ""
            }
        }
         *
         *   - log_level 取值：1 Trace、2 Debug、3 Info、4 Warn、5 Error、6 完全不输出
         *   - region 取值：0 国内、1 海外、2 RND
         *   - modules 取值：app_duration（开启时长埋点）。如果 modules 不传，或者为空，
         *                  则仅开启 OpenLog 功能，不会上报游戏时长
         *
         *  commonVariablesGetter 用于获取运行时会发生变化的公参
         *
         * 成功返回 0，失败返回 -1
         */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TdkOnAppStarted(string cfg, CommonVariablesGetter commonVariablesGetter,
            FreeStringCallback freeString);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr CommonVariablesGetter();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void FreeStringCallback(IntPtr intPtr);

        /**
         * App 退出时调用，只需要调用一次。
         */
        [DllImport(DllName, EntryPoint = "TdkOnAppStopped")]
        private static extern void TdkOnAppStoppedNative();

        internal static void TdkOnAppStopped()
        {
            TapOpenlogStandalone.PendingCalls.Stop(() => TdkOnAppStoppedNative());
        }

        /**
         * 启用功能模块，如：app_duration
         *
         *   modules JSON 数组，如：["app_duration"]
         *
         */
        [DllImport(DllName, EntryPoint = "TdkEnableModules")]
        private static extern void TdkEnableModulesNative(string modules);

        internal static void TdkEnableModules(string modules)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkEnableModulesNative(modules), "modules");
        }

        /**
        * 禁用功能模块，如：app_duration
        *
        *   modules JSON 数组，如：["app_duration"]
        *
        */
        [DllImport(DllName, EntryPoint = "TdkDisableModules")]
        private static extern void TdkDisableModulesNative(string modules);

        internal static void TdkDisableModules(string modules)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkDisableModulesNative(modules), "modules");
        }

        /**
         * 需要发送埋点日志时调用。
         * SDK 内部会整合初始化接口传入的公参、extraArgsFunc 返回的公参，以及 log 里的业务参数，
         * 然后再发送到 OpenLog 服务端
         *
         *   logstore 如：tapsdk、tapsdk-apm
         *   log 埋点日志，仅需传递业务参数，JSON 格式：{"action":"xxx", "open_id":"yyy","tds_user_id":"zzz"}
         */
        [DllImport(DllName, EntryPoint = "TdkOpenLog")]
        private static extern void TdkOpenLogNative(string logStore, string logContent);

        internal static void TdkOpenLog(string logStore, string logContent)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkOpenLogNative(logStore, logContent));
        }

        /**
        * 用户登录成功时调用。
        *
        *   userInfo 用户信息，JSON 格式：{"open_id":"","tds_user_id":""}
        */
        [DllImport(DllName, EntryPoint = "TdkOnLogin")]
        private static extern void TdkOnLoginNative(string userInfo);

        internal static void TdkOnLogin(string userInfo)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkOnLoginNative(userInfo), "account");
        }

        /**
         * 用户登出时调用。
         */
        [DllImport(DllName, EntryPoint = "TdkOnLogout")]
        private static extern void TdkOnLogoutNative();

        internal static void TdkOnLogout()
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkOnLogoutNative(), "account");
        }

        /**
         * 用户切到后台时调用。
         */
        [DllImport(DllName, EntryPoint = "TdkOnForeground")]
        private static extern void TdkOnForegroundNative();

        internal static void TdkOnForeground()
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkOnForegroundNative(), "activity");
        }

        /**
         * 游戏切回前台时调用。
         */
        [DllImport(DllName, EntryPoint = "TdkOnBackground")]
        private static extern void TdkOnBackgroundNative();

        internal static void TdkOnBackground()
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkOnBackgroundNative(), "activity");
        }

        /**
         * 设置额外的时长模块日志参数，JSON K/V 格式。每次调用这个接口时，会用最新得到的参数，替换原有参数。
         *
         *   params 额外日志参数，JSON 格式：{"K1":"V1","K2":"V2","K3":"V3"}
         */
        [DllImport(DllName, EntryPoint = "TdkSetExtraAppDurationParams")]
        private static extern void TdkSetExtraAppDurationParamsNative(string paramsJson);

        internal static void TdkSetExtraAppDurationParams(string paramsJson)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkSetExtraAppDurationParamsNative(paramsJson), "extra");
        }

        /**
         * 设置日志等级
         */
        [DllImport(DllName, EntryPoint = "TdkSetLogLevel")]
        private static extern void TdkSetLogLevelNative(int logLevel, int logToConsole);

        internal static void TdkSetLogLevel(int logLevel, int logToConsole)
        {
            TapOpenlogStandalone.PendingCalls.Invoke(() => TdkSetLogLevelNative(logLevel, logToConsole), "logging");
        }

        /**
         * 代码版本，如：1.2.5
         */
        [DllImport(DllName)]
        internal static extern IntPtr TdkVersion();

        /**
         * git commit 版本，如：98f5d81a0fdcab9a755878b3e825c2cb510e5196
         */
        [DllImport(DllName)]
        internal static extern IntPtr TdkGitCommit();

        // 用于返回 TdkVersion 的封装方法
        public static string GetTdkVersion()
        {
            return Marshal.PtrToStringAnsi(TdkVersion());
        }

        // 用于返回 TdkGitCommit 的封装方法
        public static string GetTdkGitCommit()
        {
            return Marshal.PtrToStringAnsi(TdkGitCommit());
        }
    }
}