namespace TapSDK.OnlineBattle
{
    public class TapOnlineBattleConstant
    {
        public const int ERROR_INVALID_PARAMS = -1; // 无效参数
        public const int ERROR_INIT_INTERNAL_FAILED = -2; // 初始化内部失败或异常
        public const int ERROR_UNKNOWN = 100; // 未知错误
        public const int ERROR_NOT_INIT_OR_LOGIN = 101; // 未初始化或登录
        public const int ERROR_NOT_LAUNCHED_BY_TAP_CLIENT = 200; // 在 Windows 平台未完成启动校验

        public const int ERROR_SYSTEM_ERROR = 1; // 系统错误
        public const int ERROR_SDK_ERROR = 2; // SDK错误，可能内存分配失败、http对象创建失败等

        public const int ERROR_REQUEST_RATE_LIMIT_EXCEEDED = 3; // 请求频率超限
        public const int ERROR_MALICIOUS_USER = 4; // 网关认定为恶意用户，拒绝请求或关闭连接。建议不要重连
        public const int ERROR_TOO_MANY_CONNECTIONS = 5; // 因同时连接数过多而被踢下线。建议不要重连，避免互踢，导致重连死循环
        public const int ERROR_NETWORK_ERROR = 6; // 网络错误，可能是连接超时、断开等
        public const int ERROR_INVALID_REQUEST = 11; // 请求不合法
        public const int ERROR_INVALID_AUTHORIZATION = 12; // 认证信息不合法
        public const int ERROR_UNAUTHORIZED = 13; // 尚未完成登录认证
        public const int ERROR_ALREADY_CONNECTED = 14; // 已经登录，不能重复登录
        public const int ERROR_PREVIOUS_REQUEST_IN_PROGRESS = 15; // 上一个请求未完成，不接受新的请求
        public const int ERROR_UNIMPLEMENTED = 16; // 请求了后端服务未实现的功能
        public const int ERROR_FORBIDDEN = 17; // 用户没有对当前动作的权限，引导重新身份验证并不能提供任何帮助，而且这个请求也不应该被重复提交
        public const int ERROR_ROOM_TEMPLATE_NOT_FOUND = 18; // 房间模板不存在
        public const int ERROR_ROOM_COUNT_LIMIT_EXCEEDED = 19; // 房间总数量超过限制
        public const int ERROR_NOT_IN_ROOM = 20; // 尚未加入房间
        public const int ERROR_ALREADY_IN_ROOM = 21; // 已经在房间中，不能重复加入
        public const int ERROR_NOT_ROOM_OWNER = 22; // 不是房主，不能执行此操作
        public const int ERROR_ROOM_FULL = 23; // 房间已满，不能加入
        public const int ERROR_ROOM_NOT_EXIST = 24; // 房间不存在
        public const int ERROR_FRAME_SYNC_NOT_STARTED = 25; // 对战未开始，不能执行此操作
        public const int ERROR_FRAME_SYNC_ALREADY_STARTED = 26; // 对战已开始，不能执行此操作
        public const int ERROR_FRAME_INPUT_SIZE_LIMIT_EXCEEDED = 28; // 对战帧数据大小超过限制
        public const int ERROR_FRAME_INPUT_COUNT_LIMIT_EXCEEDED = 29; // 每帧可接受的输入数量超过限制
        public const int ERROR_PLAYER_NOT_FOUND = 30; // 玩家不存在
    }
}
