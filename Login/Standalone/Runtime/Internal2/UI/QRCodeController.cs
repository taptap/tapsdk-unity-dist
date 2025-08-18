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
        private QRCodeData qrcodeData;

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

            ILoginLang lang = LoginLanguage.GetCurrentLang();
            titleText.text = lang.QrTitleLogin();
            tipsText.text = $"{lang.QrNoticeUse()} <b>TapTap</b> {lang.QrNoticeClient()}{lang.QrNoticeScanToLogin()}";

            // 加载二维码
            _ = RefreshQRCode(clientId, scopes);
        }

        public void Unload() {
            isRunning = false;
        }

        private async Task RefreshQRCode(string clientId, string[] scopes) {
            try {
                refreshButton.gameObject.SetActive(false);
                qrcodeData = await LoginService.GetQRCodeUrl(clientId, scopes);
                qrcodeImage.texture = QRCodeUtils.EncodeQrImage(qrcodeData.Url, 320, 320);

                // 加载二维码完成后再开启监听
                _ = ListenScanQRCode(clientId);

                await Task.Delay(qrcodeData.ExpiresIn * 1000);

                // 过期暂停监听
                isRunning = false;
                ShowRefreshQRCode();
            } catch (Exception) {
                // 加载失败
                ShowRefreshQRCode();
                return;
            }
        }

        private async Task ListenScanQRCode(string clientId) {
            isRunning = true;
            while (isRunning) {
                if (qrcodeData == null) {
                    await Task.Delay(3 * 1000);
                } else {
                    try {
                        TokenData tokenData = await LoginService.RequestScanQRCodeResult(clientId, qrcodeData.DeviceCode);
                        TapLog.Log("Login , QRCodeController Success");
                        onAuth.Invoke(tokenData, TapLoginTracker.LOGIN_TYPE_CODE);
                        return ;
                    } catch (TapHttpServerException e) {
                        string errorMsg = e.ErrorData?.Error ?? "";
                        ILoginLang lang = LoginLanguage.GetCurrentLang();
                        if (errorMsg == AUTH_PENDING) {

                        } else if (errorMsg == AUTH_WAITING) {
                            UI.UIManager.Instance.OpenToast(true, 
                                $"{lang.QrnNoticeSuccess()}，{lang.QrnNoticeSuccess2()}",
                                icon: UI.UIManager.WhiteToastSuccessIcon);
                        } else if (errorMsg == AUTH_DENIED) {
                            UI.UIManager.Instance.OpenToast(true, 
                                $"{lang.QrNoticeCancel()}，{lang.QrNoticeCancel2()}",
                                icon: UI.UIManager.WhiteToastErrorIcon);
                            ShowRefreshQRCode();
                        }
                        await Task.Delay(qrcodeData.Interval * 1000);
                    } catch (Exception) {
                        await Task.Delay(qrcodeData.Interval * 1000);
                    }
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
                _ = RefreshQRCode(clientId, scopes);
            });
        }
    }
}
