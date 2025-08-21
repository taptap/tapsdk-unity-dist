using System;
using UnityEngine.UI;
using TapSDK.UI;
using UnityEngine;
using TapSDK.Compliance.Model;
using TapSDK.Core;

namespace TapSDK.Compliance.Internal {
    public class TaptapComplianceHealthReminderController : BasePanelController
    {
        public Text titleText;
        public Text contentText;
        public Button switchAccountButton;
        public Button okButton;
        public ScrollRect scrollRect;

        private Action OnOk { get; set; }
        private Action OnSwitchAccount { get; set; }

        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            titleText = transform.Find("Root/TitleText").GetComponent<Text>();
            scrollRect = transform.Find("Root/Scroll View").GetComponent<ScrollRect>();
            contentText = transform.Find("Root/Scroll View/Viewport/Content/ContentText").GetComponent<Text>();
            switchAccountButton = transform.Find("Root/SwitchAccountButton").GetComponent<Button>();
            okButton = transform.Find("Root/OKButton").GetComponent<Button>();
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();

            switchAccountButton.onClick.AddListener(OnSwitchAccountButtonClicked);
            okButton.onClick.AddListener(OnOKButtonClicked);
        }

        internal void Show(PlayableResult playable, Action onOk, Action onSwitchAccount)
        {
            OnOk = onOk;
            OnSwitchAccount = onSwitchAccount;
            string title = playable.Title;
            string content = playable.Content;
            //服务端异常，返回了无效的提示文案,为避免异常闪退，使用保底文案
            if(title == null || title.Length == 0 || content == null || content.Length == 0){
                HealthReminderDesc healthReminderDesc;
                if(playable.RemainTime > 0){
                    healthReminderDesc = TapTapComplianceManager.CurrentUserAntiResult.localConfig.timeRangeConfig.uITipText.allow;
                }else{
                    healthReminderDesc = TapTapComplianceManager.CurrentUserAntiResult.localConfig.timeRangeConfig.uITipText.reject;
                }
                title = healthReminderDesc.tipTitle;
                content = healthReminderDesc.tipDescription;
            }
            titleText.text = title;
            // 替换富文本标签
            //周六、周日和法定节假日每日 20 时至 21 时向未成年人提供 60 分钟网络游戏服务
            TapLogger.Debug("remain tip = " + content);    
            if(content.Contains("# ${remaining} #")){    
                string timeDesc;
                if(playable.RemainTime >= 60){
                    int remainTime = (int)Math.Ceiling(playable.RemainTime * 1d / 60);
                    timeDesc = remainTime.ToString();
                    content = content.Replace("# ${remaining} #", timeDesc);
                    TapLogger.Debug("remain tip = " + content); 
                }else{
                    int index = content.IndexOf("# ${remaining} #");
                    string substring1 = content.Substring(0,index);
                    string substring2 = content.Substring(index)
                        .Replace("# ${remaining} #", playable.RemainTime.ToString())
                        .Replace("分钟","秒");
                    TapLogger.Debug("remain tip sub1 = " + substring1 + " sub2 = " + substring2); 
                    content = substring1 + substring2;    
                }
            }
           
            contentText.text = content.Replace(" ", "\u00A0");
            if (IsTextOverflowing(contentText, out int lineCount, out float lineHeight)) {
                scrollRect.enabled = true;
                contentText.rectTransform.sizeDelta = new Vector2(contentText.rectTransform.sizeDelta.x,
                    lineCount * lineHeight);
                    
                var contentRect = scrollRect.transform.Find("Viewport/Content").GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x,
                    lineCount * lineHeight);
            }
            else {
                scrollRect.enabled = false;
            }
            switchAccountButton.gameObject.SetActive(onSwitchAccount != null);

            var buttonText = okButton.transform.Find("Text").GetComponent<Text>();
            var switchButtonText = switchAccountButton.transform.Find("Text").GetComponent<Text>();
           
                buttonText.text = playable.RemainTime > 0
                    ? TapTapComplianceManager.LocalizationItems.Current.EnterGame
                    : TapTapComplianceManager.LocalizationItems.Current.ExitGame;
            }

        

        private void OnOKButtonClicked()
        {
            Close();
            OnOk?.Invoke();
        }

        private void OnSwitchAccountButtonClicked()
        {
            Close();
            OnSwitchAccount?.Invoke();
        }
        
        bool IsTextOverflowing(Text text, out int lineCount, out float lineHeight)
        {
            var textGenerator = text.cachedTextGenerator;
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            textGenerator.Populate(text.text, settings);
            lineCount = textGenerator.lineCount;
            if (TapTapComplianceManager.IsUseMobileUI()) {
                lineCount += 3;
            }
            lineHeight = 25 + (text.lineSpacing - 1) * 25;
            return lineCount > 5;
        }
    }
}