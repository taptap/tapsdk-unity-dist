using System;
using UnityEngine;

namespace TapSDK.Core
{
    public class BridgeAndroid : IBridge
    {
        private string bridgeJavaClz = "com.taptap.sdk.kit.internal.enginebridge.EngineBridge";

        private string instanceField = "INSTANCE";

        private string registerHandlerMethod = "registerHandler";

        private string callHandlerMethod = "execCommand";

        private string callHandlerAsyncMethod = "execCommandAsync";

        private string initMethod = "init";

        private string registerMethod = "registerService";

        private readonly AndroidJavaObject _mAndroidBridge;

        private static readonly BridgeAndroid SInstance = new BridgeAndroid();

        public static BridgeAndroid GetInstance()
        {
            return SInstance;
        }

        private BridgeAndroid()
        {
            var mCurrentActivity =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            _mAndroidBridge = new AndroidJavaClass(bridgeJavaClz).GetStatic<AndroidJavaObject>(instanceField);
            _mAndroidBridge.Call(initMethod, mCurrentActivity);
        }

        public void Register(string serviceClzName, string serviceImplName)
        {
            if (_mAndroidBridge == null)
            {
                return;
            }

            try
            {
                var serviceClass = new AndroidJavaClass(serviceClzName);
                var serviceImpl = new AndroidJavaObject(serviceImplName);
                _mAndroidBridge.Call(registerMethod, serviceClass, serviceImpl);
            }
            catch (Exception e)
            {
                Debug.Log("register Failed:" + e);
                //
            }
        }

        public void Call(Command command, Action<Result> action)
        {
            _mAndroidBridge?.Call(callHandlerMethod, command.ToJSON(), new BridgeCallback(action));
        }

        public void Call(Command command)
        {
            _mAndroidBridge?.Call(callHandlerMethod, command.ToJSON(), null);
        }
        public string CallWithReturnValue(Command command, Action<Result> action)
        {
            return _mAndroidBridge?.Call<string>(callHandlerAsyncMethod, command.ToJSON());
        }
    }
}