using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Login.Mobile.Runtime;
using TapSDK.Login.Internal;
using UnityEngine;
using System.Runtime.InteropServices;

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
                    Debug.Log("Login result: " + result.content);
                    var wrapper = new AccountWrapper(result.content);
                    if (wrapper.code == 1)
                    {
                        tsc.TrySetCanceled();
                    } else if (wrapper.code == 0)
                    {
                        tsc.TrySetResult(wrapper.account);
                    }
                    else
                    {
                        tsc.TrySetException(new Exception(wrapper.message));
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
            Result result = await EngineBridge.GetInstance().Emit(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getCurrentTapAccount")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder());
            Debug.Log("Current account: " + result.content);
            return new AccountWrapper(result.content).account;
        }
    }
}