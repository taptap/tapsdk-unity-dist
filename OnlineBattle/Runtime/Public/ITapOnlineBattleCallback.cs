namespace TapSDK.OnlineBattle
{
    /// <summary>
    /// 全局事件监听回调
    /// </summary>
    public interface ITapOnlineBattleCallback
    {
        /// <summary>
        /// 与服务端连接断开，建议当错误码为网络异常时进行重连
        /// </summary>
        /// <param name="code"> 错误码，参考 <see cref="TapOnlineBattleConstant"/> </param>
        /// <param name="msg"> 错误信息 </param>
        void OnDisconnected(int code, string msg);

        /// <summary>
        /// 对战服务端异常，开发者需要退出对战、房间、队伍等状态，此时处于 connect 之后的状态
        /// </summary>
        void OnBattleServiceError();

        /// <summary>
        /// 房间属性变更
        /// </summary>
        /// <param name="id"> 房间 ID </param>
        /// <param name="name"> 房间名称 </param>
        /// <param name="customProperties"> 房间自定义属性 </param>
        void OnRoomPropertiesChanged(string id, string name, string customProperties);

        /// <summary>
        /// 玩家更新自定义属性
        /// </summary>
        /// <param name="playerId"> 更新了自定义属性的玩家ID </param>
        /// <param name="properties"> 新的属性值 </param>
        void OnPlayerCustomPropertiesChanged(string playerId, string properties);

        /// <summary>
        /// 玩家更新自定义状态
        /// </summary>
        /// <param name="playerId"> 更新了自定义状态的玩家ID </param>
        /// <param name="status"> 新的状态 </param>
        void OnPlayerCustomStatusChanged(string playerId, int status);

        /// <summary>
        /// 开始帧同步
        /// </summary>
        /// <param name="frameSyncInfo"> 帧同步基础信息 </param>
        void OnFrameSyncStarted(FrameSyncInfo frameSyncInfo);

        /// <summary>
        /// 收到帧同步数据回调
        /// </summary>
        /// <param name="frameData"> 帧数据 </param>
        void OnFrameReceived(FrameData frameData);

        /// <summary>
        /// 停止帧同步
        /// </summary>
        /// <param name="roomId"> 房间 ID </param>
        /// <param name="frameSyncId"> 帧同步 ID，房间内唯一 </param>
        /// <param name="reason"> 原因 0：房主主动结束，1：因30分钟超时结束 </param>
        void OnFrameSyncStopped(string roomId, int frameSyncId, int reason);

        /// <summary>
        /// 玩家离线
        /// </summary>
        /// <param name="roomId"> 房间ID </param>
        /// <param name="roomOwnerId"> 房主ID。如果离线的是房主，则roomOwnerId为新房主ID；反之，为原房主ID</param>
        /// <param name="playerId"> 离线玩家ID </param>
        void OnPlayerOffline(string roomId, string roomOwnerId, string playerId);

        /// <summary>
        /// 玩家离开房间
        /// </summary>
        /// <param name="roomId"> 房间ID </param>
        /// <param name="roomOwnerId"> 房主ID。如果离开的是房主，则roomOwnerId为新房主ID；反之，为原房主ID</param>
        /// <param name="playerId"> 离开房间的玩家ID </param>
        void OnPlayerLeft(string roomId, string roomOwnerId, string playerId);

        /// <summary>
        /// 玩家进入房间
        /// </summary>
        /// <param name="roomId"> 房间ID </param>
        /// <param name="playerInfo"> 玩家信息 </param>
        void OnPlayerEntered(string roomId, PlayerInfo playerInfo);

        /// <summary>
        /// 收到玩家自定义消息
        /// </summary>
        /// <param name="playerId"> 消息发送者玩家ID </param>
        /// <param name="msg"> 自定义消息，格式由开发者决定，必须是utf8字符串，最大2048字节 </param>
        void OnCustomMessageReceived(string playerId, string msg);

        /// <summary>
        /// 玩家被踢出
        /// </summary>
        /// <param name="roomId"> 被踢玩家所属房间ID </param>
        /// <param name="playerId"> 被踢玩家ID </param>
        void OnPlayerKicked(string roomId, string playerId);
    }
}
