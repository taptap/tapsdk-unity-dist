using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Compliance.Model
{
    
  
    public class Prompt
    {

        /// <summary>
        /// 标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; private set; }      
        
        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty("description_plain")]
        public string Content { get; private set; }
        
        /// <summary>
        /// 否定按钮内容
        /// </summary>
        [JsonProperty("negative_button")]
        public string NegativeButtonText { get; private set; }
        
        /// <summary>
        /// 否定按钮内容
        /// </summary>
        [JsonProperty("positive_button")]
        public string PositiveButtonText { get; private set; }
    }
    
    
    public class RealNameText
    {
        // Tap 快速实名认证提示
        [JsonProperty("taptap_auth")]
        public Prompt tapAuthTip;

        // 手动实名认证提示
        [JsonProperty("manual_auth")]
        public Prompt manualAuthTip;

        //认证信息正在审核中提示
        [JsonProperty("auth_waiting")]
        public Prompt authWaitingTip;

        //上次手动认证失败，再次手动认证提示
        [JsonProperty("auth_failed")]
        public Prompt manualAuthFailedTip;
    }


    public class RealNameConfigResult 
    {
        // 实名过程 UI 提示文案
        [JsonProperty("real_name_text")]
        public RealNameText realNameText { get; private set; }

        [JsonProperty("manual_auth_enable")]
        public bool useManual = true;

    }

    internal class RealNameConfigResponse : BaseResponse 
    {
        [JsonProperty("data")]
        internal RealNameConfigResult Result { get; private set; }
    }
}
