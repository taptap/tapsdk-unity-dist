using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TapSDK.Core;
using Newtonsoft.Json;
using TapSDK.Leaderboard;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Leaderboard.Mobile
{
    public class TapTapLeaderboardImpl : ILeaderboardPlatform
    {

        private const string SERVICE_NAME = "BridgeLeaderboardService";

        private static bool hasRegisterCallBack = false;

        private static List<ITapTapLeaderboardCallback> callbacks = new List<ITapTapLeaderboardCallback>();

        public TapTapLeaderboardImpl()
        {
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.leaderboard.unity.BridgeLeaderboardService",
                "com.taptap.sdk.leaderboard.unity.BridgeLeaderboardServiceImpl");
        }

        public void OpenLeaderboard(string leaderboardId, string collection)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("openLeaderboard")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", collection)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void OpenUserProfile(string openId, string unionId)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("openUserProfile")
                .Args("openId", openId ?? "")
                .Args("unionId", unionId ?? "")
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command);
        }

        public void SubmitScores(List<SubmitScoresRequest.ScoreItem> scores,
            ITapTapLeaderboardResponseCallback<SubmitScoresResponse> callback)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("submitScores")
                .Args("scores", JsonConvert.SerializeObject(scores))
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                TapLog.Log("SubmitScores, result ==>>> " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "success":
                        var jsonStr = SafeDictionary.GetValue<string>(dic, "data");
                        TapLog.Log("submit scores success: " + jsonStr);
                        var data = JsonConvert.DeserializeObject<SubmitScoresResponse>(jsonStr);
                        if (callback != null)
                        {
                            callback.OnSuccess(data);
                        }
                        break;
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        TapLog.Log("failed to submit scores, errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                        if (callback != null)
                        {
                            callback.OnFailure(errorCode, errorMsg);
                        }
                        break;
                }
            });
        }

        public void LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken,
            ITapTapLeaderboardResponseCallback<LeaderboardScoreResponse> callback)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadLeaderboardScores")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("nextPage", nextPage)
                .Args("periodToken", periodToken)
                .CommandBuilder();
            
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                TapLog.Log("LoadLeaderboardScores, result ==>>> " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "success":
                        var jsonStr = SafeDictionary.GetValue<string>(dic, "data");
                        TapLog.Log("load leaderboard scores success: " + jsonStr);
                        var data = JsonConvert.DeserializeObject<LeaderboardScoreResponse>(jsonStr);
                        if (callback != null)
                        {
                            callback.OnSuccess(data);
                        }
                        break;
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        TapLog.Log("load leaderboard scores failed, errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                        if (callback != null)
                        {
                            callback.OnFailure(errorCode, errorMsg);
                        }
                        break;
                }
            });
        }

        public void LoadCurrentPlayerLeaderboardScore(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            ITapTapLeaderboardResponseCallback<UserScoreResponse> callback)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadCurrentPlayerLeaderboardScore")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("periodToken", periodToken)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                TapLog.Log("LoadCurrentPlayerLeaderboardScore, result ==>>> " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "success":
                        var jsonStr = SafeDictionary.GetValue<string>(dic, "data");
                        TapLog.Log("Load current player leaderboard score success: " + jsonStr);
                        var data = JsonConvert.DeserializeObject<UserScoreResponse>(jsonStr);
                        if (callback != null)
                        {
                            callback.OnSuccess(data);
                        }
                        break;
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        TapLog.Log("Load current player leaderboard score failed: errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                        if (callback != null)
                        {
                            callback.OnFailure(errorCode, errorMsg);
                        }
                        break;
                }
            });
        }

        public void LoadPlayerCenteredScores(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount,
            ITapTapLeaderboardResponseCallback<LeaderboardScoreResponse> callback)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadPlayerCenteredScores")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("periodToken", periodToken)
                .Args("maxCount", maxCount)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                TapLog.Log("LoadPlayerCenteredScores, result ==>>> " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "success":
                        var jsonStr = SafeDictionary.GetValue<string>(dic, "data");
                        TapLog.Log("Load player centered scores success: " + jsonStr);
                        var data = JsonConvert.DeserializeObject<LeaderboardScoreResponse>(jsonStr);
                        if (callback != null)
                        {
                            callback.OnSuccess(data);
                        }
                        break;
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        TapLog.Log("Load failed load player centered scores:: errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                        if (callback != null)
                        {
                            callback.OnFailure(errorCode, errorMsg);
                        }
                        break;
                }
            });
        }

        public void SetShareCallback(ITapTapLeaderboardShareCallback callback)
        {
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("setShareCallback")
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                TapLog.Log("SetShareCallback, result ==>>> " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "success":
                        var localPath = SafeDictionary.GetValue<string>(dic, "data");
                        TapLog.Log("share success: " + localPath);
                        callback.OnShareSuccess(localPath);
                        break;
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        callback.OnShareFailed(new Exception(errorMsg));
                        break;
                }
            });
        }

        public void RegisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
        {
            InitRegisterCallBack();
            callbacks.Add(callback);
        }

        public void UnRegisterLeaderboardCallback(ITapTapLeaderboardCallback callback)
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
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("registerLeaderboardCallback")
                .Callback(true)
                .OnceTime(false)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS)
                {
                    return;
                }

                if (string.IsNullOrEmpty(result.content))
                {
                    return;
                }
                
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var status = SafeDictionary.GetValue<string>(dic, "status");
                switch (status)
                {
                    case "failure":
                        var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                        var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                        foreach (var callback in callbacks)
                        {
                            callback.OnLeaderboardResult(errorCode, errorMsg);
                        }
                        break;
                }
            });
        }
    }
}