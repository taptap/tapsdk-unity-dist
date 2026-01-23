using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using TapSDK.Login;
using UnityEngine;
#if UNITY_STANDALONE_WIN
using TapSDK.Core.Standalone;
#endif

#if !UNITY_EDITOR_OSX
namespace TapSDK.OnlineBattle
{
    public partial class TapTapOnlineBattle
    {
        public static string Version = "4.10.0";
        private static readonly bool isRND = false;

        // c 层是否初始化
        private static bool _hasInitNative = false;

        // 初始化配置项
        private static TapTapSdkOptions SdkOptions;

        private static object _lockObj = new object();

        // 储存方法调用与 Native 回调映射
        private static ConcurrentDictionary<long, object> AsyncNativeTaskMaps =
            new ConcurrentDictionary<long, object>();

        // 存储方法调用 sessionId 在 Native 回调映射
        private static ConcurrentDictionary<long, string> AsyncNativeSessionIdMaps =
            new ConcurrentDictionary<long, string>();

        // Native 层处理成功
        private const int RESULT_SUCCESS = 1;

        // Native 层处理失败
        private const int RESULT_FAILED = 0;

        // 开发者注册的回调集合
        private static HashSet<ITapOnlineBattleCallback> OnlineBattleCallbacks;
        private static TapTapAccount CurrentTapAccount;

        internal static void Init(TapTapSdkOptions options)
        {
            if (_hasInitNative)
            {
                return;
            }
            TapOnlineBattleUtils.Log("TapOnlineBattle start init");
            TapOnlineBattleTracker.Instance.TrackInit();
            SdkOptions = options;
            string cacheDir = Path.Combine(
                Application.persistentDataPath,
                "onlinebattle_" + options.clientId
            );
            string deviceID = SystemInfo.deviceUniqueIdentifier;
            int region = isRND ? 2 : 0;
            Dictionary<string, object> initConfig = new Dictionary<string, object>()
            {
                { "region", region },
                { "logToConsole", 1 },
                { "logLevel", 3 },
                { "dataDir", cacheDir },
                { "ua", $"TapSDK-Unity/{TapTapOnlineBattle.Version}" },
                { "lang", "zh-CN" },
                { "platform", "PC" },
                { "deviceId", deviceID },
                { "sdkArtifact", "Unity" },
                { "sdkModuleVer", TapTapOnlineBattle.Version },
            };
            string config = JsonConvert.SerializeObject(initConfig);
            int result = TapOnlineBattleWrapper.Init(
                config,
                new TapOnlineBattleWrapper.OnlineBattleCallbacks()
                {
                    ConnectCB = OnConnect,
                    DisconnectCB = OnDisconnected,
                    CreateRoomCB = OnCreateRoom,
                    MatchRoomCB = OnMatchRoom,
                    GetRoomListCB = OnGetRoomList,
                    JoinRoomCB = OnJoinRoom,
                    LeaveRoomCB = OnLeaveRoom,
                    UpdatePlayerCustomStatusCB = OnUpdatePlayerCustomStatus,
                    UpdatePlayerCustomPropertiesCB = OnUpdatePlayerCustomProperties,
                    UpdateRoomPropertiesCB = OnUpdateRoomProperties,
                    KickRoomPlayerCB = OnKickRoomPlayer,
                    StartFrameSyncCB = OnStartFrameSync,
                    SendFrameInputCB = OnSendFrameInput,
                    SendCustomMessageCB = OnSendCustomMessage,
                    StopFrameSyncCB = OnStopFrameSync,

                    EnterRoomNotificationCB = OnEnterRoomNotification,
                    LeaveRoomNotificationCB = OnLeaveRoomNotification,
                    PlayerOfflineNotificationCB = OnPlayerOfflineNotification,
                    PlayerCustomStatusNotificationCB = OnPlayerCustomStatusNotification,
                    PlayerCustomPropertiesNotificationCB = OnPlayerCustomPropertiesNotification,
                    RoomPropertiesNotificationCB = OnRoomPropertiesNotification,
                    RoomPlayerKickedNotificationCB = OnRoomPlayerKickedNotification,
                    FrameSyncStartNotificationCB = OnFrameSyncStartNotification,
                    FrameSyncCB = OnFrameSync,
                    CustomMessageNotificationCB = OnCustomMessageNotification,
                    FrameSyncStopNotificationCB = OnFrameSyncStopNotification,
                    BattleServiceErrorCB = OnBattleServiceError,
                    DisconnectNotificationCB = OnDisconnectNotification,
                }
            );
            TapLog.Log("TapOnlineBattleStandalone init result = " + result);
            if (result == 0)
            {
                _hasInitNative = true;
                EventManager.AddListener(EventManager.OnTapUserChanged, OnLoginInfoChanged);
            }
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <returns> 用户 playerId </returns>
        public static Task<string> Connect()
        {
            return CheckStateAndCallNativeMethod<string>(
                MethodName.Connect,
                nativeAsyncAction: async (requestID) =>
                {
                    TapTapAccount tapAccount = await GetCurrentAccount();
                    Dictionary<string, object> initConfig = new Dictionary<string, object>()
                    {
                        { "clientId", SdkOptions.clientId },
                        { "clientToken", SdkOptions.clientToken },
                        {
                            "accessToken",
                            new Dictionary<string, string>()
                            {
                                { "kid", tapAccount.accessToken.kid },
                                { "key", tapAccount.accessToken.macKey },
                            }
                        },
                    };
                    string config = JsonConvert.SerializeObject(initConfig);
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleConnect(requestID, config);
                }
            );
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public static Task Disconnect()
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.DisConnect,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleDisconnect(
                        requestID,
                        SdkOptions.clientId
                    );
                },
                needCheckLoginState: false
            );
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="createRoomConfig"> 创建房间配置</param>
        /// <returns> 房间信息 </returns>
        public static Task<RoomInfo> CreateRoom(CreateRoomConfig createRoomConfig)
        {
            return CheckStateAndCallNativeMethod<RoomInfo>(
                MethodName.CreateRoom,
                requestID =>
                {
                    string config = JsonConvert.SerializeObject(createRoomConfig);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int createResult = TapOnlineBattleWrapper.TapSdkOnlineBattleCreateRoom(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return createResult;
                }
            );
        }

        /// <summary>
        /// 匹配房间
        /// </summary>
        /// <param name="mathchRoomConfig"> 匹配房间配置 </param>
        /// <returns> 房间信息</returns>
        public static Task<RoomInfo> MatchRoom(MatchRoomConfig mathchRoomConfig)
        {
            return CheckStateAndCallNativeMethod<RoomInfo>(
                MethodName.MatchRoom,
                requestID =>
                {
                    string config = JsonConvert.SerializeObject(mathchRoomConfig);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int createResult = TapOnlineBattleWrapper.TapSdkOnlineBattleMatchRoom(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return createResult;
                }
            );
        }

        /// <summary>
        /// 获取房间列表
        /// </summary>
        /// <param name="roomType"> 房间类型 </param>
        /// <param name="offset"> 请求偏移量，默认为 0 </param>
        /// <param name="limit"> 请求获取的房间数量，默认为 20 </param>
        /// <returns> 房间信息列表信息 </returns>
        public static Task<RoomListData> GetRoomList(
            string roomType,
            int offset = 0,
            int limit = 20
        )
        {
            return CheckStateAndCallNativeMethod<RoomListData>(
                MethodName.GetRoomList,
                requestID =>
                {
                    Dictionary<string, object> requestData = new Dictionary<string, object>()
                    {
                        { "roomType", roomType },
                        { "offset", offset },
                        { "limit", limit },
                    };
                    string config = JsonConvert.SerializeObject(requestData);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int getResult = TapOnlineBattleWrapper.TapSdkOnlineBattleGetRoomList(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return getResult;
                }
            );
        }

        /// <summary>
        /// 进入房间
        /// </summary>
        /// <param name="roomId"> 房间ID </param>
        /// <param name="playerConfig"> 玩家配置，选填</param>
        /// <returns> 房间信息 </returns>
        public static Task<RoomInfo> JoinRoom(string roomId, PlayerConfig playerConfig)
        {
            return CheckStateAndCallNativeMethod<RoomInfo>(
                MethodName.JoinRoom,
                requestID =>
                {
                    Dictionary<string, object> requestData = new Dictionary<string, object>()
                    {
                        { "roomId", roomId },
                        { "playerCfg", playerConfig },
                    };
                    string config = JsonConvert.SerializeObject(requestData);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int getResult = TapOnlineBattleWrapper.TapSdkOnlineBattleJoinRoom(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return getResult;
                }
            );
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        /// <returns></returns>
        public static Task LeaveRoom()
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.LeaveRoom,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleLeaveRoom(
                        requestID,
                        SdkOptions.clientId
                    );
                }
            );
        }

        /// <summary>
        /// 更新玩家自定义状态请求
        /// </summary>
        /// <param name="status"> 玩家自定义状态，具体含义由游戏方定义 </param>
        /// <returns></returns>
        public static Task UpdatePlayerCustomStatus(int status)
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.UpdatePlayerCustomStatus,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleUpdatePlayerCustomStatus(
                        requestID,
                        SdkOptions.clientId,
                        status
                    );
                }
            );
        }

        /// <summary>
        /// 更新玩家自定义属性请求，在房间里，且未开战时才能调用
        /// </summary>
        /// <param name="status"> 玩家自定义属性，必须是utf8字符串，具体格式和含义由游戏方定义，最大2048字节 </param>
        /// <returns></returns>
        public static Task UpdatePlayerCustomProperties(string properties)
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.UpdatePlayerCustomProperties,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleUpdatePlayerCustomProperties(
                        requestID,
                        SdkOptions.clientId,
                        properties
                    );
                }
            );
        }

        /// <summary>
        /// 更新房间属性，在房间里，且未开战时才能调用，仅限房主调用
        /// </summary>
        /// <param name="roomName"> 房间名称 </param>
        /// <param name="properties"> 房间自定义属性，最大2048字节 </param>
        public static Task UpdateRoomProperties(string roomName, string properties)
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.UpdateRoomProperties,
                requestID =>
                {
                    Dictionary<string, object> requestData = new Dictionary<string, object>()
                    {
                        { "name", roomName },
                        { "customProperties", properties },
                    };
                    string config = JsonConvert.SerializeObject(requestData);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int getResult = TapOnlineBattleWrapper.TapSdkOnlineBattleUpdateRoomProperties(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return getResult;
                }
            );
        }

        /// <summary>
        /// 开始帧同步
        /// </summary>
        /// <returns></returns>
        public static Task StartFrameSync()
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.StartFrameSync,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleStartFrameSync(
                        requestID,
                        SdkOptions.clientId
                    );
                }
            );
        }

        /// <summary>
        /// 发送玩家对战操作，同一帧允许发送多次操作，无需等待请求完成的回调，即可发送下一个操作
        /// </summary>
        /// <param name="data"> 玩家操作请求 </param>
        /// <returns></returns>
        public static Task SendFrameInput(string data)
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.SendFrameInput,
                requestID =>
                {
                    Dictionary<string, object> requestData = new Dictionary<string, object>()
                    {
                        { "data", data },
                    };
                    string config = JsonConvert.SerializeObject(requestData);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int getResult = TapOnlineBattleWrapper.TapSdkOnlineBattleSendFrameInput(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return getResult;
                }
            );
        }

        /// <summary>
        /// 停止帧同步
        /// </summary>
        /// <returns></returns>
        public static Task StopFrameSync()
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.StopFrameSync,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleStopFrameSync(
                        requestID,
                        SdkOptions.clientId
                    );
                }
            );
        }

        /// <summary>
        /// 踢玩家出房间,在房间里，且未开战时才能调用，仅限房主调用
        /// </summary>
        /// <param name="playerId"> 被踢玩家ID </param>
        /// <returns></returns>
        public static Task KickRoomPlayer(string playerId)
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.KickRoomPlayer,
                requestID =>
                {
                    return TapOnlineBattleWrapper.TapSdkOnlineBattleKickRoomPlayer(
                        requestID,
                        SdkOptions.clientId,
                        playerId
                    );
                }
            );
        }

        /// <summary>
        /// 发送自定义消息，每秒允许调用20次，无需等待请求完成的回调，即可发送下一条消息
        /// </summary>
        /// <param name="msg"> 自定义消息，格式由开发者决定，必须是utf8字符串，最大2048字节 </param>
        /// <param name="type"> 消息接收者类型。0：房间内所有玩家，不包括发送者；1：发送给指定玩家</param>
        /// <param name="receivers"> 当type==1时有效，发送给该字段指定的玩家，最多20个ID</param>
        /// <returns></returns>
        public static Task SendCustomMessage(
            string msg,
            int type = 0,
            List<string> receivers = null
        )
        {
            return CheckStateAndCallNativeMethod<object>(
                MethodName.SendCustomMessage,
                requestID =>
                {
                    Dictionary<string, object> requestData = new Dictionary<string, object>()
                    {
                        { "msg", msg },
                        { "type", type },
                        { "receivers", receivers },
                    };
                    string config = JsonConvert.SerializeObject(requestData);
                    var (ptr, len, free) = TapOnlineBattleUtils.StringToPtr(config);
                    int getResult = TapOnlineBattleWrapper.TapSdkOnlineBattleSendCustomMessage(
                        requestID,
                        SdkOptions.clientId,
                        ptr,
                        len
                    );
                    // 调用后释放内存
                    free();
                    return getResult;
                }
            );
        }

        /// <summary>
        /// 使用指定的种子seed，创建新的随机数生成器对象
        /// </summary>
        /// <param name="seed"> 随机数生成器的种子，请使用BattleStartNotification里返回的seed，以保证对战中所有玩家生成一致的随机数</param>
        /// <returns>  随机数生成器对象 </returns>
        public static RandomNumberGenerator NewRandomNumberGenerator(int seed)
        {
            return new RandomNumberGenerator(seed);
        }

        /// <summary>
        /// 检查初始化与登录状态，并调用设置的 Native 方法, 同步和异步方法二选一
        /// </summary>
        /// <typeparam name="T"> 返回的参数类型 </typeparam>
        /// <param name="method"> 方法名称，用于内部埋点 </param>
        /// <param name="nativeAction"> native 方法 </param>
        /// <param name="nativeAsyncAction"> native 异步方法 </param>
        /// <param name="needCheckLoginState"> 是否需要检验登录状态 </param>
        /// <returns></returns>
        internal static Task<T> CheckStateAndCallNativeMethod<T>(
            MethodName methodName,
            Func<long, int> nativeAction = null,
            Func<long, Task<int>> nativeAsyncAction = null,
            bool needCheckLoginState = true
        )
        {
            string method = TapOnlineBattleUtils.GetTrackMethodName(methodName);
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            string sessionId = Guid.NewGuid().ToString();
            // 判断是否需要发送埋点
            bool needSendTrack = IsTrackingRequired(methodName);
            if (needSendTrack)
            {
                TapOnlineBattleTracker.Instance.TrackStart(method, sessionId);
            }

            async void internalMethod()
            {
                /// Windows 平台判断是否通过启动校验
                if (!CheckPCLaunchState())
                {
                    TapException tapException = new TapException(
                        TapOnlineBattleConstant.ERROR_NOT_LAUNCHED_BY_TAP_CLIENT,
                        "IsLaunchedFromTapTapPC validation failed"
                    );
                    if (needSendTrack)
                    {
                        TapOnlineBattleTracker.Instance.TrackFailure(
                            method,
                            sessionId,
                            tapException.Code,
                            tapException.Message
                        );
                    }
                    taskCompletionSource.TrySetException(tapException);
                    return;
                }

                bool isStateValid = await CheckInitAndLoginState(needCheckLoginState);
                if (!isStateValid)
                {
                    TapException initOrLoginException = new TapException(
                        TapOnlineBattleConstant.ERROR_NOT_INIT_OR_LOGIN,
                        "not init or login"
                    );
                    if (needSendTrack)
                    {
                        TapOnlineBattleTracker.Instance.TrackFailure(
                            method,
                            sessionId,
                            initOrLoginException.Code,
                            initOrLoginException.Message
                        );
                    }
                    taskCompletionSource.TrySetException(initOrLoginException);
                    return;
                }

                long requestID = TapOnlineBattleUtils.GenerateRequestID();
                if (needSendTrack)
                {
                    AsyncNativeSessionIdMaps.TryAdd(requestID, sessionId);
                }
                AsyncNativeTaskMaps.TryAdd(requestID, taskCompletionSource);
                int getResult = 0;
                if (nativeAction != null)
                {
                    getResult = nativeAction(requestID);
                }
                else
                {
                    getResult = await nativeAsyncAction(requestID);
                }
                TapLog.Log(method + " native result = " + getResult);
                // 此时不会触发异步回调，直接触发异常给开发者
                if (getResult != 0)
                {
                    if (needSendTrack)
                    {
                        AsyncNativeSessionIdMaps.TryRemove(requestID, out _);
                        TapOnlineBattleTracker.Instance.TrackFailure(
                            method,
                            sessionId,
                            getResult,
                            "native invoke error : " + getResult
                        );
                    }
                    AsyncNativeTaskMaps.TryRemove(requestID, out _);
                    taskCompletionSource.TrySetException(
                        new TapException(getResult, "native error : " + getResult)
                    );
                }
            }
            internalMethod();
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 检查是否通过 PC 启动校验
        /// </summary>
        private static bool CheckPCLaunchState()
        {
#if UNITY_STANDALONE_WIN
            return TapClientStandalone.isPassedInLaunchedFromTapTapPCCheck();
#endif
            return true;
        }

        /// <summary>
        /// 检查初始化和登录状态
        /// </summary>
        /// <param name="needCheckLoginState"> 是否需要检验登录状态</param>
        /// <returns></returns>
        private static async Task<bool> CheckInitAndLoginState(bool needCheckLoginState)
        {
            if (SdkOptions != null)
            {
                lock (_lockObj)
                {
                    if (!_hasInitNative)
                    {
                        TapOnlineBattleUtils.Log("not init success, so return", true);
                        return false;
                    }
                }
                if (needCheckLoginState)
                {
                    TapTapAccount tapAccount = await GetCurrentAccount();
                    if (tapAccount != null && !string.IsNullOrEmpty(tapAccount.openId))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 注册监听回调，同一个回调只会注册一次
        /// </summary>
        /// <param name="callback"> 实现 <see cref="ITapOnlineBattleCallback"/> 实例 </param>
        public static void RegisterOnlineBattleCallback(ITapOnlineBattleCallback callback)
        {
            if (OnlineBattleCallbacks == null)
            {
                OnlineBattleCallbacks = new HashSet<ITapOnlineBattleCallback>();
            }
            if (callback != null && !OnlineBattleCallbacks.Contains(callback))
            {
                OnlineBattleCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// 移除注册回调
        /// </summary>
        /// <param name="callback"> 实现 <see cref="ITapOnlineBattleCallback"/> 实例</param>
        public static void UnRegisterOnlineBattleCallback(ITapOnlineBattleCallback callback)
        {
            if (callback != null && OnlineBattleCallbacks != null)
            {
                OnlineBattleCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 是否需要上报数据埋点
        /// </summary>
        /// <param name="methodName"> 方法名 </param>
        /// <returns></returns>
        private static bool IsTrackingRequired(MethodName methodName)
        {
            return methodName != MethodName.SendFrameInput
                && methodName != MethodName.SendCustomMessage;
        }

        /// <summary>
        /// 监听用户登录状态，当发生变更时主动断开连接
        /// </summary>
        private static async void OnLoginInfoChanged(object value)
        {
            TapLog.Log("TapLogin state changed, so disconnect");
            if (CurrentTapAccount != null)
            {
                await Disconnect();
            }
            await GetCurrentAccount(false);
        }

        /// <summary>
        /// 获取当前用户登录信息
        /// </summary>
        /// <param name="useCache"> 是否使用缓存</param>
        /// <returns></returns>
        private static async Task<TapTapAccount> GetCurrentAccount(bool useCache = true)
        {
            if (!useCache || CurrentTapAccount == null)
            {
                CurrentTapAccount = await TapOnlineBattleUtils.GetCurrentTapAccount();
            }
            return CurrentTapAccount;
        }
    }
}
#endif