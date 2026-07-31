using System.Runtime.CompilerServices;

// TapInitStateManager 等内部初始化状态机类型需要跨程序集被 Mobile/Standalone 的
// Runtime 调用（它们各自上报 Android/iOS 原生回调或自建 gatekeeper 请求的结果），
// 但不应该作为公开 API 暴露给 SDK 接入方，所以用 InternalsVisibleTo 只开放给这两个
// 程序集，而不是把类型本身声明成 public（Codex 审查发现）。
[assembly: InternalsVisibleTo("TapSDK.Core.Mobile.Runtime")]
[assembly: InternalsVisibleTo("TapSDK.Core.Standalone.Runtime")]
