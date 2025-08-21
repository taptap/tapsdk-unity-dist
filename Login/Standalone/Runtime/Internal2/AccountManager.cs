using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Newtonsoft.Json;
using TapSDK.Core;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.Login.Internal
{
    public class AccountManager
    {
        private static readonly string _accessToken = "taptapsdk_accesstoken";
        private static readonly string _profile = "taptapsdk_profile";
        private static readonly string _account = "taptapsdk_account";

        private static AccountManager _instance;

        private AccountManager()
        {
        }

        public static AccountManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AccountManager();
                    // 自动初始化
                    _instance.Init();
                }
                return _instance;
            }
        }

        private TapTapAccount _tapAccount;
        [CanBeNull]
        public TapTapAccount Account
        {
            get
            {
                return _tapAccount;
            }
            set
            {
                _tapAccount = value;
                TapOpenlogStandalone.openid = value?.openId ?? "";
                if (value == null)
                {
                    DataStorage.SaveString(_account, null);
                    TapAppDurationStandalone.OnLogout();
                }
                else
                {
                    DataStorage.SaveString(_account, value.ToJson());
                    TapAppDurationStandalone.OnLogin(value?.openId);
                }
            }
        }

        public void Init()
        {
            var accountStr = DataStorage.LoadString(_account);
            if (!string.IsNullOrEmpty(accountStr))
            {
                try
                {
                    Account = new TapTapAccount(Json.Deserialize(accountStr) as Dictionary<string, object>);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("TapSDK Login find account cache but parse failed" + e.Message);
                    DataStorage.RemoveCacheKey(_account);
                }
            }
            else
            {
                try
                {
                    var accessTokenStr = DataStorage.LoadString(_accessToken);
                    if (string.IsNullOrEmpty(accessTokenStr))
                    {
                        return;
                    }

                    var profileStr = DataStorage.LoadString(_profile);
                    if (string.IsNullOrEmpty(profileStr))
                    {
                        return;
                    }
                    var accessToken = JsonConvert.DeserializeObject<AccessToken>(accessTokenStr);
                    var profile = JsonConvert.DeserializeObject<Profile>(profileStr);
                    Account = new TapTapAccount(accessToken, profile.openid, profile.unionid, profile.name, profile.avatar, profile.email);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("TapSDK Login find old account cache but parse failed" + e.Message);
                    DataStorage.RemoveCacheKey(_accessToken);
                    DataStorage.RemoveCacheKey(_profile);
                }
            }
        }

        public void ClearCache()
        {
            Account = null;
        }
    }
}