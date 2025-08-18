using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using TapSDK.Login.Internal.Http;
using System.Collections.Specialized;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Internal.Log;
using TapSDK.Login.Standalone.Internal;

namespace TapSDK.Login.Internal {
    public class WebController {
        private readonly Action<TokenData, String> onAuth;

        private readonly Text titleText;
        private readonly Button jumpButton;
        private readonly Text descriptionText;

        private HttpListener server;

        private string clientId;

        protected string[] scopes;

        private bool isRunning;

        public WebController(Transform transform, Action<TokenData, String> onAuth) {
            this.onAuth = onAuth;

            titleText = transform.Find("Title").GetComponent<Text>();
            jumpButton = transform.Find("JumpButton").GetComponent<Button>();
            descriptionText = transform.Find("Description").GetComponent<Text>();

            jumpButton.onClick.AddListener(OnJumpClicked);
        }

        protected void LoadBasicInfo(string clientId) {
            this.clientId = clientId;

            ILoginLang lang = LoginLanguage.GetCurrentLang();
            titleText.text = lang.WebLogin();
            Text jumpText = jumpButton.transform.Find("Text").GetComponent<Text>();
            jumpText.text = lang.WebButtonJumpToWeb();
            descriptionText.text = lang.WebNotice();
        }

        public void Load(string clientId, string[] scopes) {
            LoadBasicInfo(clientId);
            this.scopes = scopes;
        }

        public void Unload() {
            isRunning = false;
            server?.Stop();
        }

        protected virtual async void OnJumpClicked() {
            ILoginLang lang = LoginLanguage.GetCurrentLang();
            UI.UIManager.Instance.OpenToast(true, 
                lang.WebNoticeLogin(),
                icon: UI.UIManager.WhiteToastInfoIcon);

            WebLoginRequestManager.Instance.CreateNewLoginRequest(scopes);

            string url = WebLoginRequestManager.Instance.GetCurrentRequest().GetWebLoginUrl();
            TapLog.Log("WebController , OpenURL : " + url);
            Application.OpenURL(url);

            // 启动监听
            server = new HttpListener();
            server.Prefixes.Add(WebLoginRequestManager.Instance.GetCurrentRequest().GetRedirectHost());
            server.Start();

            isRunning = true;
            while (isRunning) {
                try {
                    HttpListenerContext context = await server.GetContextAsync();
                    Debug.Log($"{context.Request.HttpMethod} {context.Request.Url}");
                    context.Response.StatusCode = 200;
                    context.Response.Close();

                    if (context.Request.HttpMethod == "OPTIONS") {
                        continue;
                    }

                    // 检测 url 合法性
                    string code = GetRequestCode(context.Request.Url);
                    // 判断授权失败
                    if (string.IsNullOrEmpty(code)) {
                        // 授权失败
                        UI.UIManager.Instance.OpenToast(true, 
                            $"{lang.WebNoticeFail()}，{lang.WebNoticeFail2()}",
                            icon: UI.UIManager.WhiteToastErrorIcon);
                        continue;
                    }

                    TokenData tokenData = await LoginService.Authorize(clientId, code);
                    TapLog.Log("Login , WebController Success");
                    onAuth.Invoke(tokenData, TapLoginTracker.LOGIN_TYPE_BROWSER);

                    return;
                } catch (Exception) {
                    continue;
                }
            }
        }

        private string GetRequestCode(Uri uri) {
            if (uri == null) {
                return null;
            }

            if (!uri.LocalPath.Contains(CodeUtil.GetTapTapOAuthPrefix())) {
                return null;
            }

            NameValueCollection queryPairs = UrlUtils.ParseQueryString(uri.Query);
            string code = queryPairs["code"];
            string state = queryPairs["state"];
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code)) {
                return null;
            }

            return state.Equals(WebLoginRequestManager.Instance.GetCurrentRequest().GetState()) ? 
                code : null;
        }
    }
}
