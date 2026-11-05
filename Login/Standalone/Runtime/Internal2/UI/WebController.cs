using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapSDK.Login.Internal {
    public class WebController {
        private readonly Action onWebLogin;

        private readonly Text titleText;
        private readonly Button jumpButton;
        private readonly Text descriptionText;

        internal WebController(Transform transform, Action onWebLogin) {
            this.onWebLogin = onWebLogin;

            titleText = transform.Find("Title").GetComponent<Text>();
            jumpButton = transform.Find("JumpButton").GetComponent<Button>();
            descriptionText = transform.Find("Description").GetComponent<Text>();

            jumpButton.onClick.AddListener(OnJumpClicked);
        }

        internal void Load() {
            ILoginLang lang = LoginLanguage.GetCurrentLang();
            titleText.text = lang.WebLogin();
            Text jumpText = jumpButton.transform.Find("Text").GetComponent<Text>();
            jumpText.text = lang.WebButtonJumpToWeb();
            descriptionText.text = lang.WebNotice();
        }

        protected virtual void OnJumpClicked() {
            onWebLogin.Invoke();
        }
    }
}
