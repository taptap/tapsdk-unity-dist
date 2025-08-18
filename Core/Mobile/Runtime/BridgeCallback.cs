using System;
using UnityEngine;
using System.Threading;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Core
{

    public class BridgeCallback : AndroidJavaProxy
    {
        Action<Result> callback;

        public BridgeCallback(Action<Result> action) :
            base(new AndroidJavaClass("com.taptap.sdk.kit.internal.enginebridge.EngineBridgeCallback"))
        {
            this.callback = action;
        }

        public override AndroidJavaObject Invoke(string method, object[] args)
        {
            if (method.Equals("onResult"))
            {
                if (args[0] is string)
                {
                    string result = (string)(args[0]);
                    TapLoom.QueueOnMainThread(() =>
                    {
                        callback(new Result(result));
                    });
                }
            }
            return null;
        }
    }
}