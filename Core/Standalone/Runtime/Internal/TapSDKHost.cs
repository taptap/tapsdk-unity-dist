namespace TapSDK.Core.Standalone.Internal
{
    /// <summary>
    /// 全 SDK 共享的通用 API 域名解析，供各 Standalone 模块统一调用，避免各自重复写一份
    /// 地区(CN/Overseas) 的域名选择逻辑。直接读 TapCoreStandalone.coreOptions.region，
    /// 调用方不需要也不应该自己传参。
    /// </summary>
    internal static class TapSDKHost
    {
        // 不能是 readonly：内网(RND)测试包要在 Init 前反射改写这两个字段，把全 SDK 的通用
        // API 域名切到 xdrnd。历史上这个覆写点是 TapHttp.HOST_CN，域名入口统一到本类之后
        // 那里只剩一份读不到的静态拷贝，覆写静默失效（成就等模块仍打生产域名、拿内网 token
        // 鉴权 → invalid self-contained access token）。改写这里才是唯一生效的入口。
        internal static string HOST_CN = "https://tapsdk.tapapis.cn";
        internal static string HOST_IO = "https://tapsdk.tapapis.com";

        internal static string GetApiHost()
        {
            // coreOptions 在 Init 之前是 null。绝大多数调用方都在 Init 之后，但连通性探测
            // 这类入口不保证，之前它们读的是常量 HOST_CN 不会出事，改成走这里就得防一手，
            // 否则会把一个 NRE 引进原本不会失败的路径。未初始化时按 CN 兜底，与改动前
            // 读 HOST_CN 的行为一致。
            TapTapSdkOptions options = TapCoreStandalone.coreOptions;
            if (options != null && options.region == TapTapRegionType.Overseas)
            {
                return HOST_IO;
            }
            return HOST_CN;
        }
    }
}
