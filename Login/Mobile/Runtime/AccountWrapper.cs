using System.Collections.Generic;
using JetBrains.Annotations;
using TapSDK.Core;
using TapSDK.Login;
using UnityEngine;

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
                return;
            }
            var dict = Json.Deserialize(json) as Dictionary<string, object>;
            code = SafeDictionary.GetValue<int>(dict, "code");
            message = SafeDictionary.GetValue<string>(dict, "message");
            if (dict.ContainsKey("content") && dict["content"] is Dictionary<string, object> accountDict)
            {
                account = new TapTapAccount(accountDict);
            }
        }
    }
}