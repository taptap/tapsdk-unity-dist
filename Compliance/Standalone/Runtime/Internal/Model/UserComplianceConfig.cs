using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Compliance.Model
{
    
    public class UserState
    {
        // Tap 快速实名认证提示
        [JsonProperty("age_limit")]
        public int ageLimit;

        // 手动实名认证提示
        [JsonProperty("is_adult")]
        public bool isAdult;

    }

    public class Policy
    {
        // 政策
        [JsonProperty("policy_active")]
        public string active;

        // 执行策略本地或服务端
        [JsonProperty("policy_model")]
        public string model;

        //心跳间隔
        [JsonProperty("policy_heartbeat_interval")]
        public int heartbeatInterval;


    }

    public class AgeCheckResult
    {
        // 政策
        [JsonProperty("allow")]
        public bool allow;

    }

    public class Local
    {
        // 政策
        [JsonProperty("time_range")]
        public TimeRangeConfig timeRangeConfig;

    }

    public class TimeRangeConfig
    {
        // 政策
        [JsonProperty("time_start")]
        public string timeStart;

        [JsonProperty("time_end")]
        public string timeEnd;

        [JsonProperty("text")]
        public HealthReminderTip uITipText;

        [JsonProperty("holidays")]
        public List<string> holidays;

    }

    public class HealthReminderTip
    {
        [JsonProperty("allow")]
        public HealthReminderDesc allow;

        [JsonProperty("reject")]
        public HealthReminderDesc reject;
    }

    public class HealthReminderDesc
    {
        [JsonProperty("title")]
        public string tipTitle="";

        [JsonProperty("description_plain")]
        public string tipDescription="";
    }

    public class UserComplianceConfigResult 
    {
        // 实名过程 UI 提示文案
        [JsonProperty("real_name")]
        public UserState userState { get; private set; }

        [JsonProperty("anti_addiction")]
        public Policy policy { get; private set; }

        [JsonProperty("content_rating_check")]
        public AgeCheckResult ageCheckResult { get; private set; }

        [JsonProperty("local")]
        public Local localConfig { get; private set; }

    }

    internal class UserComplianceConfigResponse : BaseResponse 
    {
        [JsonProperty("data")]
        internal UserComplianceConfigResult Result { get; private set; }
    }
}
