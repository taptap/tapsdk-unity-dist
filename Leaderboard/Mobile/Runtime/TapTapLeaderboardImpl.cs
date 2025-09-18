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

        public Task<SubmitScoresResponse> SubmitScores(List<SubmitScoresRequest.ScoreItem> scores)
        {
            TapLog.Log("[TapTapLeaderboardImpl] SubmitScores called with Task<SubmitScoresResponse> return type (NEW API)");
            var taskSource = new TaskCompletionSource<SubmitScoresResponse>();
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("submitScores")
                .Args("scores", JsonConvert.SerializeObject(scores))
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                try
                {
                    if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to submit scores: code=" + result.code + " content=" + result.content));
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
                            if (data != null)
                            {
                                taskSource.TrySetResult(data);
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "json convert failed: content=" + jsonStr));
                            }
                            break;
                        case "failure":
                            var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                            var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                            TapLog.Log("failed to submit scores, errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                            taskSource.TrySetException(new TapException(errorCode, errorMsg));
                            break;
                        default:
                            taskSource.TrySetException(new TapException(-1, "Unknown status: " + status));
                            break;
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to submit scores: error=" + e.Message + ", content=" + result.content));
                }
            });
            return taskSource.Task;
        }

        public Task<LeaderboardScoreResponse> LoadLeaderboardScores(
            string leaderboardId,
            string leaderboardCollection,
            string nextPage,
            string periodToken)
        {
            TapLog.Log("[TapTapLeaderboardImpl] LoadLeaderboardScores called with Task<LeaderboardScoreResponse> return type (NEW API)");
            var taskSource = new TaskCompletionSource<LeaderboardScoreResponse>();
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadLeaderboardScores")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("nextPage", nextPage)
                .Args("periodToken", periodToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                try
                {
                    if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to load leaderboard scores: code=" + result.code + " content=" + result.content));
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
                            if (data != null)
                            {
                                taskSource.TrySetResult(data);
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "json convert failed: content=" + jsonStr));
                            }
                            break;
                        case "failure":
                            var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                            var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                            TapLog.Log("load leaderboard scores failed, errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                            taskSource.TrySetException(new TapException(errorCode, errorMsg));
                            break;
                        default:
                            taskSource.TrySetException(new TapException(-1, "Unknown status: " + status));
                            break;
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to load leaderboard scores: error=" + e.Message + ", content=" + result.content));
                }
            });
            return taskSource.Task;
        }

        public Task<UserScoreResponse> LoadCurrentPlayerLeaderboardScore(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken)
        {
            TapLog.Log("[TapTapLeaderboardImpl] LoadCurrentPlayerLeaderboardScore called with Task<UserScoreResponse> return type (NEW API)");
            var taskSource = new TaskCompletionSource<UserScoreResponse>();
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadCurrentPlayerLeaderboardScore")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("periodToken", periodToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                try
                {
                    if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to load current player leaderboard score: code=" + result.code + " content=" + result.content));
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
                            if (data != null)
                            {
                                taskSource.TrySetResult(data);
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "json convert failed: content=" + jsonStr));
                            }
                            break;
                        case "failure":
                            var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                            var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                            TapLog.Log("Load current player leaderboard score failed: errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                            taskSource.TrySetException(new TapException(errorCode, errorMsg));
                            break;
                        default:
                            taskSource.TrySetException(new TapException(-1, "Unknown status: " + status));
                            break;
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to load current player leaderboard score: error=" + e.Message + ", content=" + result.content));
                }
            });
            return taskSource.Task;
        }

        public Task<LeaderboardScoreResponse> LoadPlayerCenteredScores(
            string leaderboardId,
            string leaderboardCollection,
            string periodToken,
            int? maxCount)
        {
            TapLog.Log("[TapTapLeaderboardImpl] LoadPlayerCenteredScores called with Task<LeaderboardScoreResponse> return type (NEW API)");
            var taskSource = new TaskCompletionSource<LeaderboardScoreResponse>();
            var command = new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("loadPlayerCenteredScores")
                .Args("leaderboardId", leaderboardId)
                .Args("leaderboardCollection", leaderboardCollection)
                .Args("periodToken", periodToken)
                .Args("maxCount", maxCount)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder();
            EngineBridge.GetInstance().CallHandler(command, result =>
            {
                try
                {
                    if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                    {
                        taskSource.TrySetException(new TapException(-1, "Failed to load player centered scores: code=" + result.code + " content=" + result.content));
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
                            if (data != null)
                            {
                                taskSource.TrySetResult(data);
                            }
                            else
                            {
                                taskSource.TrySetException(new TapException(-1, "json convert failed: content=" + jsonStr));
                            }
                            break;
                        case "failure":
                            var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                            var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                            TapLog.Log("Load failed load player centered scores:: errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                            taskSource.TrySetException(new TapException(errorCode, errorMsg));
                            break;
                        default:
                            taskSource.TrySetException(new TapException(-1, "Unknown status: " + status));
                            break;
                    }
                }
                catch (Exception e)
                {
                    taskSource.TrySetException(new TapException(-1, "Failed to load player centered scores: error=" + e.Message + ", content=" + result.content));
                }
            });
            return taskSource.Task;
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
                try
                {
                    if (result.code != Result.RESULT_SUCCESS)
                    {
                        TapLog.Log("InitRegisterCallBack failed with code: " + result.code);
                        return;
                    }

                    if (string.IsNullOrEmpty(result.content))
                    {
                        TapLog.Log("InitRegisterCallBack result content is empty");
                        return;
                    }
                    
                    TapLog.Log("InitRegisterCallBack result content: " + result.content);
                    var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                    var status = SafeDictionary.GetValue<string>(dic, "status");
                    switch (status)
                    {
                        case "success":
                            // Fix: data is already an object, not a string that needs deserialization
                            var dataDic = SafeDictionary.GetValue<Dictionary<string, object>>(dic, "data");
                            if (dataDic != null)
                            {
                                var code = SafeDictionary.GetValue<int>(dataDic, "code");
                                var message = SafeDictionary.GetValue<string>(dataDic, "message");
                                TapLog.Log("InitRegisterCallBack callback result - code: " + code + ", message: " + message);
                                foreach (var callback in callbacks)
                                {
                                    callback.OnLeaderboardResult(code, message);
                                }
                            }
                            else
                            {
                                TapLog.Log("InitRegisterCallBack data field is null or invalid");
                            }
                            break;
                        
                        case "failure":
                            var errorCode = SafeDictionary.GetValue<int>(dic, "errCode");
                            var errorMsg = SafeDictionary.GetValue<string>(dic, "errMessage");
                            TapLog.Log("InitRegisterCallBack failed - errorCode: " + errorCode + ", errorMsg: " + errorMsg);
                            foreach (var callback in callbacks)
                            {
                                callback.OnLeaderboardResult(errorCode, errorMsg);
                            }
                            break;
                        default:
                            TapLog.Log("InitRegisterCallBack unknown status: " + status);
                            break;
                    }
                }
                catch (Exception e)
                {
                    TapLog.Log("InitRegisterCallBack exception: " + e.Message + ", content: " + result.content);
                }
            });
        }
    }
}