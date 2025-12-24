using System.Collections.Generic;
using JetBrains.Annotations;
using TapSDK.Core;
using TapSDK.Login;
using UnityEngine;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Login.Mobile.Runtime
{
    public class AccountWrapper
    {
        public int code { get; }

        [CanBeNull] public string message { get; }

        [CanBeNull] public TapTapAccount account { get; }

        public AccountWrapper(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                TapLog.Log("❌ [AccountWrapper] JSON is null or empty, returning with defaults");
                return;
            }
            try
            {
                var dict = Json.Deserialize(json) as Dictionary<string, object>;
                if (dict != null)
                {
                    // 提取code
                    if (dict.ContainsKey("code"))
                    {
                        var codeValue = dict["code"];
                        code = SafeDictionary.GetValue<int>(dict, "code");
                    }
                    else
                    {
                        TapLog.Log("⚠️ [AccountWrapper] No 'code' key found in dictionary");
                    }

                    // 提取message
                    if (dict.ContainsKey("message"))
                    {
                        var messageValue = dict["message"];
                        message = SafeDictionary.GetValue<string>(dict, "message");
                        TapLog.Log("🔍 [AccountWrapper] Parsed message: '" + (message ?? "null") + "'");
                    }
                    else
                    {
                        TapLog.Log("⚠️ [AccountWrapper] No 'message' key found in dictionary");
                    }
                    // 提取content (account data)
                    if (dict.ContainsKey("content"))
                    {
                        var contentValue = dict["content"];
                        if (contentValue is Dictionary<string, object> accountDict)
                        {
                            try
                            {
                                TapLog.Log("🔧 [AccountWrapper] Creating TapTapAccount...");
                                account = new TapTapAccount(accountDict);
                            }
                            catch (System.Exception ex)
                            {
                                TapLog.Log("💥 [AccountWrapper] Stack trace: " + ex.StackTrace);
                            }
                        }
                        else
                        {
                            // 尝试其他可能的类型
                            if (contentValue is string contentStr)
                            {
                                TapLog.Log("🔍 [AccountWrapper] Content is string: '" + contentStr + "'");
                            }
                            else if (contentValue is Dictionary<object, object> objDict)
                            {
                                TapLog.Log("🔍 [AccountWrapper] Content is Dictionary<object, object> with " + objDict.Count + " items");
                            }
                        }
                    }
                    else
                    {
                        TapLog.Log("⚠️ [AccountWrapper] No 'content' key found in dictionary");
                    }
                }
                else
                {
                    TapLog.Log("❌ [AccountWrapper] Failed to deserialize JSON to dictionary");
                }
            }
            catch (System.Exception ex)
            {
                TapLog.Log("💥 [AccountWrapper] Stack trace: " + ex.StackTrace);
            }
        }
    }
}