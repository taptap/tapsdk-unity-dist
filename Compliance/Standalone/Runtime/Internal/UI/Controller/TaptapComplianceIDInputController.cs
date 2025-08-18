using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using TapSDK.UI;
using TapSDK.Compliance.Model;
using TapSDK.Compliance.Standalone.Internal;

namespace TapSDK.Compliance.Internal
{
    public class TaptapComplianceIDInputController : BasePanelController
    {
        public Button closeButton;
        public Button submitButton;

        public InputField nameInputField;
        public InputField idNumberInputField;

        public Text titleText;
        public Text descriptionText;
        public Text buttonText;

        public ScrollRect scrollRect;

        public Action<VerificationResult> OnVerified;
        public Action<Exception> OnException;
        public Action OnClosed;

        public bool activeManualVerification;
        private bool _isSending;

        // 上次服务端已校验过的错误的用户名
        private string lastErrorUserName = "";

        // 上次服务端已校验过的错误的用户身份证
        private string lastErrorUserIdNumber = "";


        private bool isSending
        {
            get => _isSending;
            set
            {
                if (value != _isSending)
                {
                    _isSending = value;
                    if (_isSending)
                        UIManager.Instance.OpenLoading();
                    else
                        UIManager.Instance.CloseLoading();
                }
            }
        }

        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            TapComplianceTracker.Instance.currentVerifyType = "manual_verify";
            closeButton = transform.Find("Root/CloseButton").GetComponent<Button>();
            submitButton = transform.Find("Root/SubmitButton").GetComponent<Button>();
            scrollRect = transform.Find("Root/Scroll View").GetComponent<ScrollRect>();
            titleText = transform.Find("Root/TitleText").GetComponent<Text>();
            descriptionText = scrollRect.transform.Find("Viewport/Content/ContentText").GetComponent<Text>();
            buttonText = submitButton.transform.Find("Text").GetComponent<Text>();

            nameInputField = transform.Find("Root/NameInput").GetComponent<InputField>();
            idNumberInputField = transform.Find("Root/IDNumInput").GetComponent<InputField>();
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();

            closeButton.onClick.AddListener(OnCloseButtonClicked);
            submitButton.onClick.AddListener(OnConfirmButtonClicked);

            var config = Config.GetInputIdentifyTip();
            if (config != null)
            {
                titleText.text = config.Title;
                descriptionText.text = config.Content.Replace(" ", "\u00A0");
                if (IsTextOverflowing(descriptionText, out int lineCount, out float lineHeight))
                {
                    scrollRect.enabled = true;
                    descriptionText.rectTransform.sizeDelta = new Vector2(descriptionText.rectTransform.sizeDelta.x,
                        40 + (lineCount - 3) * lineHeight);

                    var contentRect = scrollRect.transform.Find("Viewport/Content").GetComponent<RectTransform>();
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x,
                        40 + (lineCount - 3) * lineHeight);
                }
                else
                {
                    scrollRect.enabled = false;
                }
                buttonText.text = config.PositiveButtonText;
            }

            isSending = false;
        }

        private bool Validate(out string name, out string idNumber, out string errorTip)
        {
            errorTip = null;
            name = nameInputField.text;
            idNumber = idNumberInputField.text;
            if (string.IsNullOrWhiteSpace(name))
            {
                errorTip = "姓名不能为空";
                return false;
            }

            if (!IsIdNum(idNumber))
            {
                errorTip = "身份信息不能为空";
                return false;
            }

            return true;
        }

        private async void OnConfirmButtonClicked()
        {
            if (isSending) return;
            string errorTip;
            string name;
            string idNumber;
            var validation = Validate(out name, out idNumber, out errorTip);
            if (validation)
            {
                try
                {
                    // 如果提交时已明确为错误身份信息，直接弹 toast 提示
                    if (name.Equals(lastErrorUserName) && idNumber.Equals(lastErrorUserIdNumber))
                    {
                        UIManager.Instance.OpenToast("输入信息有误", UIManager.GeneralToastLevel.Error);
                        return;
                    }
                    isSending = true;
                    // 提交后按钮不可用
                    submitButton.interactable = false;
                    var verificationResult = await Verification.FetchVerificationManual(TapTapComplianceManager.UserId, name, idNumber);
                    isSending = false;
                    // 后端返回后按钮可用
                    submitButton.interactable = true;
                    if (!verificationResult.IsVerifyFailed)
                    {
                        // TODO@luran:本地化
                        Close();
                        if (verificationResult.IsVerified)
                        {
                            UIManager.Instance.OpenToast("提交成功", UIManager.GeneralToastLevel.Success);
                        }
                    }
                    OnVerified?.Invoke(verificationResult);
                }
                catch (Exception e)
                {
                    isSending = false;
                    submitButton.interactable = true;
                    // 判断是否为明确的错误身份信息
                    if ( e is ComplianceException  compoliaceError 
                        && compoliaceError.Error != null && compoliaceError.Error.Equals("business_code_error")
                        && compoliaceError.ErrorCode == 200001){
                            lastErrorUserName = name;
                            lastErrorUserIdNumber = idNumber;
                    }
                    OnException?.Invoke(e);
                }
            }
            else
            {
                UIManager.Instance.OpenToast(errorTip, UIManager.GeneralToastLevel.Error);
            }
        }


        private void OnCloseButtonClicked()
        {
            Close();
            OnClosed?.Invoke();
        }

        /// <summary>
        /// 验证身份证号(https://cloud.tencent.com/developer/article/1860685)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static bool IsIdNum(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
            //             double iSum = 0;
            //             // 18位验证
            //             Regex rg = new Regex(@"^\d{17}(\d|x)$");
            //             Match mc = rg.Match(input);
            //             if (!mc.Success)
            //             {
            //                 return false;
            //             }
            //
            //             // 生日验证
            //             input = input.ToLower();
            //             input = input.Replace("x", "a");
            //             try
            //             {
            //                 var year = input.Substring(6, 4);
            //                 var month = input.Substring(10, 2);
            //                 var day = input.Substring(12, 2);
            //                 DateTime.Parse(year + "-" + month + "-" + day);
            //             }
            //             catch
            //             {
            // #if UNITY_EDITOR
            //                TapLogger.Error("国内-防沉迷 身份证号非法出生日期");
            // #endif
            //                 return false;
            //             }
            //
            //             // 最后一位验证
            //             for (int i = 17; i >= 0; i--)
            //             {
            //                 iSum += (Math.Pow(2, i) % 11) *
            //                         int.Parse(input[17 - i].ToString(), System.Globalization.NumberStyles.HexNumber);
            //             }
            //
            //             if (iSum % 11 != 1)
            //             {
            // #if UNITY_EDITOR
            //                 TapLogger.Error("国内-防沉迷 身份证号非法尾号");
            // #endif
            //                 return false;
            //             }
            //
            //             return true;
        }

        bool IsTextOverflowing(Text text, out int lineCount, out float lineHeight)
        {
            var textGenerator = text.cachedTextGenerator;
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            textGenerator.Populate(text.text, settings);
            lineCount = textGenerator.lineCount + 3;
            lineHeight = 15 + (text.lineSpacing - 1) * 15;
            return lineCount > 3;
        }

    }
}