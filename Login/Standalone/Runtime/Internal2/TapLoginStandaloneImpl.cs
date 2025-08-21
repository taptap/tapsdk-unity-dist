using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone;
using TapSDK.Core.Standalone.Internal.Http;
using TapSDK.Login.Internal.Http;
using TapSDK.Login.Standalone;
using TapSDK.Login.Standalone.Internal;
using UnityEngine;

namespace TapSDK.Login.Internal
{
    public class TapLoginStandaloneImpl
    {
        private static TapLoginStandaloneImpl instance;

        // 当前是否正在登录中
        private volatile bool IsLogging = false;

        // 本地缓存的用户信息是否和 Tap 启动器一致
        internal static bool isCacheUserSameWithTapClient = true;

        private TapLoginStandaloneImpl()
        {

        }

        public static TapLoginStandaloneImpl Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapLoginStandaloneImpl();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            TapTapSdk.SDKInitialize(clientId, regionType == TapTapRegionType.CN);
            AccountManager.Instance.Init();

            TapLogger.Debug("RegisterListenerForTapClientCheck ");
            RegisterListenerForTapClientCheck();

            _ = CheckAndRefreshToken();
            TapLoginTracker.Instance.TrackInit();
        }

        public Task<TapTapAccount> Login(string[] scopes)
        {
            // 正在登录时，返回登录异常
            if (IsLogging)
            {
                var defaultTcs = new TaskCompletionSource<TapTapAccount>();
                defaultTcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_LOGOUT_INVALID_LOGIN_STATE, "Currently logging in"));
                return defaultTcs.Task;
            }
            IsLogging = true;
            string sessionId = Guid.NewGuid().ToString();
            

            if (!scopes.Contains(TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE))
            {
                scopes = scopes.Append(TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE).ToArray();
            }
            IComplianceProvider provider = BridgeUtils.CreateBridgeImplementation(typeof(IComplianceProvider),
                "TapSDK.Compliance") as IComplianceProvider;
            string complianceScope = provider?.GetAgeRangeScope(TapTapSdk.CurrentRegion is RegionCN _);
            if (complianceScope != null)
            {
                scopes = scopes.Append(complianceScope).ToArray();
            }
            
#if UNITY_STANDALONE_WIN
            // 是否使用 Tap 启动器登录
            bool isNeedLoginByClient = TapClientStandalone.IsNeedLoginByTapClient();
            if (isNeedLoginByClient)
            {
                async Task<TapTapAccount> innerLogin()
                {
                    try
                    {
                        TapLoginTracker.Instance.TrackStart("loginWithScopes", sessionId, TapLoginTracker.LOGIN_TYPE_CLIENT);
                        TapTapAccount account = await AuthorizeInternalWithTapClient<TapTapAccount>(scopes, true);
                        IsLogging = false;
                        TapLoginTracker.Instance.TrackSuccess("loginWithScopes", sessionId, TapLoginTracker.LOGIN_TYPE_CLIENT);
                        return account;
                    }
                    catch (TaskCanceledException e)
                    {
                        IsLogging = false;
                        TapLoginTracker.Instance.TrackCancel("loginWithScopes", sessionId, TapLoginTracker.LOGIN_TYPE_CLIENT);
                        throw e;
                    }
                    catch (Exception e)
                    {
                        IsLogging = false;
                        TapLoginTracker.Instance.TrackFailure("loginWithScopes", sessionId, TapLoginTracker.LOGIN_TYPE_CLIENT, (int)TapErrorCode.ERROR_CODE_UNDEFINED, e.Message ?? "未知错误");
                        throw e;
                    }
                }
                return innerLogin();
            }
#endif
            /// 非启动器，走扫码或网页流程
            TapLoginTracker.Instance.TrackStart("loginWithScopes", sessionId);
            TaskCompletionSource<TapTapAccount> tcs = new TaskCompletionSource<TapTapAccount>();
            LoginPanelController.OpenParams openParams = new LoginPanelController.OpenParams
            {
                ClientId = TapTapSdk.ClientId,
                Scopes = scopes,
                OnAuth = async (tokenData, loginType) =>
                {
                    if (tokenData == null)
                    {
                        TapLoginTracker.Instance.TrackFailure("loginWithScopes", sessionId, loginType, (int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error");
                        IsLogging = false;
                        tcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error"));
                    }
                    else
                    {
                        // 将 TokenData 转化为 AccessToken
                        AccessToken refreshToken = new AccessToken
                        {
                            kid = tokenData.Kid,
                            tokenType = tokenData.TokenType,
                            macKey = tokenData.MacKey,
                            macAlgorithm = tokenData.MacAlgorithm,
                            scopeSet = tokenData.Scopes
                        };
                        try
                        {
                            ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, refreshToken);
                            if (profileData != null)
                            {
                                TapLoginTracker.Instance.TrackSuccess("loginWithScopes", sessionId, loginType);
                                AccountManager.Instance.Account = new TapTapAccount(
                                    refreshToken, profileData.OpenId, profileData.UnionId, profileData.Name, profileData.Avatar,
                                    profileData.Email);
                                IsLogging = false;
                                tcs.TrySetResult(AccountManager.Instance.Account);
                            }
                            else
                            {
                                TapLoginTracker.Instance.TrackFailure("loginWithScopes", sessionId, loginType, (int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error");
                                IsLogging = false;
                                tcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error"));
                            }
                        }
                        catch (Exception e)
                        {
                            TapLoginTracker.Instance.TrackFailure("loginWithScopes", sessionId, loginType, (int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error");
                            IsLogging = false;
                            tcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error " + e.Message));
                        }
                    }
                },
                OnError = (e, loginType) =>
                {
                    TapLoginTracker.Instance.TrackFailure("loginWithScopes", sessionId, loginType, e.Code, e.Message);
                    IsLogging = false;
                    tcs.TrySetException(e);
                },
                OnClose = () =>
                {
                    TapLoginTracker.Instance.TrackCancel("loginWithScopes", sessionId);
                    IsLogging = false;
                    tcs.TrySetCanceled();
                }
            };
            TapSDK.UI.UIManager.Instance.OpenUI<LoginPanelController>("Prefabs/TapLogin/LoginPanel", openParams);
            return tcs.Task;
            
        }
        
#if UNITY_STANDALONE_WIN
        private async Task<T> AuthorizeInternalWithTapClient<T>(string[] scopes = null, bool needProfile = true)
        {
            string info = "{\"device_id\":\"" + SystemInfo.deviceModel + "\"}";
            string sdkUA = "client_id=" + TapTapSdk.ClientId + "&uuid=" + SystemInfo.deviceUniqueIdentifier;
            TapLogger.Debug("LoginWithScopes start in thread = " + Thread.CurrentThread.ManagedThreadId);
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

            string responseType = "code";
            string redirectUri = "tapoauth://authorize";
            string state = Guid.NewGuid().ToString("N");
            string codeVerifier = CodeUtil.GenerateCodeVerifier();
            string codeChallenge = CodeUtil.GetCodeChallenge(codeVerifier);
            string versionCode = TapTapSDK.Version;
            string codeChallengeMethod = "S256";
            TapLoginClientBridge.TapLoginResponseByTapClient response = await TapLoginClientBridge.LoginWithScopesAsync(scopes,
            responseType, redirectUri, codeChallenge, state, codeChallengeMethod, versionCode, sdkUA, info);
            TapLogger.Debug("start handle login result");
            TapLogger.Debug("LoginWithScopes handle in thread = " + Thread.CurrentThread.ManagedThreadId);

            if (response.isCancel)
            {
                taskCompletionSource.TrySetException(new TaskCanceledException());
            }
            else if (response.isFail || string.IsNullOrEmpty(response.redirectUri))
            {
                taskCompletionSource.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, response.errorMsg ?? "未知错误"));
            }
            else
            {
                TapLogger.Debug("login success prepare get token");
                try
                {
                    Uri uri = new Uri(response.redirectUri);
                    NameValueCollection queryPairs = UrlUtils.ParseQueryString(uri.Query);
                    string code = queryPairs["code"];
                    string uriState = queryPairs["state"];
                    string error = queryPairs["error"];
                    if (string.IsNullOrEmpty(error) && uriState == state && !string.IsNullOrEmpty(code))
                    {
                        TokenData tokenData = await LoginService.Authorize(TapTapSdk.ClientId, code, codeVerifier);
                        if (tokenData == null)
                        {
                            throw new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "get token data failed");
                        }
                        else
                        {
                            // 将 TokenData 转化为 AccessToken
                            AccessToken refreshToken = new AccessToken
                            {
                                kid = tokenData.Kid,
                                tokenType = tokenData.TokenType,
                                macKey = tokenData.MacKey,
                                macAlgorithm = tokenData.MacAlgorithm,
                                scopeSet = tokenData.Scopes
                            };
                            if (!needProfile)
                            {
                                taskCompletionSource.TrySetResult((T)(object)refreshToken);
                            }
                            else
                            {
                                ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, refreshToken);
                                if (profileData != null)
                                {
                                    AccountManager.Instance.Account = new TapTapAccount(
                                        refreshToken, profileData.OpenId, profileData.UnionId, profileData.Name, profileData.Avatar,
                                        profileData.Email);
                                    taskCompletionSource.TrySetResult((T)(object)AccountManager.Instance.Account);
                                }
                                else
                                {
                                    throw new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "fetch profile data failed");
                                }
                            }
                        }
                    }
                    else
                    {
                        TapLogger.Debug("login success prepare get token but get  error " + error);
                        throw new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, error ?? "数据解析异常");
                    }
                }
                catch (Exception ex)
                {
                    TapLogger.Debug("login success prepare get token  fail " + ex.Message);
                    taskCompletionSource.TrySetException(ex);
                }
            }
            return await taskCompletionSource.Task;
        }
#endif

        public Task<AccessToken> Authorize(string[] scopes = null)
        {

#if UNITY_STANDALONE_WIN
            // 是否使用 Tap 启动器登录
            bool isNeedLoginByClient = TapClientStandalone.IsNeedLoginByTapClient();

            if (isNeedLoginByClient)
            {
                async Task<AccessToken> innerLogin()
                {
                    AccessToken token = await AuthorizeInternalWithTapClient<AccessToken>(scopes, false);
                    return token;
                }
                return innerLogin();
            }
#endif

            TaskCompletionSource<AccessToken> tcs = new TaskCompletionSource<AccessToken>();
            LoginPanelController.OpenParams openParams = new LoginPanelController.OpenParams
            {
                ClientId = TapTapSdk.ClientId,
                Scopes = new HashSet<string>(scopes).ToArray(),
                OnAuth = (tokenData, loginType) =>
                {
                    if (tokenData == null)
                    {
                        tcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error"));
                    }
                    else
                    {
                        // 将 TokenData 转化为 AccessToken
                        AccessToken accessToken = new AccessToken
                        {
                            kid = tokenData.Kid,
                            tokenType = tokenData.TokenType,
                            macKey = tokenData.MacKey,
                            macAlgorithm = tokenData.MacAlgorithm,
                            scopeSet = tokenData.Scopes
                        };
                        tcs.TrySetResult(accessToken);
                    }
                },
                OnError = (e, loginType) =>
                {
                    tcs.TrySetException(e);
                },
                OnClose = () =>
                {
                    tcs.TrySetException(
                        new TapException((int)TapErrorCode.ERROR_CODE_LOGIN_CANCEL, "Login Cancel"));
                }
            };
            TapSDK.UI.UIManager.Instance.OpenUI<LoginPanelController>("Prefabs/TapLogin/LoginPanel", openParams);
            return tcs.Task;
            
        }

        public void Logout()
        {
            AccountManager.Instance.ClearCache();
        }

        public Task<TapTapAccount> GetCurrentAccount()
        {
            var tcs = new TaskCompletionSource<TapTapAccount>();
            tcs.TrySetResult(AccountManager.Instance.Account);
            return tcs.Task;
        }

        /// <summary>
        /// 注册启动器检查完成事件，内部处理本地缓存与启动器用户信息的一致性问题
        /// </summary>
        private void RegisterListenerForTapClientCheck()
        {
            EventManager.AddListener(EventManager.IsLaunchedFromTapTapPCFinished, (openId) =>
            {
                TapLogger.Debug("receive IsLaunchedFromTapTapPCFinished event");
                if (openId is string userId && !string.IsNullOrEmpty(userId))
                {
                    CheckLoginStateWithTapClient(userId);
                }
            });
        }

        /// <summary>
        /// 校验缓存中的用户信息是否与 Tap 启动器中的一致, 不一致时清空本地缓存
        /// 本地无用户信息时，默认为与启动器一致
        /// </summary>
        /// <param name="openId"></param>
        private async void CheckLoginStateWithTapClient(string openId)
        {
            TapTapAccount account = await GetCurrentAccount();
            if (account != null && account.openId != null)
            {
                if (account.openId != openId)
                {
                    isCacheUserSameWithTapClient = false;
                    TapLogger.Debug("receive IsLaunchedFromTapTapPCFinished event and not same");
                    Logout();
                }
                else
                {
                    isCacheUserSameWithTapClient = true;
                }
            }
            else
            {
                isCacheUserSameWithTapClient = true;
            }
        }
        private async Task CheckAndRefreshToken()
        {
            try
            {
                AccessToken accessToken = AccountManager.Instance.Account?.accessToken;
                if (accessToken != null)
                {
                    TokenData tokenData = null;
                    try
                    {
                        tokenData = await LoginService.RefreshToken(TapTapSdk.ClientId, accessToken.kid);
                    }
                    catch (TapHttpServerException e)
                    {
                        //清除本地缓存
                        if (e.ErrorData.Code < 0)
                        {
                            Logout();
                        }
                        return;
                    }

                    if (tokenData == null)
                    {
                        return;
                    }
                    AccessToken refreshToken = new AccessToken
                    {
                        kid = tokenData.Kid,
                        tokenType = tokenData.TokenType,
                        macKey = tokenData.MacKey,
                        macAlgorithm = tokenData.MacAlgorithm,
                        scopeSet = tokenData.Scopes
                    };
                    ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, refreshToken);
                    if (profileData != null && isCacheUserSameWithTapClient)
                    {
                        AccountManager.Instance.Account = new TapTapAccount(
                            refreshToken, profileData.OpenId, profileData.UnionId, profileData.Name, profileData.Avatar,
                            profileData.Email);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("refresh TapToken fail reason : " + e.Message + "\n stack = " + e.StackTrace);
            }
        }
    }
}