using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AOT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using static TapSDK.OnlineBattle.TapOnlineBattleWrapper;

namespace TapSDK.OnlineBattle
{
    /// <summary>
    /// Native 回调实现
    /// </summary>
    public partial class TapTapOnlineBattle
    {
        #region 初始化回调实现

        // 登录请求回调，成功时使用SignInResponse解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnConnect(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            TapLog.Log("receive OnConnect ");
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<string> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                    TapLog.Log("sign in value = " + value);
                    Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                        Dictionary<string, object>
                    >(value);
                    MethodName method = MethodName.Connect;
                    if (success == RESULT_SUCCESS)
                    {
                        if (
                            dict.TryGetValue("playerId", out object playerObj)
                            && playerObj is string player
                        )
                        {
                            SendSuccessTrackEvent(reqID, method);
                            taskCompletionSource.TrySetResult(player);
                        }
                        else
                        {
                            var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                            SendFailTrackEvent(reqID, method, tapException);
                            taskCompletionSource.TrySetException(tapException);
                        }
                    }
                    else
                    {
                        var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        // 登出请求回调
        [MonoPInvokeCallback(typeof(DisConnectCallback))]
        private static void OnDisconnected(long reqID, string clientID)
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<object> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    SendSuccessTrackEvent(reqID, MethodName.DisConnect);
                    taskCompletionSource.SetResult(null);
                }
                AsyncNativeTaskMaps.TryRemove(reqID, out _);
            }
        }

        // 创建房间请求回调，成功时使用CreateRoomResponse解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnCreateRoom(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<RoomInfo> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    MethodName method = MethodName.CreateRoom;
                    string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                    TapLog.Log("onCreate room info = " + value);
                    if (success == RESULT_SUCCESS)
                    {
                        RoomInfo roomInfo = JsonConvert
                            .DeserializeObject<RoomInfoResult>(value)
                            .roomInfo;
                        if (roomInfo != null)
                        {
                            SendSuccessTrackEvent(reqID, method);
                            taskCompletionSource.TrySetResult(roomInfo);
                        }
                        else
                        {
                            var tapException = TapOnlineBattleUtils.GenetateExceptionByData(null);
                            SendFailTrackEvent(reqID, method, tapException);
                            taskCompletionSource.TrySetException(tapException);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                            Dictionary<string, object>
                        >(value);
                        var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        // 匹配房间请求回调，成功时使用MatchRoomResponse解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnMatchRoom(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<RoomInfo> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    MethodName method = MethodName.MatchRoom;
                    string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                    if (success == RESULT_SUCCESS)
                    {
                        RoomInfo roomInfo = JsonConvert
                            .DeserializeObject<RoomInfoResult>(value)
                            .roomInfo;
                        if (roomInfo != null)
                        {
                            SendSuccessTrackEvent(reqID, method);
                            taskCompletionSource.TrySetResult(roomInfo);
                        }
                        else
                        {
                            var tapException = TapOnlineBattleUtils.GenetateExceptionByData(null);
                            SendFailTrackEvent(reqID, method, tapException);
                            taskCompletionSource.TrySetException(tapException);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                            Dictionary<string, object>
                        >(value);
                        var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        // 获取房间列表请求回调，成功时使用GetRoomListResponse解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnGetRoomList(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<RoomListData> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                    MethodName method = MethodName.GetRoomList;
                    TapLog.Log("OnGetRoomList value = " + value);
                    if (success == RESULT_SUCCESS)
                    {
                        RoomListData roomList = JsonConvert.DeserializeObject<RoomListData>(value);
                        if (roomList != null)
                        {
                            SendSuccessTrackEvent(reqID, method);
                            taskCompletionSource.TrySetResult(roomList);
                        }
                        else
                        {
                            var tapException = TapOnlineBattleUtils.GenetateExceptionByData(null);
                            SendFailTrackEvent(reqID, method, tapException);
                            taskCompletionSource.TrySetException(tapException);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                            Dictionary<string, object>
                        >(value);
                        var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        // 加入房间请求回调，成功时使用JoinRoomResponse解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnJoinRoom(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<RoomInfo> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                    MethodName method = MethodName.JoinRoom;
                    TapLog.Log("join home roomInfo = " + value);
                    if (success == RESULT_SUCCESS)
                    {
                        RoomInfo roomInfo = JsonConvert
                            .DeserializeObject<RoomInfoResult>(value)
                            .roomInfo;
                        if (roomInfo != null)
                        {
                            TapLog.Log("join home roomInfo players= " + roomInfo.players.Count);
                            SendSuccessTrackEvent(reqID, method);
                            taskCompletionSource.TrySetResult(roomInfo);
                        }
                        else
                        {
                            var tapException = TapOnlineBattleUtils.GenetateExceptionByData(null);
                            SendFailTrackEvent(reqID, method, tapException);
                            taskCompletionSource.TrySetException(tapException);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                            Dictionary<string, object>
                        >(value);
                        var tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        // 离开房间请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnLeaveRoom(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(reqID, success, data, dataLen, MethodName.LeaveRoom);
        }

        // 更新玩家自定义状态请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnUpdatePlayerCustomStatus(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(
                reqID,
                success,
                data,
                dataLen,
                MethodName.UpdatePlayerCustomStatus
            );
        }

        // 更新玩家自定义属性请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnUpdatePlayerCustomProperties(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(
                reqID,
                success,
                data,
                dataLen,
                MethodName.UpdatePlayerCustomProperties
            );
        }

        // 更新房间属性请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnUpdateRoomProperties(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(
                reqID,
                success,
                data,
                dataLen,
                MethodName.UpdateRoomProperties
            );
        }

        // 开始对战请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnStartFrameSync(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(reqID, success, data, dataLen, MethodName.StartFrameSync);
        }

        // 发送操作数据请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnSendFrameInput(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(reqID, success, data, dataLen, MethodName.SendFrameInput);
        }

        // 对战结束请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnStopFrameSync(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(reqID, success, data, dataLen, MethodName.StopFrameSync);
        }

        // 踢玩家出房间请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnKickRoomPlayer(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(reqID, success, data, dataLen, MethodName.KickRoomPlayer);
        }

        // 发送自定义消息请求回调，成功时无需解析返回
        [MonoPInvokeCallback(typeof(RequestCallback))]
        private static void OnSendCustomMessage(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        )
        {
            HandleAsyncCallbackWithData(
                reqID,
                success,
                data,
                dataLen,
                MethodName.SendCustomMessage
            );
        }

        // 通知房间内所有玩家，有新玩家进入房间，使用EnterRoomNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnEnterRoomNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, object> result = JsonConvert.DeserializeObject<
                Dictionary<string, object>
            >(value);
            if (
                result.TryGetValue("roomId", out var roomObj)
                && roomObj is string roomId
                && result.TryGetValue("playerInfo", out var playerObj)
                && playerObj is JObject playerInfoData
            )
            {
                PlayerInfo playerInfo = playerInfoData.ToObject<PlayerInfo>();
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerEntered(roomId, playerInfo);
                        }
                    }
                });
            }
        }

        // 通知房间内所有玩家，有玩家离开房间，使用LeaveRoomNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnLeaveRoomNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (
                result.TryGetValue("roomId", out string roomId)
                && result.TryGetValue("roomOwnerId", out string roomOwnerId)
                && result.TryGetValue("playerId", out string playerId)
            )
            {
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerLeft(roomId, roomOwnerId, playerId);
                        }
                    }
                });
            }
        }

        // 只有对战中的玩家离线才会触发，通知房间内所有玩家，使用PlayerOfflineNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnPlayerOfflineNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (
                result.TryGetValue("roomId", out string roomId)
                && result.TryGetValue("roomOwnerId", out string roomOwnerId)
                && result.TryGetValue("playerId", out string playerId)
            )
            {
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerOffline(roomId, roomOwnerId, playerId);
                        }
                    }
                });
            }
        }

        // 玩家自定义状态变更时触发，通知房间内其他玩家，使用PlayerCustomStatusNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnPlayerCustomStatusNotification(
            string clientID,
            IntPtr data,
            uint dataLen
        )
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, object> result = JsonConvert.DeserializeObject<
                Dictionary<string, object>
            >(value);
            if (
                result.TryGetValue("playerId", out object playerIdObj)
                && playerIdObj is string playerId
            )
            {
                int status = 0;
                if (result.TryGetValue("status", out object statusObj))
                {
                    status = Convert.ToInt32(statusObj);
                }
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerCustomStatusChanged(playerId, status);
                        }
                    }
                });
            }
        }

        // 玩家自定义状属性更时触发，通知房间内其他玩家，使用PlayerCustomPropertiesNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnPlayerCustomPropertiesNotification(
            string clientID,
            IntPtr data,
            uint dataLen
        )
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (result.TryGetValue("playerId", out string playerId))
            {
                string properties = result.TryGetValue("properties", out string temp) ? temp : "";
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerCustomPropertiesChanged(playerId, properties);
                        }
                    }
                });
            }
        }

        // 房间自定义属性更新时触发，通知房间内其他玩家，使用RoomPropertiesNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnRoomPropertiesNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (result.TryGetValue("id", out string id))
            {
                string name = result.TryGetValue("name", out string nameValue) ? nameValue : "";
                string customProperties = result.TryGetValue(
                    "customProperties",
                    out string customPropertiesValue
                )
                    ? customPropertiesValue
                    : "";
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnRoomPropertiesChanged(id, name, customProperties);
                        }
                    }
                });
            }
        }

        // 通知房间内所有玩家，对战（帧同步）开始，使用BattleStartNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnFrameSyncStartNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            TapLog.Log("OnBattleStartNotification value = " + value);
            FrameSyncInfo battleInfo = JsonConvert.DeserializeObject<FrameSyncInfo>(value);
            TapOnlineBattleUtils.RunOnMainThread(() =>
            {
                if (OnlineBattleCallbacks != null)
                {
                    foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                    {
                        callback.OnFrameSyncStarted(battleInfo);
                    }
                }
            });
        }

        // 给房间内所有玩家同步最新的对战帧数据，使用BattleFrameSynchronization解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnFrameSync(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            FrameData inputInfo = JsonConvert.DeserializeObject<FrameData>(value);
            TapOnlineBattleUtils.RunOnMainThread(() =>
            {
                if (OnlineBattleCallbacks != null)
                {
                    foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                    {
                        callback.OnFrameReceived(inputInfo);
                    }
                }
            });
        }

        // 通知房间内所有玩家，对战结束，使用BattleStopNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnFrameSyncStopNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            TapLog.Log("OnBattleStopNotification " + value);
            Dictionary<string, object> result = JsonConvert.DeserializeObject<
                Dictionary<string, object>
            >(value);
            if (
                result.TryGetValue("roomId", out var roomIdObj)
                && roomIdObj is string roomId
                && result.TryGetValue("frameSyncId", out var frameSyncIdObj)
            )
            {
                TapLog.Log("OnBattleStopNotification notify player");
                int frameSyncId = Convert.ToInt32(frameSyncIdObj);
                int reason = 0;
                if (result.TryGetValue("reason", out var reasonObj))
                {
                    reason = Convert.ToInt32(reasonObj);
                }
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnFrameSyncStopped(roomId, frameSyncId, reason);
                        }
                    }
                });
            }
        }

        // 玩家被踢出房间通知，使用RoomPlayerKickedNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnRoomPlayerKickedNotification(
            string clientID,
            IntPtr data,
            uint dataLen
        )
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (
                result.TryGetValue("roomId", out string roomId)
                && result.TryGetValue("playerId", out string playerId)
            )
            {
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnPlayerKicked(roomId, playerId);
                        }
                    }
                });
            }
        }

        // 收到自定义消息时回调，使用CustomMessageNotification解析返回
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnCustomMessageNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, string> result = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(value);
            if (result.TryGetValue("playerId", out string playerId))
            {
                string msg = result.TryGetValue("msg", out string msgValue) ? msgValue : "";
                TapOnlineBattleUtils.RunOnMainThread(() =>
                {
                    if (OnlineBattleCallbacks != null)
                    {
                        foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                        {
                            callback.OnCustomMessageReceived(playerId, msg);
                        }
                    }
                });
            }
        }

        // 对战服务异常时回调，dataLen为0，无需解析data。收到该回调时，端侧需要退出对战、房间、队伍等状态
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnBattleServiceError(string clientID, IntPtr data, uint dataLen)
        {
            TapOnlineBattleUtils.RunOnMainThread(() =>
            {
                if (OnlineBattleCallbacks != null)
                {
                    foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                    {
                        callback.OnBattleServiceError();
                    }
                }
            });
        }

        // 被踢、断线时回调。用Error解析data，如果是断线，可以重连；如果是被踢，不要重连
        [MonoPInvokeCallback(typeof(NotificationCallback))]
        private static void OnDisconnectNotification(string clientID, IntPtr data, uint dataLen)
        {
            string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                Dictionary<string, object>
            >(value);
            TapException tapException = TapOnlineBattleUtils.GenetateExceptionByData(dict);

            if (OnlineBattleCallbacks != null)
            {
                foreach (ITapOnlineBattleCallback callback in OnlineBattleCallbacks)
                {
                    callback.OnDisconnected(tapException.Code, tapException.Message);
                }
            }
        }

        #endregion

        /// <summary>
        /// 处理无返回结果的异步回调
        /// </summary>
        private static void HandleAsyncCallbackWithData(
            long reqID,
            int success,
            IntPtr data,
            uint dataLen,
            MethodName method
        )
        {
            if (
                AsyncNativeTaskMaps.TryGetValue(reqID, out var obj)
                && obj is TaskCompletionSource<object> taskCompletionSource
            )
            {
                if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
                {
                    if (success == RESULT_SUCCESS)
                    {
                        SendSuccessTrackEvent(reqID, method);
                        taskCompletionSource.TrySetResult(null);
                    }
                    else
                    {
                        string value = TapOnlineBattleUtils.PtrToString(data, dataLen);
                        Dictionary<string, object> dict = JsonConvert.DeserializeObject<
                            Dictionary<string, object>
                        >(value);
                        TapException tapException = TapOnlineBattleUtils.GenetateExceptionByData(
                            dict
                        );
                        SendFailTrackEvent(reqID, method, tapException);
                        taskCompletionSource.TrySetException(tapException);
                    }
                }
            }
            AsyncNativeTaskMaps.TryRemove(reqID, out _);
        }

        /// <summary>
        /// 发送请求失败埋点，会忽略 sendInput 和 sendCustomMsg 方法
        /// </summary>
        /// <param name="reqID"> 请求唯一 ID </param>
        /// <param name="method"> 调用方法名 </param>
        /// <param name="tapException"> 异常数据 </param>
        private static void SendFailTrackEvent(
            long reqID,
            MethodName method,
            TapException tapException
        )
        {
            if (!IsTrackingRequired(method))
            {
                return;
            }
            string methodValue = TapOnlineBattleUtils.GetTrackMethodName(method);
            if (AsyncNativeSessionIdMaps.TryGetValue(reqID, out string sessionId))
            {
                TapOnlineBattleTracker.Instance.TrackFailure(
                    methodValue,
                    sessionId,
                    tapException.Code,
                    tapException.Message
                );
                AsyncNativeSessionIdMaps.TryRemove(reqID, out _);
            }
        }

        /// <summary>
        /// 发送成功埋点，会忽略 sendInput 和 sendCustomMsg方法
        /// </summary>
        /// <param name="reqID"> 请求 ID</param>
        /// <param name="method"> 调用方法 </param>
        private static void SendSuccessTrackEvent(long reqID, MethodName method)
        {
            if (!IsTrackingRequired(method))
            {
                return;
            }
            string methodValue = TapOnlineBattleUtils.GetTrackMethodName(method);
            if (AsyncNativeSessionIdMaps.TryGetValue(reqID, out string sessionId))
            {
                TapOnlineBattleTracker.Instance.TrackSuccess(methodValue, sessionId);
                AsyncNativeSessionIdMaps.TryRemove(reqID, out _);
            }
        }
    }
}
