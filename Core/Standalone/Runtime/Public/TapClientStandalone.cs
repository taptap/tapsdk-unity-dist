using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal;
using TapSDK.Core.Standalone.Internal.Openlog;
using TapSDK.UI;
using UnityEditor;
using UnityEngine;

namespace TapSDK.Core.Standalone
{
#if UNITY_STANDALONE_WIN
    public class TapClientStandalone
    {
        // 是否是渠道服游戏包
        private static bool isChannelPackage = false;

        // -1 未执行 0 失败  1 成功
        private static int lastIsLaunchedFromTapTapPCResult = -1;
        private static bool isRuningIsLaunchedFromTapTapPC = false;

        // 当为渠道游戏包时，与启动器的初始化校验结果
        private static TapInitResult tapInitResult;

        // <summary>
        // 校验游戏是否通过启动器唤起，建立与启动器通讯
        //</summary>
        public static async Task<bool> IsLaunchedFromTapTapPC()
        {
            // 正在执行中
            if (isRuningIsLaunchedFromTapTapPC)
            {
                UIManager.Instance.OpenToast(
                    "IsLaunchedFromTapTapPC 正在执行，请勿重复调用",
                    UIManager.GeneralToastLevel.Error
                );
                TapLog.Error("IsLaunchedFromTapTapPC 正在执行，请勿重复调用");
                return false;
            }
            // 多次执行时返回上一次结果
            if (lastIsLaunchedFromTapTapPCResult != -1)
            {
                TapLog.Log(
                    "IsLaunchedFromTapTapPC duplicate invoke return "
                        + lastIsLaunchedFromTapTapPCResult
                );
                return lastIsLaunchedFromTapTapPCResult > 0;
            }

            isChannelPackage = true;
            TapTapSdkOptions coreOptions = TapCoreStandalone.coreOptions;
            if (coreOptions == null)
            {
                UIManager.Instance.OpenToast(
                    "IsLaunchedFromTapTapPC 调用必须在初始化之后",
                    UIManager.GeneralToastLevel.Error
                );
                TapLog.Error("IsLaunchedFromTapTapPC 调用必须在初始化之后");
                return false;
            }
            string clientId = coreOptions.clientId;
            string pubKey = coreOptions.clientPublicKey;
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(pubKey))
            {
                UIManager.Instance.OpenToast(
                    "clientId 及 TapPubKey 参数都不能为空, clientId ="
                        + clientId
                        + ", TapPubKey = "
                        + pubKey,
                    UIManager.GeneralToastLevel.Error
                );
                TapLog.Error(
                    "clientId 或 TapPubKey 无效, clientId = " + clientId + ", TapPubKey = " + pubKey
                );
                return false;
            }
            isRuningIsLaunchedFromTapTapPC = true;

            string sessionId = Guid.NewGuid().ToString();
            TapCoreTracker.Instance.TrackStart(TapCoreTracker.METHOD_LAUNCHER, sessionId);
            try
            {
                TapInitResult result = await RunClientBridgeMethod(clientId, pubKey);
                TapLog.Log(
                    "check startupWithClientBridge finished thread = "
                        + Thread.CurrentThread.ManagedThreadId
                );
                isRuningIsLaunchedFromTapTapPC = false;
                if (result.needQuitGame)
                {
                    lastIsLaunchedFromTapTapPCResult = 0;
                    TapCoreTracker.Instance.TrackSuccess(
                        TapCoreTracker.METHOD_LAUNCHER,
                        sessionId,
                        TapCoreTracker.SUCCESS_TYPE_RESTART
                    );
                    TapLog.Log("IsLaunchedFromTapTapPC Quit game");
#if UNITY_EDITOR
                    TapLog.Log(
                        $"本地测试文件配置错误，请确保 taptap_client_id.txt 文件拷贝到 {EditorApplication.applicationPath} 同级目录下"
                    );
                    EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif

                    return false;
                }
                else
                {
                    if (result.result == (int)TapSDKInitResult.OK)
                    {
                        string currentClientId;
                        bool isFetchClientIdSuccess = TapClientBridge.GetClientId(
                            out currentClientId
                        );
                        TapLog.Log("IsLaunchedFromTapTapPC get  clientId = " + currentClientId);
                        if (
                            isFetchClientIdSuccess
                            && !string.IsNullOrEmpty(currentClientId)
                            && currentClientId != clientId
                        )
                        {
                            UIManager.Instance.OpenToast(
                                "SDK 中配置的 clientId = "
                                    + clientId
                                    + "与 Tap 启动器中"
                                    + currentClientId
                                    + "不一致",
                                UIManager.GeneralToastLevel.Error
                            );
                            TapLog.Error(
                                "SDK 中配置的 clientId = "
                                    + clientId
                                    + "与 Tap 启动器中"
                                    + currentClientId
                                    + "不一致"
                            );
                            TapCoreTracker.Instance.TrackFailure(
                                TapCoreTracker.METHOD_LAUNCHER,
                                sessionId,
                                -1,
                                "SDK 中配置的 clientId = "
                                    + clientId
                                    + "与 Tap 启动器中"
                                    + currentClientId
                                    + "不一致"
                            );
                            lastIsLaunchedFromTapTapPCResult = 0;
                            return false;
                        }
                        string openId;
                        bool fetchOpenIdSuccess = TapClientBridge.GetTapUserOpenId(out openId);
                        if (fetchOpenIdSuccess)
                        {
                            TapLog.Log("IsLaunchedFromTapTapPC get  openId = " + openId);
                            EventManager.TriggerEvent(
                                EventManager.IsLaunchedFromTapTapPCFinished,
                                openId
                            );
                        }
                        else
                        {
                            TapLog.Log("IsLaunchedFromTapTapPC get  openId failed");
                        }
                        lastIsLaunchedFromTapTapPCResult = 1;
                        TapClientBridgePoll.StartUp();
                        TapCoreTracker.Instance.TrackSuccess(
                            TapCoreTracker.METHOD_LAUNCHER,
                            sessionId,
                            TapCoreTracker.SUCCESS_TYPE_INIT
                        );
                        TapLog.Log("IsLaunchedFromTapTapPC check success");
                        // 如果开发者已经注册了监听客户端运行状态，此时添加对应回调
                        if (
                            taptapPCStateChangeListeners != null
                            && taptapPCStateChangeListeners.Count > 0
                            && !hasRegisterSystemListener
                        )
                        {
                            TapClientBridge.RegisterSystemStateCallback(TapTapPCStateDelegate);
                            hasRegisterSystemListener = true;
                        }
                        return true;
                    }
                    else
                    {
                        TapCoreTracker.Instance.TrackFailure(
                            TapCoreTracker.METHOD_LAUNCHER,
                            sessionId,
                            (int)result.result,
                            result.errorMsg ?? ""
                        );
                        lastIsLaunchedFromTapTapPCResult = 0;
                        TapLog.Log(
                            "IsLaunchedFromTapTapPC show TapClient tip Pannel "
                                + result.result
                                + " , error = "
                                + result.errorMsg
                        );
                        string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                        if (Resources.Load<GameObject>(tipPannelPath) != null)
                        {
                            var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(
                                tipPannelPath
                            );
                            pannel.Show(result.result);
                        }
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                lastIsLaunchedFromTapTapPCResult = 0;
                TapCoreTracker.Instance.TrackFailure(
                    TapCoreTracker.METHOD_LAUNCHER,
                    sessionId,
                    (int)TapSDKInitResult.Unknown,
                    e.Message ?? ""
                );

                TapLog.Log(
                    "IsLaunchedFromTapTapPC check exception = " + e.Message + " \n" + e.StackTrace
                );
                string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                if (Resources.Load<GameObject>(tipPannelPath) != null)
                {
                    var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(
                        tipPannelPath
                    );
                    pannel.Show((int)TapSDKInitResult.Unknown);
                }
                return false;
            }
        }

        private static async Task<TapInitResult> RunClientBridgeMethod(
            string clientId,
            string pubKey
        )
        {
            TaskCompletionSource<TapInitResult> task = new TaskCompletionSource<TapInitResult>();
            try
            {
                await Task.Run(() =>
                {
                    TapLog.Log(
                        "check startupWithClientBridge start thread = "
                            + Thread.CurrentThread.ManagedThreadId
                    );
                    bool needQuitGame = TapClientBridge.TapSDK_RestartAppIfNecessary(clientId);
                    TapLog.Log(
                        "RunClientBridgeMethodWithTimeout invoke  TapSDK_RestartAppIfNecessary result = "
                            + needQuitGame
                    );
                    TapLog.Log(
                        "RunClientBridgeMethodWithTimeout invoke  TapSDK_RestartAppIfNecessary finished "
                    );
                    if (needQuitGame)
                    {
                        tapInitResult = new TapInitResult(needQuitGame);
                    }
                    else
                    {
                        string outputError;
                        int tapSDKInitResult = TapClientBridge.CheckInitState(
                            out outputError,
                            pubKey
                        );
                        TapLog.Log(
                            "RunClientBridgeMethodWithTimeout invoke  CheckInitState result = "
                                + tapSDKInitResult
                                + ", error = "
                                + outputError
                        );
                        tapInitResult = new TapInitResult(tapSDKInitResult, outputError);
                    }
                    task.TrySetResult(tapInitResult);
                });
            }
            catch (Exception ex)
            {
                TapLog.Log("RunClientBridgeMethodWithTimeout invoke C 方法出错！" + ex.Message);
                task.TrySetException(ex);
            }
            return await task.Task;
        }

        /// <summary>
        /// 是否需要从启动器登录
        /// </summary>
        public static bool IsNeedLoginByTapClient()
        {
            return isChannelPackage;
        }

        public static bool isPassedInLaunchedFromTapTapPCCheck()
        {
            return lastIsLaunchedFromTapTapPCResult > 0;
        }

        private static HashSet<Action<int>> taptapPCStateChangeListeners;
        private static volatile bool hasRegisterSystemListener = false;
        /// <summary>
        /// 设置 TapPC 客户端状态监听
        /// </summary>
        internal static void RegisterTapTapPCStateChangeListener(Action<int> action)
        {
            if (taptapPCStateChangeListeners == null)
            {
                taptapPCStateChangeListeners = new HashSet<Action<int>>();
            }
            taptapPCStateChangeListeners.Add(action);
            if (isPassedInLaunchedFromTapTapPCCheck() && !hasRegisterSystemListener)
            {
                TapClientBridge.RegisterSystemStateCallback(TapTapPCStateDelegate);
                hasRegisterSystemListener = true;
            }
        }

        internal static void UnRegisterTapTapPCStateChangeListener(Action<int> action)
        {
            if (taptapPCStateChangeListeners != null && taptapPCStateChangeListeners.Count > 0)
            {
                taptapPCStateChangeListeners.Remove(action);
                if (taptapPCStateChangeListeners.Count == 0)
                {
                    TapClientBridge.UnRegisterSystemStateCallback(TapTapPCStateDelegate);
                    hasRegisterSystemListener = false;
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void TapTapPCStateDelegate(int id, IntPtr userData)
        {
            if (id == (int)TapEventID.SystemStateChanged)
            {
                SystemStateResponse response = Marshal.PtrToStructure<SystemStateResponse>(
                    userData
                );
                if (taptapPCStateChangeListeners != null)
                {
                    foreach (var listener in taptapPCStateChangeListeners)
                    {
                        listener(response.state);
                    }
                }
            }
        }

        // 初始化校验结果
        private class TapInitResult
        {
            internal int result;
            internal string errorMsg;

            internal bool needQuitGame = false;

            public TapInitResult(int result, string errorMsg)
            {
                this.result = result;
                this.errorMsg = errorMsg;
            }

            public TapInitResult(bool needQuitGame)
            {
                this.needQuitGame = needQuitGame;
            }
        }
    }
#endif
}
