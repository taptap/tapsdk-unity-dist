using System;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal.Log;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TapSDK.OnlineBattle
{
    internal class TapOnlineBattleWrapper
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        internal const string DllName = "onlinebattle_sdk";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        internal const string DllName = "libonlinebattle_sdk";
#elif UNITY_ANDROID
        internal const string DllName = "onlinebattle_sdk";
#elif UNITY_IOS
        internal const string DllName = "__Internal";
#else
        internal const string DllName = "onlinebattle_sdk";
#endif

        // 登录、创建房间、匹配房间等请求的回调
        //   - reqID 原样返回调用方调用登录接口时传入的reqID，用于调用方对应到原始请求
        //   - clientID TapSDK为clientId，Tap Miniapp为miniappId
        //   - success 1表示请求处理成功，此时使用对应的结构体解析返回；0表示处理失败，此时使用Error解析返回
        //   - data 根据success的值，可能是处理成功的返回数据，或者是Error数据
        //   - dataLen data的长度，字节
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RequestCallback(
            long reqID,
            string clientID,
            int success,
            IntPtr data,
            uint dataLen
        );

        // 登出回调
        //   - reqID 原样返回调用 Disconnect()时传入的reqID，用于调用方对应到原始请求。如果为0，则表示本次回调不是因为SignOut()导致，而是因为被踢、连接断开等其他原因
        //   - clientID TapSDK为clientId，Tap Miniapp为miniappId
        //   - data reqID为0时，使用Error解析。判断code的值，如果是网络错误，可以调用SignIn()重连；如果是被踢，不要重连
        //   - dataLen data的长度，字节
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisConnectCallback(long reqID, string clientID);

        // 服务端消息通知回调，对战开始通知、帧数据通知、离开房间通知、被踢下线通知、对战服务异常通知等等
        //   - clientID TapSDK为clientId，Tap Miniapp为miniappId
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NotificationCallback(string clientID, IntPtr data, uint dataLen);

        // 初始化回调结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct OnlineBattleCallbacks
        {
            public RequestCallback ConnectCB; // 登录请求回调，成功时使用SignInResponse解析返回
            public DisConnectCallback DisconnectCB; // 登出请求回调。回调时若reqID不为0，无需解析data；若reqID为0，需要用Error解析data，判断是否要重连
            public RequestCallback CreateRoomCB; // 创建房间请求回调，成功时使用CreateRoomResponse解析返回
            public RequestCallback MatchRoomCB; // 匹配房间请求回调，成功时使用MatchRoomResponse解析返回
            public RequestCallback GetRoomListCB; // 获取房间列表请求回调，成功时使用GetRoomListResponse解析返回
            public RequestCallback JoinRoomCB; // 加入房间请求回调，成功时使用JoinRoomResponse解析返回
            public RequestCallback LeaveRoomCB; // 离开房间请求回调，成功时无需解析返回
            public RequestCallback UpdatePlayerCustomStatusCB; // 更新玩家自定义状态请求回调，成功时无需解析返回
            public RequestCallback UpdatePlayerCustomPropertiesCB; // 更新玩家自定义属性请求回调，成功时无需解析返回
            public RequestCallback UpdateRoomPropertiesCB; // 更新房间属性请求回调，成功时无需解析返回
            public RequestCallback KickRoomPlayerCB; // 踢玩家出房间请求回调，成功时无需解析返回
            public RequestCallback StartFrameSyncCB; // 开始对战请求回调，成功时无需解析返回
            public RequestCallback SendFrameInputCB; // 发送操作数据请求回调，成功时无需解析返回
            public RequestCallback SendCustomMessageCB; // 发送自定义消息请求回调，成功时无需解析返回
            public RequestCallback StopFrameSyncCB; // 对战结束请求回调，成功时无需解析返回

            public NotificationCallback EnterRoomNotificationCB; // 通知房间内所有玩家，有新玩家进入房间，使用EnterRoomNotification解析返回
            public NotificationCallback LeaveRoomNotificationCB; // 通知房间内所有玩家，有玩家离开房间，使用LeaveRoomNotification解析返回
            public NotificationCallback PlayerOfflineNotificationCB; // 只有对战中的玩家离线才会触发，通知房间内所有玩家，使用PlayerOfflineNotification解析返回
            public NotificationCallback PlayerCustomStatusNotificationCB; // 玩家自定义状态变更时触发，通知房间内其他玩家，使用PlayerCustomStatusNotification解析返回
            public NotificationCallback PlayerCustomPropertiesNotificationCB; // 玩家自定义状属性更时触发，通知房间内其他玩家，使用PlayerCustomPropertiesNotification解析返回
            public NotificationCallback RoomPropertiesNotificationCB; // 房间自定义属性更新时触发，通知房间内其他玩家，使用RoomPropertiesNotification解析返回
            public NotificationCallback RoomPlayerKickedNotificationCB; // 玩家被踢出房间通知，使用RoomPlayerKickedNotification解析返回
            public NotificationCallback FrameSyncStartNotificationCB; // 通知房间内所有玩家，对战（帧同步）开始，使用BattleStartNotification解析返回
            public NotificationCallback FrameSyncCB; // 给房间内所有玩家同步最新的对战帧数据，使用BattleFrameSynchronization解析返回
            public NotificationCallback CustomMessageNotificationCB; // 收到自定义消息时回调，使用CustomMessageNotification解析返回
            public NotificationCallback FrameSyncStopNotificationCB; // 通知房间内所有玩家，对战结束，使用BattleStopNotification解析返回
            public NotificationCallback BattleServiceErrorCB; // 对战服务异常时回调，dataLen为0，无需解析data。收到该回调时，端侧需要退出对战、房间、队伍等状态
            public NotificationCallback DisconnectNotificationCB; // 断线、被踢时触发
        };

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int TapSdkOnlineBattleInitialize(
            string jsonCfg,
            ref OnlineBattleCallbacks callbacks
        );

        /// <summary>
        /// 程序退出前调用一次，关闭连接、释放资源。非线程安全，并发调用可能导致崩溃。
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkOnlineBattleFinalize();

        /**
        * 使用指定的种子seed，创建新的随机数生成器对象，线程安全。
        * 当不再需要使用该随机数生成器时（比如对战结束后），必须调用TapSdkOnlineBattleFreeRandomNumberGenerator()释放资源
        *
        * @param seed 随机数生成器的种子，请使用BattleStartNotification里返回的seed，以保证对战中所有玩家生成一致的随机数
        *
        * @return 随机数生成器对象ID，调用TapSdkOnlineBattleRandomInt()时，需要传入该ID
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long TapSdkOnlineBattleNewRandomNumberGenerator(int seed);

        /**
        * 使用objID指定的随机数生成器对象，生成0 ~ 0x7fffffff之间的随机整数，线程安全
        *
        * @param objID TapSdkOnlineBattleNewRandomNumberGenerator()返回的对象ID
        *
        * @return 0 ~ 0x7fffffff之间的随机整数
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleRandomInt(long objID);

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkOnlineBattleFreeRandomNumberGenerator(long objID);

        /**
        * 设置日志等级
        *   - logLevel 日志等级：1 trace、2 debug、3 info、4 warn、5 error、> 5 不打日志。建议调试时设为1，正式版设为3。
        *   - logToConsole 是否输出到控制台：0 不输出、1 输出。
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void TapSdkOnlineBattleSetLogLevel(int logLevel, int logToConsole);

        /**
        * 异步和服务端创建连接，执行登录请求，完成后通过回调函数SignInCB通知调用方。登录成功后，才能发起创建房间、匹配房间等请求
        *
        * @param reqID 回调SignInCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
        * @param jsonCfg 登录所需参数，具体格式请参考SignInRequest
        *
        * @return
        *   - 0  仅表示参数正确，不代表登录成功，登录请求处理结果通过回调函数SignInCB返回
        *   - -1 参数错误
        *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleConnect(long reqID, string jsonCfg);

        /**
        * 异步和服务端关闭连接，完成后通过回调函数SignOutCB通知调用方。登出后，再次执行TapSdkOnlineBattleSignIn成功前，无法发起创建房间、匹配房间等请求
        *
        * @param reqID 回调SignOutCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
        * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
        *
        * @return
        *   - 0  仅表示参数正确，不代表登出完成，登出完成通过回调函数SignOutCB返回
        *   - -1 参数错误
        *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleDisconnect(long reqID, string clientID);

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleCreateRoom(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleMatchRoom(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleGetRoomList(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleJoinRoom(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleLeaveRoom(long reqID, string clientID);

        /**
        * 异步执行更新玩家自定义状态请求，完成后通过UpdatePlayerCustomStatusCB回调通知调用方。在房间里，且未开战时才能调用
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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleUpdatePlayerCustomStatus(
            long reqID,
            string clientID,
            int status
        );

        /**
        * 异步执行更新玩家自定义属性请求，完成后通过UpdatePlayerCustomPropertiesCB回调通知调用方。在房间里，且未开战时才能调用
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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleUpdatePlayerCustomProperties(
            long reqID,
            string clientID,
            string properties
        );

        /**
        * 异步执行更新房间属性请求，完成后通过UpdateRoomPropertiesCB回调通知调用方。在房间里，且未开战时才能调用，仅限房主调用
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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleUpdateRoomProperties(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

        /**
        * 异步执行开始对战请求，完成后通过StartBattleCB回调通知调用方
        *
        * @param reqID 回调StartBattleCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
        * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
        *
        * @return
        *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数StartBattleCB返回
        *   - -1 参数错误
        *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleStartFrameSync(long reqID, string clientID);

        /**
        * 异步执行发送玩家游戏操作，完成后通过SendInputCB回调通知调用方。同一帧允许发送多次操作，无需等待请求完成的回调，即可发送下一个操作
        *
        * @param reqID 回调SendInputCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
        * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
        * @param data 玩家操作请求，格式请参考SendBattleInputRequest
        * @param dataLen data的长度（字节）
        *
        * @return
        *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数SendInputCB返回
        *   - -1 参数错误
        *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleSendFrameInput(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

        /**
        * 异步执行停止对战（帧同步）请求，完成后通过StopBattleCB回调通知调用方
        *
        * @param reqID 回调StopBattleCB时，原样返回调用方传入的reqID，用于调用方对应到原始请求。不允许为0
        * @param clientID TapSDK填ClientID，Tap Miniapp填MiniappID
        *
        * @return
        *   - 0  仅表示参数正确，不代表请求完成，请求完成通过回调函数StopBattleCB返回
        *   - -1 参数错误
        *   - -2 未初始化。尚未调用TapSdkOnlineBattleInitialize()，或者初始化失败，或者已经调用了TapSdkOnlineBattleFinalize()
        */
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleStopFrameSync(long reqID, string clientID);

        /**
        * 异步执行踢玩家出房间请求，完成后通过KickRoomPlayerCB回调通知调用方。在房间里，且未开战时才能调用，仅限房主调用
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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleKickRoomPlayer(
            long reqID,
            string clientID,
            string playerId
        );

        /**
        * 异步执行发送自定义消息请求，完成后通过SendCustomMessageCB回调通知调用方。每秒允许调用20次，无需等待请求完成的回调，即可发送下一条消息
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
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int TapSdkOnlineBattleSendCustomMessage(
            long reqID,
            string clientID,
            IntPtr data,
            uint dataLen
        );

        // 保持回调引用，防止 GC
        private static OnlineBattleCallbacks callbacksRef;

        internal static int Init(string config, OnlineBattleCallbacks callbacks)
        {
            // 编辑器模式下关闭时主动释放 Native 资源
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            callbacksRef = callbacks;
            return TapSdkOnlineBattleInitialize(config, ref callbacksRef);
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                TapLog.Log("Play Mode 即将结束（从 Play 返回 Edit）");
                TapSdkOnlineBattleFinalize();
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TapLog.Log("已经回到 Edit Mode");
            }
        }
#endif
    }
}
