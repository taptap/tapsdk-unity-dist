using System;
using System.Runtime.InteropServices;
using System.Text;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Standalone.Internal
{
    internal enum TapSDKInitResult
    {
        // 初始化成功
        OK = 0,

        // 其他错误
        FailedGeneric = 1,

        // 未找到 TapTap，用户可能未安装，请引导用户下载安装 TapTap
        NoPlatform = 2,

        // 已安装 TapTap，游戏未通过 TapTap 启动
        NotLaunchedByPlatform = 3,

        // 平台版本不匹配，请引导用户升级 TapTap 与游戏至最新版本，再重新运行游戏
        PlatformVersionMismatch = 4,

        // SDK 本地执行时未知错误
        Unknown = -1,
    };

    internal enum TapEventID
    {
        SystemStateChanged = 1, // TapTap 客户端运行状态事件监听
    }

    // 系统事件类型
    internal enum SystemState
    {
        Unknown = 0, // 未知
        Online = 1, // 在线
        Offline = 2, // 离线
        Shutdown = 3, // 退出
    };

    // 授权返回结果结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct SystemStateResponse
    {
        public int state; // 运行状态
    }

    public class TapClientBridge
    {
#if UNITY_STANDALONE_WIN
        public const string DLL_NAME = "taptap_api";
#endif

#if UNITY_STANDALONE_WIN
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapSDK_RestartAppIfNecessary(
            [MarshalAs(UnmanagedType.LPStr)] string clientId
        );

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSDK_Init(
            StringBuilder errMsg,
            [MarshalAs(UnmanagedType.LPStr)] string pubKey
        );

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSDK_Shutdown();

        // 定义与 C 兼容的委托
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallbackDelegate(int id, IntPtr userData);

        // 系统状态返回结果结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct SystemStateResponse
        {
            public SystemState state; // 枚举直接映射
        }

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TapSDK_RegisterCallback(int callbackId, IntPtr callback);

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSDK_RunCallbacks();

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TapSDK_UnregisterCallback(int callbackId, IntPtr callback);

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapUser_GetOpenID(StringBuilder openId);

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapSDK_GetClientID(StringBuilder clientId);

        // 初始化检查
        internal static int CheckInitState(out string errMessage, string key)
        {
            StringBuilder errMsgBuffer = new StringBuilder(1024); // 分配 1024 字节缓冲区
            int result = TapSDK_Init(errMsgBuffer, key);
            errMessage = errMsgBuffer.ToString();
            TapLog.Log("CheckInitState result = " + result);
            return result;
        }

        internal static bool GetTapUserOpenId(out string openId)
        {
            StringBuilder openIdBuffer = new StringBuilder(256); // 分配一个足够大的缓冲区
            bool success = TapUser_GetOpenID(openIdBuffer); // 调用 C 函数
            openId = openIdBuffer.ToString();
            return success;
        }

        internal static bool GetClientId(out string clientId)
        {
            StringBuilder clientIDBuffer = new StringBuilder(256); // 分配一个足够大的缓冲区
            bool success = TapSDK_GetClientID(clientIDBuffer); // 调用 C 函数
            clientId = clientIDBuffer.ToString();
            return success;
        }

        private static CallbackDelegate _systemStateCallbackInstance;
        internal static void RegisterSystemStateCallback(CallbackDelegate callback)
        {
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
            if (_systemStateCallbackInstance != null)
            {
                UnRegisterSystemStateCallback(_systemStateCallbackInstance);
            }
            _systemStateCallbackInstance = callback;
            TapSDK_RegisterCallback((int)TapEventID.SystemStateChanged, funcPtr);
        }

        internal static void UnRegisterSystemStateCallback(CallbackDelegate callback)
        {
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
            TapSDK_UnregisterCallback((int)TapEventID.SystemStateChanged, funcPtr);
            _systemStateCallbackInstance = null;
        }
#endif
    }
}
