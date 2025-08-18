using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core;
using Newtonsoft.Json;
using TapSDK.Achievement.Internal.Util;
using TapSDK.Achievement;

namespace TapSDK.Achievement.Mobile
{
    public class TapAchievementMobile : ITapTapAchievement
    {

        private const string SERVICE_NAME = "BridgeAchievementService";

        private static bool hasRegisterCallBack = false;

        private static List<ITapAchievementCallback> callbacks = new List<ITapAchievementCallback>();

        public TapAchievementMobile()
        {
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.achievement.unity.internal.BridgeAchievementService",
                "com.taptap.sdk.achievement.unity.internal.BridgeAchievementServiceImpl");
        }

        public void Init(string clientId, TapTapRegionType regionType, TapTapAchievementOptions achievementOptions)
        {
        }

        public void Increment(string achievementId, int step = 0)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("increment")
                .Args("achievementId", achievementId)
                .Args("steps", step)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void Unlock(string achievementId)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("unlock")
                .Args("achievementId", achievementId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void ShowAchievements()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("showAchievements")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void SetToastEnable(bool enable)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("setToastEnable")
                .Args("enableToast", enable)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void RegisterCallBack(ITapAchievementCallback callback)
        {
            InitRegisterCallBack();
            callbacks.Add(callback);
        }

        public void UnRegisterCallBack(ITapAchievementCallback callback)
        {
            callbacks.Remove(callback);
        }

        private void InitRegisterCallBack()
        {
            if (hasRegisterCallBack)
            {
                return;
            }
            hasRegisterCallBack = true;
            var command = new Command.Builder();
            command.Service(SERVICE_NAME);
            command.Method("registerCallback")
                .Callback(true)
                .OnceTime(false);
            EngineBridge.GetInstance().CallHandler(command.CommandBuilder(), (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                Debug.Log("TapSdk4UnityDemo -->> Bridge Callback == " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var state = SafeDictionary.GetValue<string>(dic, "state");
                switch (state)
                {
                    case "success":
                        var code = SafeDictionary.GetValue<int>(dic, "code");
                        var resultJson = SafeDictionary.GetValue<string>(dic, "result");
                        TapAchievementLog.Log($"TapAchievementMobile -- success -- code: {code}, result: {resultJson}");
                        TapAchievementResult tapAchievementResult = TapAchievementResult.FromJson(resultJson);
                        callbacks.ForEach((x) =>
                        {
                            x.OnAchievementSuccess(code: code, result: tapAchievementResult);
                        });
                        break;
                    case "failure":
                        var achievementId = SafeDictionary.GetValue<string>(dic, "achievementId");
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errorCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errorMsg");
                        TapAchievementLog.Log($"TapAchievementMobile -- failure -- achievementId: {achievementId}, errorCode: {errorCode}, errorMsg: {errorMsg}");
                        callbacks.ForEach((x) =>
                        {
                            x.OnAchievementFailure(achievementId: achievementId, errorCode: errorCode, errorMsg: errorMsg ?? "");
                        });
                        break;
                }

            });
        }

    }
}
