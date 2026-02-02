
using TapSDK.UI;
using TapSDK.Compliance.Model;
using UnityEngine.UI;
using System;
using UnityEngine;

namespace TapSDK.Compliance.Internal 
{
    public class TaptapComplianceHealthPaymentController : BasePanelController
    {
        public Text titleText;
        public Text contentText;
        public Text buttonText;
        public Button okButton;
        public ScrollRect scrollRect;
        private Action _onOk;

        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            titleText = transform.Find("Root/TitleText").GetComponent<Text>();
            scrollRect = transform.Find("Root/Scroll View").GetComponent<ScrollRect>();
            contentText = transform.Find("Root/Scroll View/Viewport/Content/ContentText").GetComponent<Text>();
            okButton = transform.Find("Root/OKButton").GetComponent<Button>();
            buttonText = okButton.transform.Find("Text").GetComponent<Text>();
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();

            okButton.onClick.AddListener(OnOKButtonClicked);
        }

        internal void Show(PayableResult payable)
        {
            titleText.text = payable.Title;
            contentText.text = ProcessContent(payable.Content);
            // var buttonText = Config.GetHealthTip();
            // if (!string.IsNullOrEmpty(buttonText))
            //     this.buttonText.text = buttonText;
        }

        internal void Show(string title, string content, string buttonText, Action onOk = null)
        {
            titleText.text = title;
            contentText.text = ProcessContent(content);
            if (!string.IsNullOrEmpty(buttonText))
                this.buttonText.text = buttonText;
            _onOk = onOk;
        }

        private string ProcessContent(string content)
        {
            return content?.Replace(" ", "\u00A0")?.Replace("&nbsp;", "\u00A0");
        }

        private void OnOKButtonClicked()
        {
            Close();
            _onOk?.Invoke();
        }
    }
}