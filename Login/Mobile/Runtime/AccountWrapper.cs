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
            TapLog.Log("🔧 [AccountWrapper] Constructor called");
            TapLog.Log("🔍 [AccountWrapper] Input JSON: '" + (json ?? "null") + "'");
            TapLog.Log("🔍 [AccountWrapper] Input JSON length: " + (json?.Length ?? 0));
            
            if (string.IsNullOrEmpty(json))
            {
                TapLog.Log("❌ [AccountWrapper] JSON is null or empty, returning with defaults");
                return;
            }
            
            try
            {
                TapLog.Log("🔧 [AccountWrapper] Deserializing JSON...");
                var dict = Json.Deserialize(json) as Dictionary<string, object>;
                TapLog.Log("🔍 [AccountWrapper] Deserialization result: " + (dict != null ? "Success" : "Failed"));
                
                if (dict != null)
                {
                    TapLog.Log("🔍 [AccountWrapper] Dictionary keys: [" + string.Join(", ", dict.Keys) + "]");
                    
                    // 提取code
                    if (dict.ContainsKey("code"))
                    {
                        var codeValue = dict["code"];
                        TapLog.Log("🔍 [AccountWrapper] Raw code value: " + codeValue + " (type: " + codeValue?.GetType().Name + ")");
                        code = SafeDictionary.GetValue<int>(dict, "code");
                        TapLog.Log("🔍 [AccountWrapper] Parsed code: " + code);
                    }
                    else
                    {
                        TapLog.Log("⚠️ [AccountWrapper] No 'code' key found in dictionary");
                    }
                    
                    // 提取message
                    if (dict.ContainsKey("message"))
                    {
                        var messageValue = dict["message"];
                        TapLog.Log("🔍 [AccountWrapper] Raw message value: " + messageValue + " (type: " + messageValue?.GetType().Name + ")");
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
                        TapLog.Log("🔍 [AccountWrapper] Raw content value type: " + contentValue?.GetType().Name);
                        TapLog.Log("🔍 [AccountWrapper] Content value: " + contentValue);
                        
                        if (contentValue is Dictionary<string, object> accountDict)
                        {
                            TapLog.Log("✅ [AccountWrapper] Content is Dictionary<string, object>");
                            TapLog.Log("🔍 [AccountWrapper] Account dict keys: [" + string.Join(", ", accountDict.Keys) + "]");
                            
                            try
                            {
                                TapLog.Log("🔧 [AccountWrapper] Creating TapTapAccount...");
                                account = new TapTapAccount(accountDict);
                                TapLog.Log("✅ [AccountWrapper] TapTapAccount created successfully");
                                
                                if (account != null)
                                {
                                    TapLog.Log("🔍 [AccountWrapper] Account created with:");
                                    TapLog.Log("🔍 [AccountWrapper] - Name: '" + (account.name ?? "null") + "'");
                                    TapLog.Log("🔍 [AccountWrapper] - OpenId: '" + (account.openId ?? "null") + "'");
                                    TapLog.Log("🔍 [AccountWrapper] - UnionId: '" + (account.unionId ?? "null") + "'");
                                    TapLog.Log("🔍 [AccountWrapper] - Email: '" + (account.email ?? "null") + "'");
                                    TapLog.Log("🔍 [AccountWrapper] - Avatar: '" + (account.avatar ?? "null") + "'");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                TapLog.Log("💥 [AccountWrapper] Exception creating TapTapAccount: " + ex.Message);
                                TapLog.Log("💥 [AccountWrapper] Stack trace: " + ex.StackTrace);
                            }
                        }
                        else
                        {
                            TapLog.Log("❌ [AccountWrapper] Content is not Dictionary<string, object>");
                            TapLog.Log("🔍 [AccountWrapper] Content actual type: " + (contentValue?.GetType().FullName ?? "null"));
                            
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
                TapLog.Log("💥 [AccountWrapper] Exception in constructor: " + ex.Message);
                TapLog.Log("💥 [AccountWrapper] Stack trace: " + ex.StackTrace);
            }
            
            TapLog.Log("✅ [AccountWrapper] Constructor completed - code: " + code + ", account: " + (account != null ? "present" : "null"));
        }
    }
}