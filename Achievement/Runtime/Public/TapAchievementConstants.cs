namespace TapSDK.Achievement
{
    public class TapTapAchievementConstants
    {
        // 未初始化
        public static readonly int NOT_INITIALIZED = 80000;
        // 区域不支持
        public static readonly int REGION_NOT_SUPPORTED = 80001;
        // 当前未登录，需要登录
        public static readonly int NOT_LOGGED = 80002;
        // 当前登录失效，需要重新登录
        public static readonly int ACCESS_DENIED = 80010;
        // 无效参数
        public static readonly int INVALID_REQUEST = 80020;
        // 网络异常
        public static readonly int NETWORK_ERROR = 80030;
        // 未知错误，如：代理导致网络错误
        public static readonly int UNKNOWN_ERROR = 80100;

        // unlock解锁成就成功
        public static readonly int UNLOCK_SUCCESS = 70001;
        // 增加步长成功
        public static readonly int INCREMENT_SUCCESS = 70002;
    }
}