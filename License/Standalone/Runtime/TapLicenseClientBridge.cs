using System;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone;
using TapSDK.Core.Standalone.Internal;

#if PLATFORM_STANDALONE_WIN
namespace TapSDK.License.Standalone
{
    internal class TapLicenseClientBridge
    {
        private static Action<string, bool> currentDlcDelegate;
        private static Action<bool> currentLicenseDelegate;

        /// 查询是否购买 DLC , 未调用 isLaunchFromPC 会抛异常
        public static bool QueryDLC(string skuId)
        {
            if (!TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck())
            {
                throw new Exception(
                    "queryDLC must be invoked after IsLaunchedFromTapTapPC success"
                );
            }
            bool success = LicenseNativeBridge.TapDLC_IsOwned(skuId);
            return success;
        }

        /// 跳转到 TapTap 客户端 DLC 购买页面 , 未调用 isLaunchFromPC 会抛异常
        public static bool ShowStore(string skuId)
        {
            if (!TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck())
            {
                throw new Exception(
                    "purchaseDLC must be invoked after IsLaunchedFromTapTapPC success"
                );
            }
            TapLog.Log("purchaseDLC start = " + skuId);
            return LicenseNativeBridge.TapDLC_ShowStore(skuId);
        }

        /// 注册 DLC 购买状态变更回调，包括购买成功和退款
        public static void RegisterDLCOwnedCallback(Action<string, bool> dlcDelegate)
        {
            currentDlcDelegate = dlcDelegate;
            LicenseNativeBridge.RegisterCallback(
                LicenseNativeBridge.TapEventID.DLCPlayableStatusChanged,
                DLCCallbackDelegate
            );
        }

        /// DLC 回调
        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void DLCCallbackDelegate(int id, IntPtr userData)
        {
            TapLog.Log("queryDlC recevie callback " + id);
            if (currentDlcDelegate != null)
            {
                LicenseNativeBridge.DLCPlayableStatusChangedResponse response =
                    Marshal.PtrToStructure<LicenseNativeBridge.DLCPlayableStatusChangedResponse>(
                        userData
                    );
                TapLog.Log(
                    "queryDlC callback =  " + response.dlc_id + " isOwn = " + response.is_playable
                );
                currentDlcDelegate(response.dlc_id, response.is_playable != 0);
            }
        }

        /// 注册 License 购买状态变更回调，包括购买成功和退款
        public static void RegisterLicenseCallback(Action<bool> licensecDelegate)
        {
            currentLicenseDelegate = licensecDelegate;
            LicenseNativeBridge.RegisterCallback(
                LicenseNativeBridge.TapEventID.GamePlayableStatusChanged,
                LicenseCallbackDelegate
            );
        }

        /// License 回调
        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void LicenseCallbackDelegate(int id, IntPtr userData)
        {
            TapLog.Log("License recevie callback " + id);
            if (currentLicenseDelegate != null)
            {
                LicenseNativeBridge.GamePlayableStatusChangedResponse response =
                    Marshal.PtrToStructure<LicenseNativeBridge.GamePlayableStatusChangedResponse>(
                        userData
                    );
                TapLog.Log("License callback  isOwn changed " + response.is_playable);
                currentLicenseDelegate(response.is_playable != 0);
            }
        }

        public static bool HasLicense()
        {
            if (!TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck())
            {
                throw new Exception(
                    "checkLicense must be invoked after IsLaunchedFromTapTapPC success"
                );
            }
            return LicenseNativeBridge.TapApps_IsOwned();
        }
    }

    internal class LicenseNativeBridge
    {
        internal enum TapEventID
        {
            // [4001, 6000), reserved for TapTap ownership events
            GamePlayableStatusChanged = 4001,
            DLCPlayableStatusChanged = 4002,
        };

        public const string DLL_NAME = "taptap_api";

        // 检查是否拥有当前游戏
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapApps_IsOwned();

        // 游戏本体可玩状态变更事件响应结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct GamePlayableStatusChangedResponse
        {
            public byte is_playable; // 游戏本体是否可玩
        };

        // 显示指定 DLC 的商店页面
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapDLC_ShowStore([MarshalAs(UnmanagedType.LPStr)] string dlcId);

        // 查询用户是否拥有指定的 DLC
        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool TapDLC_IsOwned([MarshalAs(UnmanagedType.LPStr)] string dlcId);

        // DLC 授权完成响应结果
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DLCPlayableStatusChangedResponse
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dlc_id; // DLC ID

            public byte is_playable; // 是否可玩，当用户购买 DLC （外置 DLC 为购买且下载完成后），此值返回 true。其他情况返回 false
        }

        // 预防 GC 回收的静态变量
        private static TapClientBridge.CallbackDelegate _dlcCallbackInstance;

        private static TapClientBridge.CallbackDelegate _licenseCallbackInstance;

        internal static void RegisterCallback(
            TapEventID eventID,
            TapClientBridge.CallbackDelegate callback
        )
        {
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
            switch (eventID)
            {
                case TapEventID.DLCPlayableStatusChanged:
                    if (_dlcCallbackInstance != null)
                    {
                        UnRegisterCallback(eventID, _dlcCallbackInstance);
                    }
                    _dlcCallbackInstance = callback;
                    break;
                case TapEventID.GamePlayableStatusChanged:
                    if (_licenseCallbackInstance != null)
                    {
                        UnRegisterCallback(eventID, _licenseCallbackInstance);
                    }
                    _licenseCallbackInstance = callback;
                    break;
            }

            TapClientBridge.TapSDK_RegisterCallback((int)eventID, funcPtr);
        }

        // 移除回调
        internal static void UnRegisterCallback(
            TapEventID eventID,
            TapClientBridge.CallbackDelegate callback
        )
        {
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
            TapClientBridge.TapSDK_UnregisterCallback((int)eventID, funcPtr);
            switch (eventID)
            {
                case TapEventID.DLCPlayableStatusChanged:
                    _dlcCallbackInstance = null;
                    break;
                case TapEventID.GamePlayableStatusChanged:
                    _licenseCallbackInstance = null;
                    break;
            }
        }
    }
}
#endif
