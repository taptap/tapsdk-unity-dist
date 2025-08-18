using System;
using UnityEngine.UI;
using TapSDK.UI;
using TapSDK.Compliance;


namespace TapSDK.Compliance.Internal {
    public class TaptapComplianceRetryAlertController : BasePanelController
    {
        public Text messageText;
        public Text buttonText;
        public Button retryButton;

        private Action _onRetry;

        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            messageText = transform.Find("Root/BackgroundImage/MessageText").GetComponent<Text>();
            retryButton = transform.Find("Root/BackgroundImage/RetryButton").GetComponent<Button>();
            buttonText = retryButton.transform.Find("Text").GetComponent<Text>();
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();
            buttonText.text = TapTapComplianceManager.LocalizationItems.Current.Retry;
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        private void OnRetryClicked()
        {
            Close();
            _onRetry?.Invoke();
        }

        internal void Show(string message, Action onRetry, string confirmButtonText = null)
        {
            messageText.text = message;
            _onRetry = onRetry;
            buttonText.text = string.IsNullOrEmpty(confirmButtonText) ? TapTapComplianceManager.LocalizationItems.Current.Retry : confirmButtonText;
        }
    }
}