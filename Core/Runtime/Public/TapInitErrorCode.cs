namespace TapSDK.Core
{
    /// <summary>
    /// ITapInitCallback.OnInitFail 使用的跨平台对齐错误码
    /// </summary>
    public static class TapInitErrorCode
    {
        /// <summary>
        /// Init 调用时参数本身非法（如 clientId/clientToken 为空）
        /// </summary>
        public const int ParamError = 1000;

        /// <summary>
        /// gatekeeper 返回 invalid_client：应用信息与 clientId/clientToken 不匹配，不可自动恢复
        /// </summary>
        public const int ConfigError = 1001;

        /// <summary>
        /// gatekeeper 请求多次尝试后仍因网络原因失败，可通过重新调用 Init 恢复
        /// </summary>
        public const int NetworkError = 1002;

        /// <summary>
        /// Init 内部同步初始化步骤（反射创建 IInitTask、各模块 Init、TapTapEvent.Init 等）
        /// 自身抛出异常，不是参数校验失败，接入方不需要去检查 TapTapSdkOptions 参数
        /// </summary>
        public const int InternalError = 1003;
    }
}
