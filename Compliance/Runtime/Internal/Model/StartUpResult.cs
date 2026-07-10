
namespace TapSDK.Compliance.Model 
{
    public static class StartUpResult
    {
        public const int LOGIN_SUCCESS      = 500;    // 登录成功-ok
        public const int EXITED             = 1000;   // 用户登出
        public const int SWITCH_ACCOUNT     = 1001;   // 切换账号
        public const int PERIOD_RESTRICT    = 1030;   // 当前用户达到宵禁时长
        public const int DURATION_LIMIT     = 1050;   // 时长限制

        public const int AGE_LIMIT     = 1100;   // 适龄限制
        public const int INVALID_CLIENT_OR_NETWORK_ERROR     = 1200;   // 应用配置错误或网络异常
        public const int REAL_NAME_STOP     = 9002;   // 实名过程中点击了关闭实名窗

        public static bool Contains(int val) {
            if (val == LOGIN_SUCCESS) return true;
            if (val == EXITED) return true;
            if (val == SWITCH_ACCOUNT) return true;
            if (val == PERIOD_RESTRICT) return true;
            if (val == DURATION_LIMIT) return true;
            if (val == REAL_NAME_STOP) return true;
            if (val == AGE_LIMIT) return true;
            if (val == INVALID_CLIENT_OR_NETWORK_ERROR) return true;
            return false;
        }
    }
}
