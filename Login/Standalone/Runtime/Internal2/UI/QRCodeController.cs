using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TapSDK.Login.Internal.Http;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;
using UnityEngine.EventSystems;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using TapSDK.Login.Standalone.Internal;

namespace TapSDK.Login.Internal {
    public class QRCodeController {
        private readonly static string DEFAULT_CLIENT_CN_IOS = "Sprites/tapsdk-login-client-cn-ios";
        private readonly static string DEFAULT_CLIENT_CN_ANDROID = "Sprites/tapsdk-login-client-cn-android";

        private readonly static string DEFAULT_CLIENT_GLOBAL_IOS = "Sprites/tapsdk-login-client-global-ios";
        private readonly static string DEFAULT_CLIENT_GLOBAL_ANDROID = "Sprites/tapsdk-login-client-global-android";

        private readonly static string AUTH_PENDING = "authorization_pending";
        private readonly static string AUTH_WAITING = "authorization_waiting";
        private readonly static string AUTH_DENIED = "access_denied";
        // private readonly static string AUTH_SUCCESS = "";

        private readonly Action<TokenData, String> onAuth;

        private readonly Text titleText;
        private readonly RawImage qrcodeImage;
        private readonly Text tipsText;
        private readonly GameObject scanTips;
        private readonly RawImage demoImage;

        private readonly Button refreshButton;

        private bool isRunning;
        private bool isLoaded;
        private bool isRequestInFlight;
        private bool openBrowserWhenQRReady;
        private uint requestGeneration;

        private string clientId;
        private string[] scopes;

        public QRCodeController(Transform transform, Action<TokenData, String> onAuth) {
            this.onAuth = onAuth;

            titleText = transform.Find("Title").GetComponent<Text>();
            qrcodeImage = transform.Find("QRCode/Image").GetComponent<RawImage>();
            tipsText = transform.Find("Tips").GetComponent<Text>();

            ClientButtonListener iOSButtonListener = transform.Find("Clients/iOSButton").GetComponent<ClientButtonListener>();
            iOSButtonListener.GetComponent<Button>().onClick.AddListener(() => {
                EventSystem.current.SetSelectedGameObject(null);
            });
            iOSButtonListener.OnMouseEnter = OnShowIOSDemoImage;
            iOSButtonListener.OnMouseExit = OnCloseDemoImage;
            ClientButtonListener androidButtonListener = transform.Find("Clients/AndroidButton").GetComponent<ClientButtonListener>();
            androidButtonListener.OnMouseEnter = OnShowAndroidDemoImage;
            androidButtonListener.OnMouseExit = OnCloseDemoImage;
            androidButtonListener.GetComponent<Button>().onClick.AddListener(() => {
                EventSystem.current.SetSelectedGameObject(null);
            });

            scanTips = transform.Find("ScanTips").gameObject;
            demoImage = scanTips.transform.Find("DemoImage").GetComponent<RawImage>();
            scanTips.SetActive(false);

            refreshButton = transform.Find("QRCode/RefreshButton").GetComponent<Button>();
        }

        public void Load(string clientId, string[] scopes) {
            this.clientId = clientId;
            this.scopes = scopes;
            isLoaded = true;

            ILoginLang lang = LoginLanguage.GetCurrentLang();
            titleText.text = lang.QrTitleLogin();
            tipsText.text = $"{lang.QrNoticeUse()} <b>TapTap</b> {lang.QrNoticeClient()} {lang.QrNoticeScanToLogin()}";

            // 加载二维码
            _ = RefreshQRCode();
        }

        public void Unload() {
            isLoaded = false;
            isRunning = false;
            isRequestInFlight = false;
            openBrowserWhenQRReady = false;
            ++requestGeneration;
        }

        internal void StartWebLogin() {
            _ = RefreshQRCode(true);
        }

        private bool IsSessionActive(uint requestId) {
            return isLoaded && isRunning && requestId == requestGeneration;
        }

        private async Task RefreshQRCode(bool openBrowser = false) {
            if (!isLoaded) {
                return;
            }

            // Preserve a web click while the initial QR request or a refresh is pending.
            if (openBrowser && !openBrowserWhenQRReady) {
                openBrowserWhenQRReady = true;
                UI.UIManager.Instance.OpenToast(true,
                    LoginLanguage.GetCurrentLang().WebNoticeLogin(),
                    icon: UI.UIManager.WhiteToastInfoIcon);
            }
            if (isRequestInFlight) {
                return;
            }

            isRequestInFlight = true;
            isRunning = false;
            uint requestId = ++requestGeneration;
            try {
                refreshButton.gameObject.SetActive(false);
                QRCodeData qrcodeData = await LoginService.GetQRCodeUrl(clientId, scopes);
                if (!isLoaded || requestId != requestGeneration) {
                    return;
                }
                if (qrcodeData == null || string.IsNullOrEmpty(qrcodeData.Url) ||
                    string.IsNullOrEmpty(qrcodeData.DeviceCode)) {
                    throw new InvalidOperationException("QR code response is missing its URL or device code");
                }
                qrcodeImage.texture = QRCodeUtils.EncodeQrImage(qrcodeData.Url, 320, 320);

                openBrowser = openBrowserWhenQRReady;
                openBrowserWhenQRReady = false;
                if (openBrowser) {
                    Application.OpenURL(qrcodeData.Url);
                }
                isRequestInFlight = false;
                isRunning = true;
                // 加载二维码完成后再开启监听
                _ = ListenScanQRCode(clientId, qrcodeData, requestId, openBrowser);

                await Task.Delay(qrcodeData.ExpiresIn * 1000);
                if (!IsSessionActive(requestId)) {
                    return;
                }

                // 过期暂停监听
                isRunning = false;
                ShowRefreshQRCode();
            } catch (Exception e) {
                if (!isLoaded || requestId != requestGeneration) {
                    return;
                }
                isRunning = false;
                TapLog.Log("Login, QR code request failed: " + e.Message);
                ShowRefreshQRCode();
                if (openBrowser || openBrowserWhenQRReady) {
                    ILoginLang lang = LoginLanguage.GetCurrentLang();
                    UI.UIManager.Instance.OpenToast(true,
                        $"{lang.WebNoticeFail()}，{lang.WebNoticeFail2()}",
                        icon: UI.UIManager.WhiteToastErrorIcon);
                }
            } finally {
                if (requestId == requestGeneration) {
                    isRequestInFlight = false;
                    openBrowserWhenQRReady = false;
                }
            }
        }

        private async Task ListenScanQRCode(string clientId, QRCodeData qrcodeData,
            uint requestId, bool webLoginSession) {
            while (IsSessionActive(requestId)) {
                try {
                    TokenData tokenData = await LoginService.RequestScanQRCodeResult(clientId, qrcodeData.DeviceCode);
                    if (!IsSessionActive(requestId)) {
                        return;
                    }
                    isRunning = false;
                    TapLog.Log("Login , QRCodeController Success");
                    onAuth.Invoke(tokenData, webLoginSession ?
                        TapLoginTracker.LOGIN_TYPE_BROWSER : TapLoginTracker.LOGIN_TYPE_CODE);
                    return ;
                } catch (TapHttpServerException e) {
                    if (!IsSessionActive(requestId)) {
                        return;
                    }
                    string errorMsg = e.ErrorData?.Error ?? "";
                    ILoginLang lang = LoginLanguage.GetCurrentLang();
                    if (errorMsg == AUTH_PENDING) {

                    } else if (errorMsg == AUTH_WAITING && !webLoginSession) {
                        UI.UIManager.Instance.OpenToast(true,
                            $"{lang.QrnNoticeSuccess()}，{lang.QrnNoticeSuccess2()}",
                            icon: UI.UIManager.WhiteToastSuccessIcon);
                    } else if (errorMsg == AUTH_DENIED) {
                        isRunning = false;
                        UI.UIManager.Instance.OpenToast(true,
                            $"{lang.QrNoticeCancel()}，{lang.QrNoticeCancel2()}",
                            icon: UI.UIManager.WhiteToastErrorIcon);
                        ShowRefreshQRCode();
                        return;
                    }
                    await Task.Delay(qrcodeData.Interval * 1000);
                } catch (Exception e) {
                    if (!IsSessionActive(requestId)) {
                        return;
                    }
                    TapLog.Log("Login, QR code polling failed; retrying: " + e.Message);
                    await Task.Delay(qrcodeData.Interval * 1000);
                }
            }
        }

        private void OnShowIOSDemoImage() {
            string url = DEFAULT_CLIENT_CN_IOS;
            if (TapTapSDK.taptapSdkOptions != null && TapTapSDK.taptapSdkOptions.region == TapTapRegionType.Overseas) {
                url = DEFAULT_CLIENT_GLOBAL_IOS;
            }
            ShowDemoImage(url);
        }

        private void OnShowAndroidDemoImage() {
            string url = DEFAULT_CLIENT_CN_ANDROID;
            if (TapTapSDK.taptapSdkOptions != null && TapTapSDK.taptapSdkOptions.region == TapTapRegionType.Overseas) {
                url = DEFAULT_CLIENT_GLOBAL_ANDROID;
            }
            ShowDemoImage(url);
        }

        private async void ShowDemoImage(string url) {
            demoImage.texture = null;
            scanTips.SetActive(true);
            try {
                demoImage.texture = Resources.Load<Texture2D>(url);
            } catch (Exception) {
                TapLog.Log("Load demo image failed : " + url);
                demoImage.texture = null;
            }
        }

        private void OnCloseDemoImage() {
            scanTips.SetActive(false);
        }

        private void ShowRefreshQRCode() {
            qrcodeImage.texture = null;
            refreshButton.gameObject.SetActive(true);
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => {
                _ = RefreshQRCode();
            });
        }
    }
}
