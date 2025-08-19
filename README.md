# tapsdk-unity-dist

TapTap SDK Unity 开发包集合，为 Unity 开发者提供完整的 TapTap 服务集成解决方案。

## GitHub 接入指南（以 Login 模块为例）

### 第一步：添加 SDK 所需的外部依赖

SDK 内部使用了部分第三方库，开发者接入时需先确保 SDK 外部依赖库已正常接入，具体设置如下：

1. SDK 使用的 JSON 解析库为 `Newtonsoft-json`，如果当前工程已接入该依赖库，则不需额外处理，否则需在 `Packages/manifest.json` 添加如下依赖：

```json
"com.unity.nuget.newtonsoft-json":"3.2.1"
```

2. SDK 使用 `com.google.external-dependency-manager` 管理 Android、iOS 依赖，如果当前工程已接入该依赖库，则不需额外处理，否则需在 `Packages/manifest.json` 添加如下依赖：

```json
{
   "dependencies": {
      "com.google.external-dependency-manager": "1.2.179"
   },
   "scopedRegistries": [
      {
         "name": "package.openupm.com",
         "url": "https://package.openupm.com",
         "scopes": [
            "com.google.external-dependency-manager"
         ]
      }
   ]
}
```

### 第二步：GitHub 接入方式

在项目的 `Packages/manifest.json` 文件中添加以下依赖：

```json
"dependencies":{
   "com.taptap.tapsdk.unity.core":"https://github.com/taptap/tapsdk-unity-dist.git?path=Core#4.7.2",
   "com.taptap.tapsdk.unity.login":"https://github.com/taptap/tapsdk-unity-dist.git?path=Login#4.7.2"
}
```

在 Unity 顶部菜单中选择 **Window > Package Manager** 可查看已经安装在项目中的包。

### 第三步：iOS 配置

在 `Assets/Plugins/iOS/Resource` 目录下创建 `TDS-Info.plist` 文件，复制以下代码并且**替换其中的 `ClientId`**：

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>taptap</key>
    <dict>
        <key>client_id</key>
        <string>ClientId</string>
    </dict>
</dict>
</plist>
```

### 第四步：SDK 初始化和使用

```csharp
using TapSDK.Core;
using TapSDK.Login;

// SDK 初始化
TapSDKCore.Init(new TapSDKCoreOptions
{
    clientId = "your_client_id",
    clientToken = "your_client_token",
    serverUrl = "https://your_server_url",
    regionType = RegionType.CN
});

// 登录
try {
    TapTapAccount account = await TapTapLogin.Instance.Login();
    Debug.Log($"登录成功，用户 ID: {account.AccessToken.kid}");
} catch (Exception e) {
    Debug.LogError($"登录失败: {e.Message}");
}
```

### 其他模块接入

其他功能模块的接入方式类似，在 `Packages/manifest.json` 的 `dependencies` 中添加对应的模块路径即可：

```json
"dependencies": {
   "com.taptap.tapsdk.unity.core": "https://github.com/taptap/tapsdk-unity-dist.git?path=Core#4.7.2",
   "com.taptap.tapsdk.unity.login": "https://github.com/taptap/tapsdk-unity-dist.git?path=Login#4.7.2",
   "com.taptap.tapsdk.unity.compliance": "https://github.com/taptap/tapsdk-unity-dist.git?path=Compliance#4.7.2",
   "com.taptap.tapsdk.unity.moment": "https://github.com/taptap/tapsdk-unity-dist.git?path=Moment#4.7.2",
   "com.taptap.tapsdk.unity.achievement": "https://github.com/taptap/tapsdk-unity-dist.git?path=Achievement#4.7.2",
   "com.taptap.tapsdk.unity.license": "https://github.com/taptap/tapsdk-unity-dist.git?path=License#4.7.2",
   "com.taptap.tapsdk.unity.review": "https://github.com/taptap/tapsdk-unity-dist.git?path=Review#4.7.2",
   "com.taptap.tapsdk.unity.relation": "https://github.com/taptap/tapsdk-unity-dist.git?path=Relation#4.7.2",
   "com.taptap.tapsdk.unity.relationlite": "https://github.com/taptap/tapsdk-unity-dist.git?path=RelationLite#4.7.2",
   "com.taptap.tapsdk.unity.leaderboard": "https://github.com/taptap/tapsdk-unity-dist.git?path=Leaderboard#4.7.2",
   "com.taptap.tapsdk.unity.iap": "https://github.com/taptap/tapsdk-unity-dist.git?path=IAP#4.7.2",
   "com.taptap.tapsdk.unity.update": "https://github.com/taptap/tapsdk-unity-dist.git?path=Update#4.7.2"
}
```

> 💡 **提示**：详细文档请参考 [TapTap 登录开发指南](https://developer.taptap.cn/docs/sdk/taptap-login/guide/)





## 相关链接

- [TapTap 开发者文档](https://developer.taptap.cn/docs/)

---

© 2025 TapTap. All Rights Reserved.
