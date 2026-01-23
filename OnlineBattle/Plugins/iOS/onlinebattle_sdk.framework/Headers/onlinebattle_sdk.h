#pragma once

#include "export_api.h"
#include "onlinebattle_common.h"

///////////////////////////////////////////////////////////////////////////////////////

#ifdef __cplusplus
extern "C" {
#endif

/**
 * 初始化接口，只需要调用一次。非线程安全，并发调用可能崩溃。
 *
 * @param jsonCfg 初始化配置，JSON 格式：
 *
 *     TapSDK参数格式
 *     {
 *       "region": 2,
 *       "logToConsole": 1,
 *       "logLevel": 3,
 *       "dataDir": "/tmp/onlinebattle",
 *       "ua": "TapSDK-Android/3.28.0",
 *       "lang": "zh-CN",
 *       "platform": "Android",
 *       "deviceId": "123456",
 *       "sdkArtifact": "Android",
 *       "sdkModuleVer": "4.6.0-alpha.7"
 *     }
 *
 *     Tap Miniapp参数格式
 *     {
 *       "region": 2,
 *       "logToConsole": 1,
 *       "logLevel": 3,
 *       "dataDir": "/tmp/onlinebattle",
 *       "ua": "TapSDK-Android/3.28.0",
 *       "lang": "zh-CN",
 *       "runtimeVer": "4.6.0-alpha.7"
 *     }
 *
 *     - region 取值：0 国内、1 海外、2 RND、3 海外RND
 *     - logToConsole 是否输出到控制台：0 不输出、1 输出。
 *     - logLevel 取值：1 Trace、2 Debug、3 Info、4 Warn、5 Error、6 完全不输出
 *     - dataDir 保存本地缓存和日志文件的目录，不允许为空
 *     - ua user agent，不允许为空
 *     - lang 语言，允许为空
 *     - platform 不允许为空，TapSDK专用参数
 *     - deviceId 设备ID，不允许为空，TapSDK专用参数
 *     - sdkArtifact 不允许为空，TapSDK专用参数
 *     - sdkModuleVer 不允许为空，TapSDK专用参数
 *     - runtimeVer 宿主版本，不允许为空，Tap Miniapp专用参数
 *
 * @param callbacks 回调函数。回调函数中禁止执行耗时操作（如网络IO），否则会影响SDK的正常工作。如有耗时操作，请在抛到其他线程执行
 *
 * @return 成功返回0，失败返回-1
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleInitialize(const char* jsonCfg, const struct OnlineBattleCallbacks* callbacks);

/**
 * 程序退出前调用一次，关闭连接、释放资源。非线程安全，并发调用可能导致崩溃。
 */
TAPSDK_EXPORT_API void TapSdkOnlineBattleFinalize();

/**
 * 使用指定的种子seed，创建新的随机数生成器对象，线程安全。
 * 当不再需要使用该随机数生成器时（比如对战结束后），必须调用TapSdkOnlineBattleFreeRandomNumberGenerator()释放资源
 *
 * @param seed 随机数生成器的种子，请使用BattleStartNotification里返回的seed，以保证对战中所有玩家生成一致的随机数
 *
 * @return 随机数生成器对象ID，调用TapSdkOnlineBattleRandomInt()时，需要传入该ID
 */
TAPSDK_EXPORT_API int64_t TapSdkOnlineBattleNewRandomNumberGenerator(int seed);

/**
 * 使用objID指定的随机数生成器对象，生成0 ~ 0x7fffffff之间的随机整数，线程安全
 *
 * @param objID TapSdkOnlineBattleNewRandomNumberGenerator()返回的对象ID
 *
 * @return 0 ~ 0x7fffffff之间的随机整数
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleRandomInt(int64_t objID);

/**
 * 销毁objID指定的随机数生成器对象，释放资源。当不再需要使用该随机数生成器时（比如对战结束后），必须调用此函数释放资源。
 * 对每个objID，只需调用一次此函数，多次调用会导致崩溃
 *
 * @param objID TapSdkOnlineBattleNewRandomNumberGenerator()返回的对象ID
 *
 * @note 胶水层应根据语言特性，实现GC时自动调用此函数释放资源。建议：
 *    - C#使用Finalizer
 *    - Java使用java.lang.ref.Cleaner
 *    - OC使用dealloc
 */
TAPSDK_EXPORT_API void TapSdkOnlineBattleFreeRandomNumberGenerator(int64_t objID);

/**
 * 设置日志等级
 *   - logLevel 日志等级：1 trace、2 debug、3 info、4 warn、5 error、> 5 不打日志。建议调试时设为1，正式版设为3。
 *   - logToConsole 是否输出到控制台：0 不输出、1 输出。
 */
TAPSDK_EXPORT_API void TapSdkOnlineBattleSetLogLevel(int logLevel, int logToConsole);

/**
 * 代码版本，如：1.2.5
 */
TAPSDK_EXPORT_API const char* TapSdkOnlineBattleVersion();

/**
 * git commit 版本，如：98f5d81a0fdcab9a755878b3e825c2cb510e5196
 */
TAPSDK_EXPORT_API const char* TapSdkOnlineBattleGitCommit();

/**
 * 异步和服务端创建连接，完成后通过回调函数ConnectCB通知调用方。登录成功后，才能发起创建房间、匹配房间等请求
 *
 * @param reqID 回调ConnectCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param jsonCfg 登录所需参数，格式请参考ConnectRequest
 *
 * @return
 *   - 0  仅表示参数正确，不代表登录成功，登录请求处理结果通过回调函数ConnectCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleConnect(int64_t reqID, const char* jsonCfg);

/**
 * 异步和服务端断开连接，完成后通过回调函数DisconnectCB通知调用方。断连后，再次执行TapSdkOnlineBattleConnect成功前，无法发起创建房间、匹配房间等请求
 *
 * @param reqID 回调DisconnectCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  仅表示参数正确，不代表登出完成，登出完成通过回调函数DisconnectCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleDisconnect(int64_t reqID, const char* clientID);

/**
 * 小游戏切到后台时调用。切后台后，1分钟内未切回前台，SDK会认为小游戏已退出，自动关闭连接、释放资源，同时回调DisconnectNotificationCB
 *
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  成功
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleOnBackground(const char* clientID);

/**
 * 小游戏切回前台时调用。
 *
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  成功
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleOnForeground(const char* clientID);

/**
 * 异步执行创建房间请求，完成后通过CreateRoomCB回调通知调用方
 *
 * @param reqID 回调CreateRoomCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 创建房间请求，格式请参考CreateRoomRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数CreateRoomCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleCreateRoom(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行匹配房间请求，匹配不到时，根据请求参数自动创建房间，完成后通过MatchRoomCB回调通知调用方
 *
 * @param reqID 回调MatchRoomCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 匹配房间请求，格式请参考MatchRoomRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数MatchRoomCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleMatchRoom(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行获取房间列表请求，完成后通过GetRoomListCB回调通知调用方
 *
 * @param reqID 回调GetRoomListCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 获取房间列表请求，格式请参考GetRoomListRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数GetRoomListCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleGetRoomList(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行加入房间请求，完成后通过JoinRoomCB回调通知调用方
 *
 * @param reqID 回调JoinRoomCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 加入房间请求，格式请参考JoinRoomRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数JoinRoomCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleJoinRoom(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行离开房间请求，完成后通过LeaveRoomCB回调通知调用方。处于对战状态时不允许调用离开房间，会通过LeaveRoomCB返回对应错误
 *
 * @param reqID 回调LeaveRoomCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数LeaveRoomCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleLeaveRoom(int64_t reqID, const char* clientID);

/**
 * 异步执行更新玩家自定义状态请求，完成后通过UpdatePlayerCustomStatusCB回调通知调用方。帧同步中不允许更新。
 * 和UpdatePlayerCustomProperties()、UpdateRoomProperties()、SendCustomMessage()共享每秒15次调用限制
 *
 * @param reqID 回调UpdatePlayerCustomStatusCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param status 玩家自定义状态，具体含义由游戏方定义
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数UpdatePlayerCustomStatusCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleUpdatePlayerCustomStatus(int64_t reqID, const char* clientID, int32_t status);

/**
 * 异步执行更新玩家自定义属性请求，完成后通过UpdatePlayerCustomPropertiesCB回调通知调用方。帧同步中不允许更新。
 * 和UpdatePlayerCustomStatus()、UpdateRoomProperties()、SendCustomMessage()共享每秒15次调用限制
 *
 * @param reqID 回调UpdatePlayerCustomPropertiesCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param properties 玩家自定义属性，必须是utf8字符串，具体格式和含义由游戏方定义，最大2048字节
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数UpdatePlayerCustomPropertiesCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleUpdatePlayerCustomProperties(int64_t reqID, const char* clientID, const char* properties);

/**
 * 异步执行更新房间属性请求，完成后通过UpdateRoomPropertiesCB回调通知调用方。帧同步中不允许更新，仅限房主调用。
 * 和UpdatePlayerCustomStatus()、UpdatePlayerCustomProperties()、SendCustomMessage()共享每秒15次调用限制
 *
 * @param reqID 回调UpdateRoomPropertiesCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 更新房间属性请求，格式请参考UpdateRoomPropertiesRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数UpdateRoomPropertiesCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleUpdateRoomProperties(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行踢玩家出房间请求，完成后通过KickRoomPlayerCB回调通知调用方。帧同步中不允许踢人，仅限房主调用
 *
 * @param reqID 回调KickRoomPlayerCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param playerId 被踢玩家ID
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数KickRoomPlayerCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleKickRoomPlayer(int64_t reqID, const char* clientID, const char* playerId);

/**
 * 异步执行开始帧同步请求，完成后通过StartFrameSyncCB回调通知调用方
 *
 * @param reqID 回调StartFrameSyncCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数StartFrameSyncCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleStartFrameSync(int64_t reqID, const char* clientID);

/**
 * 异步发送玩家游戏操作，完成后通过SendFrameInputCB回调通知调用方。同一帧允许最多发送5次操作，无需等待请求完成的回调，即可发送下一个操作
 *
 * @param reqID 回调SendFrameInputCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 玩家操作请求，格式请参考SendFrameInputRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数SendFrameInputCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleSendFrameInput(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行发送自定义消息请求，完成后通过SendCustomMessageCB回调通知调用方。
 * 和UpdatePlayerCustomStatus()、UpdatePlayerCustomProperties()、UpdateRoomProperties()共享每秒15次调用限制
 *
 * @param reqID 回调SendCustomMessageCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 * @param data 自定义消息，格式请参考SendCustomMessageRequest
 * @param dataLen data的长度（字节）
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数SendCustomMessageCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleSendCustomMessage(int64_t reqID, const char* clientID, const void* data, uint32_t dataLen);

/**
 * 异步执行停止帧同步请求，完成后通过StopFrameSyncCB回调通知调用方
 *
 * @param reqID 回调StopFrameSyncCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
 * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
 *
 * @return
 *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数StopFrameSyncCB返回
 *   - -1 参数错误
 *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
 */
TAPSDK_EXPORT_API int TapSdkOnlineBattleStopFrameSync(int64_t reqID, const char* clientID);

#ifdef __cplusplus
} // extern "C"
#endif
