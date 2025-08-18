using System;
using UnityEngine.UI;
using TapSDK.UI;
using UnityEngine;

namespace TapSDK.Compliance.Internal {
    public class TapTapComplianceQuickVerifyTipController : BasePanelController {
        public class OpenParams : AbstractOpenPanelParameter {
            public Action<bool> onClicked;
            public bool justShowConfirmBtn;
            public Action onClose;
        }
        
        public Text titleText;
        public Text mainIntroText;
        public Text confirmBtn1Text;
        public Text confirmBtn2Text;
        public Text denyBtnText;
        
        public Button confirmBtn1;
        public Button confirmBtn2;
        public Button denyBtn;
        public Button closeBtn;
        
        private OpenParams param;

        public ScrollRect scrollRect;
        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            titleText = transform.Find("Root/TitleText").GetComponent<Text>();
            scrollRect = transform.Find("Root/Scroll View").GetComponent<ScrollRect>();
            mainIntroText = scrollRect.transform.Find("Viewport/Content/GameIntro").GetComponent<Text>();
            confirmBtn1Text = transform.Find("Root/Button1/ConfirmBtn/Text").GetComponent<Text>();
            confirmBtn2Text = transform.Find("Root/Button2/ConfirmBtn/Text").GetComponent<Text>();
            denyBtnText = transform.Find("Root/Button2/DenyBtn/Text").GetComponent<Text>();
            
            confirmBtn1 = transform.Find("Root/Button1/ConfirmBtn").GetComponent<Button>();
            confirmBtn2 = transform.Find("Root/Button2/ConfirmBtn").GetComponent<Button>();
            denyBtn = transform.Find("Root/Button2/DenyBtn").GetComponent<Button>();
            closeBtn = transform.Find("Root/CloseButton").GetComponent<Button>();
        
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();
            param = openParam as OpenParams;

            closeBtn.onClick.AddListener(() => {
                Close();
                param.onClose?.Invoke();
            });
            
            confirmBtn1.onClick.RemoveAllListeners();
            confirmBtn1.gameObject.SetActive(false);
            confirmBtn2.onClick.RemoveAllListeners();
            confirmBtn2.gameObject.SetActive(false);
            denyBtn.onClick.RemoveAllListeners();
            denyBtn.gameObject.SetActive(false);
            if (param.justShowConfirmBtn) {
                confirmBtn1.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(true);
                });
                confirmBtn1.gameObject.SetActive(true);
            }
            else {
                confirmBtn2.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(true);
                });
                confirmBtn2.gameObject.SetActive(true);
                denyBtn.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(false);
                });
                denyBtn.gameObject.SetActive(true);
            }
            
            var config = Config.GetQuickVerifyTipPanelTip();
            if (config != null) {
                titleText.text = config.Title;
                mainIntroText.text = config.Content.Replace(" ","\u00A0");//.Substring(0, index + + splitter.Length);
                Debug.Log("set mainIntro text = " + config.Content);
                int lineCount;
                float lineHeight;
                if (IsTextOverflowing(mainIntroText, out lineCount, out lineHeight)) {
                    scrollRect.enabled = true;
                    mainIntroText.rectTransform.sizeDelta = new Vector2(mainIntroText.rectTransform.sizeDelta.x,
                        40 + (lineCount - 4) * lineHeight);
                    
                    var contentRect = scrollRect.transform.Find("Viewport/Content").GetComponent<RectTransform>();
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x,
                        40 + (lineCount - 4) * lineHeight);
                }
                else {
                    scrollRect.enabled = false;
                }
                confirmBtn1Text.text = config.PositiveButtonText;
                confirmBtn2Text.text = config.PositiveButtonText;
                denyBtnText.text = config.NegativeButtonText;
            }
        }
        
        bool IsTextOverflowing(Text text, out int lineCount, out float lineHeight)
        {
            var textGenerator = text.cachedTextGenerator;
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            textGenerator.Populate(text.text, settings);
            lineCount = textGenerator.lineCount;
            lineHeight = 15 + (text.lineSpacing - 1) * 15;
            return textGenerator.lineCount > 6;
        }
        
    }
}