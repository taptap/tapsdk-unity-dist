namespace TapSDK.Core
{
    /// <summary>
    /// SDK 初始化结果回调。可以在 TapTapSDK.Init 之前或之后的任意时机通过
    /// TapTapSDK.AddInitCallback 注册；若注册时状态机已经到达终态，会尽快在主线程异步回调一次（并非同步，调用返回后结果不一定已经可用）。
    /// <para>
    /// 注意：内部注册表对已注册的 callback 持有强引用，不会自动释放；不再需要时请务必调用
    /// TapTapSDK.RemoveInitCallback 手动注销，否则会造成该回调持有的对象无法被回收
    /// （内存泄漏）。
    /// </para>
    /// </summary>
    public interface ITapInitCallback
    {
        /// <summary>
        /// 本次会话 gatekeeper 网络请求成功
        /// </summary>
        void OnInitSuccess();

        /// <summary>
        /// 初始化失败，errorCode 取值见 TapInitErrorCode
        /// </summary>
        void OnInitFail(int errorCode, string errorMsg);
    }
}
