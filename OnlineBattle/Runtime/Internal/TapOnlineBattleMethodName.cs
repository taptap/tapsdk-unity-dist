using System.ComponentModel;

namespace TapSDK.OnlineBattle
{
    internal enum MethodName
    {
        [Description("connect")]
        Connect,

        [Description("disconnect")]
        DisConnect,

        [Description("createRoom")]
        CreateRoom,

        [Description("matchRoom")]
        MatchRoom,

        [Description("getRoomList")]
        GetRoomList,

        [Description("joinRoom")]
        JoinRoom,

        [Description("leaveRoom")]
        LeaveRoom,

        [Description("updatePlayerCustomStatus")]
        UpdatePlayerCustomStatus,

        [Description("updatePlayerCustomProperties")]
        UpdatePlayerCustomProperties,

        [Description("updateRoomProperties")]
        UpdateRoomProperties,

        [Description("startFrameSync")]
        StartFrameSync,

        [Description("sendFrameInput")]
        SendFrameInput,

        [Description("stopFrameSync")]
        StopFrameSync,

        [Description("kickRoomPlayer")]
        KickRoomPlayer,

        [Description("sendCustomMessage")]
        SendCustomMessage,
    }
}
