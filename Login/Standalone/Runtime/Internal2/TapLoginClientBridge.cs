using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone;
using TapSDK.Core.Standalone.Internal;

/// 使用 TapTap PC 客户端发起登录
#if UNITY_STANDALONE_WIN

namespace TapSDK.Login.Internal
{
    internal class TapLoginClientBridge
    {
        public const string DLL_NAME = "taptap_api";

        private static TapClientBridge.CallbackDelegate _userCallbackInternalInstance;

        // 是否触发授权的返回结果
        internal enum AuthorizeResult
        {
            UNKNOWN = 0, // 未知
            OK = 1, // 成功触发授权
            FAILED = 2, // 授权失败
        };

        // 完成授权后的返回结果
        internal enum Result
        {
            kResult_OK = 0,
            kResult_Failed = 1,
            kResult_Canceled = 2,
        };

        // 登录事件 ID
        internal enum TapEventID
        {
            AuthorizeFinished_internal = 2001,
        };

        // 授权返回结果结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct AuthorizeFinishedResponse
        {
            public int is_cancel; // 是否取消

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string callback_uri; // 256 字节的 C 端字符串
        }

        [DllImport(DLL_NAME, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapUser_AsyncAuthorize_internal(
            [MarshalAs(UnmanagedType.LPStr)] string scopeStrings,
            [MarshalAs(UnmanagedType.LPStr)] string responseType,
            [MarshalAs(UnmanagedType.LPStr)] string redirectUri,
            [MarshalAs(UnmanagedType.LPStr)] string codeChallenge,
            [MarshalAs(UnmanagedType.LPStr)] string state,
            [MarshalAs(UnmanagedType.LPStr)] string codeChallengeMethod,
            [MarshalAs(UnmanagedType.LPStr)] string versonCode,
            [MarshalAs(UnmanagedType.LPStr)] string sdkUa,
            [MarshalAs(UnmanagedType.LPStr)] string info
        );

        internal static void RegisterCallback(
            TapEventID eventID,
            TapClientBridge.CallbackDelegate callback
        )
        {
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
            if (_userCallbackInternalInstance != null)
            {
                UnRegisterCallback(eventID, _userCallbackInternalInstance);
            }
            _userCallbackInternalInstance = callback;
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
            _userCallbackInternalInstance = null;
        }

        private static TaskCompletionSource<TapLoginResponseByTapClient> taskCompletionSource;

        /// <summary>
        /// 发起登录授权
        /// </summary>
        public static async Task<TapLoginResponseByTapClient> StartLoginWithScopes(
            string[] scopes,
            string responseType,
            string redirectUri,
            string codeChallenge,
            string state,
            string codeChallengeMethod,
            string versonCode,
            string sdkUa,
            string info
        )
        {
            if (!TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck())
            {
                // UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 正在执行，请在完成后调用授权接口", UIManager.GeneralToastLevel.Error);
                TapLog.Error(" login must be invoked after IsLaunchedFromTapTapPC success");
                return new TapLoginResponseByTapClient(
                    "login must be invoked after IsLaunchedFromTapTapPC success"
                );
            }
            taskCompletionSource = new TaskCompletionSource<TapLoginResponseByTapClient>();

            TapLog.Log(
                "LoginWithScopes start login by tapclient thread = "
                    + Thread.CurrentThread.ManagedThreadId
            );
            _ = Task.Run(() =>
            {
                try
                {
                    RegisterCallback(TapEventID.AuthorizeFinished_internal, loginCallbackDelegate);
                    AuthorizeResult authorizeResult =
                        (AuthorizeResult)TapUser_AsyncAuthorize_internal(
                            string.Join(",", scopes),
                            responseType,
                            redirectUri,
                            codeChallenge,
                            state,
                            codeChallengeMethod,
                            versonCode,
                            sdkUa,
                            info
                        );
                    TapLog.Log("LoginWithScopes start result = " + authorizeResult);
                    if (authorizeResult != AuthorizeResult.OK)
                    {
                        UnRegisterCallback(
                            TapEventID.AuthorizeFinished_internal,
                            loginCallbackDelegate
                        );
                        taskCompletionSource?.TrySetResult(
                            new TapLoginResponseByTapClient(
                                "发起授权失败，请确认 Tap 客户端是否正常运行"
                            )
                        );
                        taskCompletionSource = null;
                    }
                }
                catch (Exception ex)
                {
                    TapLog.Log("LoginWithScopes start login by tapclient error = " + ex.Message);
                    UnRegisterCallback(
                        TapEventID.AuthorizeFinished_internal,
                        loginCallbackDelegate
                    );
                    taskCompletionSource?.TrySetResult(new TapLoginResponseByTapClient(ex.Message));
                    taskCompletionSource = null;
                }
            });
            return await taskCompletionSource.Task;
        }

        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void loginCallbackDelegate(int id, IntPtr userData)
        {
            TapLog.Log("LoginWithScopes recevie callback " + id);
            if (id == (int)TapEventID.AuthorizeFinished_internal)
            {
                TapLog.Log(
                    "LoginWithScopes callback thread = " + Thread.CurrentThread.ManagedThreadId
                );
                AuthorizeFinishedResponse response =
                    Marshal.PtrToStructure<AuthorizeFinishedResponse>(userData);
                TapLog.Log(
                    "LoginWithScopes callback = "
                        + response.is_cancel
                        + " uri = "
                        + response.callback_uri
                );
                if (taskCompletionSource != null)
                {
                    UnRegisterCallback(
                        TapEventID.AuthorizeFinished_internal,
                        loginCallbackDelegate
                    );
                    taskCompletionSource.TrySetResult(
                        new TapLoginResponseByTapClient(
                            response.is_cancel != 0,
                            response.callback_uri
                        )
                    );
                    taskCompletionSource = null;
                }
            }
        }

        // 使用客户端登录结果返回值
        public class TapLoginResponseByTapClient
        {
            public bool isCancel = false;

            public string redirectUri;

            public bool isFail = false;

            public string errorMsg;

            public TapLoginResponseByTapClient(bool isCancel, string redirctUri)
            {
                this.redirectUri = redirctUri;
                this.isCancel = isCancel;
            }

            public TapLoginResponseByTapClient(string errorMsg)
            {
                isFail = true;
                isCancel = false;
                this.errorMsg = errorMsg;
            }
        }
    }
}
#endif
