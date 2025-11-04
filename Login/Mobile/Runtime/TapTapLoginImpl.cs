using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Login.Mobile.Runtime;
using TapSDK.Login.Internal;
using UnityEngine;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Login.Mobile
{
    public class TapTapLoginImpl: ITapTapLoginPlatform
    {
        #if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void RegisterTapTapSDKLoginAppDelegateListener();
        #endif

        private const string SERVICE_NAME = "BridgeLoginService";
        
        public TapTapLoginImpl(){
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.login.unity.BridgeLoginService", 
                "com.taptap.sdk.login.unity.BridgeLoginServiceImpl");
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            
            #if UNITY_IOS
            RegisterTapTapSDKLoginAppDelegateListener();
            #endif  
        }

        public Task<TapTapAccount> Login(string[] scopes)
        {
            var tsc = new TaskCompletionSource<TapTapAccount>();
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loginWithScope")
                .Args("scopes", scopes)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(),
                result =>
                {
                    TapLog.Log("🔍 [Unity Login] Raw callback result received:");
                    TapLog.Log("🔍 [Unity Login] result != null: " + (result != null));
                    if (result != null)
                    {
                        TapLog.Log("🔍 [Unity Login] result.content != null: " + (result.content != null));
                        TapLog.Log("🔍 [Unity Login] result.content type: " + (result.content?.GetType().Name ?? "null"));
                        TapLog.Log("🔍 [Unity Login] result.content length: " + (result.content?.Length ?? 0));
                        TapLog.Log("🔍 [Unity Login] result.content value: '" + (result.content ?? "null") + "'");
                        
                        // 检查是否包含预期的关键字
                        if (!string.IsNullOrEmpty(result.content))
                        {
                            TapLog.Log("🔍 [Unity Login] Contains 'code': " + result.content.Contains("code"));
                            TapLog.Log("🔍 [Unity Login] Contains 'content': " + result.content.Contains("content"));
                            TapLog.Log("🔍 [Unity Login] Contains 'message': " + result.content.Contains("message"));
                            TapLog.Log("🔍 [Unity Login] Contains 'name': " + result.content.Contains("name"));
                            TapLog.Log("🔍 [Unity Login] Contains 'openid': " + result.content.Contains("openid"));
                        }
                    }
                    
                    TapLog.Log("Login result: " + result.content);
                    
                    if (string.IsNullOrEmpty(result.content))
                    {
                        TapLog.Log("❌ [Unity Login] ERROR: result.content is null or empty!");
                        tsc.TrySetException(new Exception("Login result content is null or empty"));
                        return;
                    }
                    
                    try
                    {
                        TapLog.Log("🔧 [Unity Login] Creating AccountWrapper...");
                        var wrapper = new AccountWrapper(result.content);
                        TapLog.Log("✅ [Unity Login] AccountWrapper created successfully");
                        TapLog.Log("🔍 [Unity Login] wrapper.code: " + wrapper.code);
                        TapLog.Log("🔍 [Unity Login] wrapper.message: '" + (wrapper.message ?? "null") + "'");
                        TapLog.Log("🔍 [Unity Login] wrapper.account != null: " + (wrapper.account != null));
                        
                        if (wrapper.account != null)
                        {
                            TapLog.Log("🔍 [Unity Login] Account details:");
                            TapLog.Log("🔍 [Unity Login] - Name: '" + (wrapper.account.name ?? "null") + "'");
                            TapLog.Log("🔍 [Unity Login] - OpenId: '" + (wrapper.account.openId ?? "null") + "'");
                            TapLog.Log("🔍 [Unity Login] - UnionId: '" + (wrapper.account.unionId ?? "null") + "'");
                            TapLog.Log("🔍 [Unity Login] - Email: '" + (wrapper.account.email ?? "null") + "'");
                            TapLog.Log("🔍 [Unity Login] - Avatar: '" + (wrapper.account.avatar ?? "null") + "'");
                        }
                        
                        if (wrapper.code == 1)
                        {
                            TapLog.Log("🚫 [Unity Login] Login was canceled (code=1)");
                            tsc.TrySetCanceled();
                        } else if (wrapper.code == 0)
                        {
                            TapLog.Log("✅ [Unity Login] Login successful (code=0), setting result");
                            tsc.TrySetResult(wrapper.account);
                        }
                        else
                        {
                            TapLog.Log("❌ [Unity Login] Login failed with code: " + wrapper.code + ", message: " + wrapper.message);
                            tsc.TrySetException(new Exception(wrapper.message));
                        }
                    }
                    catch (System.Exception ex)
                    {
                        TapLog.Log("💥 [Unity Login] Exception in AccountWrapper processing: " + ex.Message);
                        TapLog.Log("💥 [Unity Login] Exception stack trace: " + ex.StackTrace);
                        tsc.TrySetException(ex);
                    }
                });
            return tsc.Task;
        }

        public void Logout()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("logout")
                .CommandBuilder());
        }

        public async Task<TapTapAccount> GetCurrentAccount()
        {
            TapLog.Log("🔍 [Unity Login] GetCurrentAccount called");
            
            Result result = await EngineBridge.GetInstance().Emit(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getCurrentTapAccount")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder());
                
            TapLog.Log("🔍 [Unity Login] GetCurrentAccount result received:");
            TapLog.Log("🔍 [Unity Login] result != null: " + (result != null));
            if (result != null)
            {
                TapLog.Log("🔍 [Unity Login] result.content != null: " + (result.content != null));
                TapLog.Log("🔍 [Unity Login] result.content type: " + (result.content?.GetType().Name ?? "null"));
                TapLog.Log("🔍 [Unity Login] result.content length: " + (result.content?.Length ?? 0));
                
                // 显示前200个字符，避免过长的日志
                if (!string.IsNullOrEmpty(result.content))
                {
                    var preview = result.content.Length > 200 ? result.content.Substring(0, 200) + "..." : result.content;
                    TapLog.Log("🔍 [Unity Login] result.content preview: '" + preview + "'");
                }
                else
                {
                    TapLog.Log("🔍 [Unity Login] result.content is null or empty!");
                }
            }
            
            TapLog.Log("Current account: " + result.content);
            
            try
            {
                var wrapper = new AccountWrapper(result.content);
                TapLog.Log("🔍 [Unity Login] AccountWrapper created, account: " + (wrapper.account != null ? "present" : "null"));
                return wrapper.account;
            }
            catch (Exception ex)
            {
                TapLog.Log("💥 [Unity Login] Exception in GetCurrentAccount: " + ex.Message);
                return null;
            }
        }
    }
}