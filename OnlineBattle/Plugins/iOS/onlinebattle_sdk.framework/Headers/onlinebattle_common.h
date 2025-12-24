#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// 建连、创建房间、匹配房间等请求的回调
//   - reqID 原样返回调用接口时传入的reqID，用于调用方对应到原始请求
//   - clientID TapSDK为client_id，Tap Miniapp为miniapp_id
//   - success 1表示请求处理成功，此时使用对应的结构体解析返回；0表示处理失败，此时使用Error解析返回
//   - data 根据success的值，可能是处理成功的返回数据，或者是Error数据
//   - dataLen data的长度，字节
typedef void (*RequestCallback)(int64_t reqID, const char* clientID, int success, const void* data, uint32_t dataLen);

// 登出回调
//   - reqID 原样返回调用Disconnect()时传入的reqID，用于调用方对应到原始请求
//   - clientID TapSDK为client_id，Tap Miniapp为miniapp_id
typedef void (*DisconnectCallback)(int64_t reqID, const char* clientID);

// 服务端消息通知回调，对战开始通知、帧数据通知、离开房间通知、被踢下线通知、断线通知、对战服务异常通知等等
//   - clientID TapSDK为client_id，Tap Miniapp为miniapp_id
typedef void (*NotificationCallback)(const char* clientID, const void* data, uint32_t dataLen);

struct OnlineBattleCallbacks {
    RequestCallback ConnectCB;                      // 建连请求回调，成功时使用ConnectResponse解析返回
    DisconnectCallback DisconnectCB;                // 断连请求回调
    RequestCallback CreateRoomCB;                   // 创建房间请求回调，成功时使用CreateRoomResponse解析返回
    RequestCallback MatchRoomCB;                    // 匹配房间请求回调，成功时使用MatchRoomResponse解析返回
    RequestCallback GetRoomListCB;                  // 获取房间列表请求回调，成功时使用GetRoomListResponse解析返回
    RequestCallback JoinRoomCB;                     // 加入房间请求回调，成功时使用JoinRoomResponse解析返回
    RequestCallback LeaveRoomCB;                    // 离开房间请求回调，成功时无需解析返回
    RequestCallback UpdatePlayerCustomStatusCB;     // 更新玩家自定义状态请求回调，成功时无需解析返回
    RequestCallback UpdatePlayerCustomPropertiesCB; // 更新玩家自定义属性请求回调，成功时无需解析返回
    RequestCallback UpdateRoomPropertiesCB;         // 更新房间属性请求回调，成功时无需解析返回
    RequestCallback KickRoomPlayerCB;               // 踢玩家出房间请求回调，成功时无需解析返回
    RequestCallback StartFrameSyncCB;               // 开始帧同步请求回调，成功时无需解析返回
    RequestCallback SendFrameInputCB;               // 发送操作数据请求回调，成功时无需解析返回
    RequestCallback SendCustomMessageCB;            // 发送自定义消息请求回调，成功时无需解析返回
    RequestCallback StopFrameSyncCB;                // 结束帧同步请求回调，成功时无需解析返回

    NotificationCallback EnterRoomNotificationCB;              // 通知房间内所有玩家，有新玩家进入房间，使用EnterRoomNotification解析返回
    NotificationCallback LeaveRoomNotificationCB;              // 通知房间内所有玩家，有玩家离开房间，使用LeaveRoomNotification解析返回
    NotificationCallback PlayerOfflineNotificationCB;          // 只有对战中的玩家离线才会触发，通知房间内所有玩家，使用PlayerOfflineNotification解析返回
    NotificationCallback PlayerCustomStatusNotificationCB;     // 玩家自定义状态变更时触发，通知房间内其他玩家，使用PlayerCustomStatusNotification解析返回
    NotificationCallback PlayerCustomPropertiesNotificationCB; // 玩家自定义状属性更时触发，通知房间内其他玩家，使用PlayerCustomPropertiesNotification解析返回
    NotificationCallback RoomPropertiesNotificationCB;         // 房间属性更新时触发，通知房间内其他玩家，使用RoomPropertiesNotification解析返回
    NotificationCallback RoomPlayerKickedNotificationCB;       // 玩家被踢出房间通知，使用RoomPlayerKickedNotification解析返回
    NotificationCallback FrameSyncStartNotificationCB;         // 通知房间内所有玩家，帧同步开始，使用FrameSyncStartNotification解析返回
    NotificationCallback FrameSyncCB;                          // 给房间内所有玩家同步最新的对战帧，使用FrameSynchronization解析返回
    NotificationCallback CustomMessageNotificationCB;          // 收到自定义消息时回调，使用CustomMessageNotification解析返回
    NotificationCallback FrameSyncStopNotificationCB;          // 通知房间内所有玩家，帧同步结束，使用FrameSyncStopNotification解析返回
    NotificationCallback BattleServiceErrorCB;                 // 对战服务异常时回调，dataLen为0，无需解析data。收到该回调时，端侧需要退出对战、房间、队伍等状态
    NotificationCallback DisconnectNotificationCB;             // 被踢、断线时回调。用Error解析data，如果是断线，可以重连；如果是被踢，不要重连
};

#ifdef __cplusplus
}
#endif
