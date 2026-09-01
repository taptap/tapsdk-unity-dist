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
        /// 保留的网络错误码。当前 gatekeeper 超时/DNS/TLS/5xx 等会降级为 Success
        ///（沿用磁盘缓存或默认配置），不会通过 OnInitFail 抛出此码。
        /// </summary>
        public const int NetworkError = 1002;

        /// <summary>
        /// Init 内部同步初始化步骤（反射创建 IInitTask、各模块 Init、TapTapEvent.Init 等）
        /// 自身抛出异常，不是参数校验失败，接入方不需要去检查 TapTapSdkOptions 参数
        /// </summary>
        public const int InternalError = 1003;
    }
}
