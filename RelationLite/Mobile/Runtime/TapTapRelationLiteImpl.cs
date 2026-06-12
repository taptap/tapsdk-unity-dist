using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Utils;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace TapSDK.RelationLite
{
    public class TapTapRelationLiteImpl : ITapTapRelationLite
    {
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void RegisterTapTapSDKRelationLiteAppDelegateListener();
#endif

        private const string SERVICE_NAME = "BridgeRelationLiteService";
        private static List<ITapTapRelationLiteCallback> callbacks = new List<ITapTapRelationLiteCallback>();
        private static List<ITapTapRelationLiteInviteCallback> inviteCallbacks = new List<ITapTapRelationLiteInviteCallback>();
        private static bool hasRegisterCallBack = false;
        private static bool hasRegisterInviteCallBack = false;
        private string _clientId;
        private TapTapRegionType _regionType;

        public TapTapRelationLiteImpl()
        {
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.relation.lite.unity.BridgeRelationLiteService",
                "com.taptap.sdk.relation.lite.unity.BridgeRelationLiteServiceImpl");
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            _clientId = clientId;
            _regionType = regionType;
#if UNITY_IOS
            RegisterTapTapSDKRelationLiteAppDelegateListener();
#endif
            TapLog.Log($"TapTapRelationLite Init with clientId: {clientId}, regionType: {regionType}");
        }

        public void InviteGame()
        {
            TapLog.Log("TapTapRelationLite InviteGame");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("inviteGameByRelationLite")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void InviteTeam(string teamId)
        {
            TapLog.Log($"TapTapRelationLite InviteTeam with teamId: {teamId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("inviteTeam")
                .Args("relationLiteTeamId", teamId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public Task<RelationLiteUserResult> GetFriendsList(string nextPageToken)
        {
            var taskSource = new TaskCompletionSource<RelationLiteUserResult>();
            TapLog.Log($"TapTapRelationLite GetFriendsList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFriendsList")
                .Args("friendListNextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to get friends list with error code: " + result.code + " and content: " + result.content));
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("friends_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "friends_list");
                            List<RelationLiteUserItem> friendsList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            taskSource.TrySetResult(new RelationLiteUserResult(friendsList, nextPage));
                        }
                        else
                        {
                            string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message", "unknown error");
                            taskSource.TrySetException(new TapException(-1, errorMessage));
                        }
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(new TapException(-1, e.Message));
                        TapLog.Error($"GetFriendsList parse result error: {e.Message}");
                    }
                });
            return taskSource.Task;
        }

        public Task<RelationLiteUserResult> GetFollowingList(string nextPageToken)
        {
            var taskSource = new TaskCompletionSource<RelationLiteUserResult>();
            TapLog.Log($"TapTapRelationLite GetFollowingList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFollowingList")
                .Args("followingListNextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to get friends list with error code: " + result.code + " and content: " + result.content));
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("following_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "following_list");
                            List<RelationLiteUserItem> followingList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            taskSource.TrySetResult(new RelationLiteUserResult(followingList, nextPage));
                        }
                        else
                        {
                            string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message", "unknown error");
                            taskSource.TrySetException(new TapException(-1, errorMessage));
                        }
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(new TapException(-1, e.Message));
                        TapLog.Error($"GetFollowingList parse result error: {e.Message}");
                    }
                });
            return taskSource.Task;
        }

        public Task<RelationLiteUserResult> GetFansList(string nextPageToken)
        {
            var taskSource = new TaskCompletionSource<RelationLiteUserResult>();
            TapLog.Log($"TapTapRelationLite GetFansList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFansList")
                .Args("fansListNextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to get fans list with error code: " + result.code + " and content: " + result.content));
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("fans_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "fans_list");
                            List<RelationLiteUserItem> fansList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            taskSource.TrySetResult(new RelationLiteUserResult(fansList, nextPage));
                        }
                        else
                        {
                            string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message", "unknown error");
                            taskSource.TrySetException(new TapException(-1, errorMessage));
                        }
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(new TapException(-1, e.Message));
                        TapLog.Error($"GetFansList parse result error: {e.Message}");
                    }
                });
            return taskSource.Task;
        }

        public Task SyncRelationshipWithOpenId(int action, string nickname, string friendNickname, string friendOpenId)
        {
            var taskSource = new TaskCompletionSource<bool>();
            TapLog.Log($"TapTapRelationLite SyncRelationshipWithOpenId with action: {action}, nickname: {nickname}, friendNickname: {friendNickname}, friendOpenId: {friendOpenId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("syncRelationshipWithOpenId")
                .Args("syncRelationAction", action)
                .Args("nickname", nickname)
                .Args("friendNickname", friendNickname)
                .Args("friendOpenId", friendOpenId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to sync relationship with open id with error code: " + result.code + " and content: " + result.content));
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("success"))
                        {
                            int success = SafeDictionary.GetValue<int>(dic, "success", -1);
                            if (success == 0)
                            {
                                taskSource.TrySetResult(true);
                            }
                            else
                            {
                                string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message", "unknown error");
                                taskSource.TrySetException(new TapException(-1, errorMessage));
                            }
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to sync relationship with open id with error code: " + result.code + " and content: " + result.content));

                        }
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(new TapException(-1, e.Message));
                        TapLog.Error($"SyncRelationshipWithOpenId parse result error: {e.Message}");
                    }
                });
            return taskSource.Task;
        }

        public Task SyncRelationshipWithUnionId(int action, string nickname, string friendNickname, string friendUnionId)
        {
            var taskSource = new TaskCompletionSource<bool>();
            TapLog.Log($"TapTapRelationLite SyncRelationshipWithUnionId with action: {action}, nickname: {nickname}, friendNickname: {friendNickname}, friendUnionId: {friendUnionId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("syncRelationshipWithUnionId")
                .Args("syncRelationAction", action)
                .Args("nickname", nickname)
                .Args("friendNickname", friendNickname)
                .Args("friendUnionId", friendUnionId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to sync relationship with union id with error code: " + result.code + " and content: " + result.content));
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("success"))
                        {
                            int success = SafeDictionary.GetValue<int>(dic, "success", -1);
                            if (success == 0)
                            {
                                taskSource.TrySetResult(true);
                            }
                            else
                            {
                                string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message", "unknown error");
                                int errorCode = SafeDictionary.GetValue<int>(dic, "error_code", -1);
                                taskSource.TrySetException(new TapException(errorCode, errorMessage));
                            }
                        }
                        else
                        {
                            taskSource.TrySetException(new TapException(-1, "Failed to sync relationship with union id with error code: " + result.code + " and content: " + result.content));
                        }
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(new TapException(-1, e.Message));
                        TapLog.Error($"SyncRelationshipWithOpenId parse result error: {e.Message}");
                    }
                });
            return taskSource.Task;
        }

        public void ShowTapUserProfile(string openId, string unionId)
        {
            TapLog.Log($"TapTapRelationLite ShowTapUserProfile with openId: {openId}, unionId: {unionId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("showTapUserProfile")
                .Args("relationLiteOpenId", openId)
                .Args("unionId", unionId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void RegisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            if (callback == null) return;
            InitRegisterCallBack();
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
                TapLog.Log("TapTapRelationLite RegisterRelationLiteCallback");
            }
        }

        public void UnregisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            if (callback != null)
            {
                callbacks.Remove(callback);
                // 当引擎中清除所有回调时，移除原生 callback
                if (callbacks.Count == 0 && hasRegisterCallBack)
                {
                    var command = new Command.Builder()
                        .Service(SERVICE_NAME)
                        .Method("unregisterRelationLiteCallback")
                        .Callback(false)
                        .OnceTime(false);
                    EngineBridge.GetInstance().CallHandler(command.CommandBuilder());
                    hasRegisterCallBack = false;
                    TapLog.Log("TapTapRelationLite UnregisterRelationLiteCallback");
                }
            }
        }

        public void RegisterRelationLiteInviteCallback(ITapTapRelationLiteInviteCallback callback)
        {
            if (callback == null) return;
            InitRegisterInviteCallBack();
            if (!inviteCallbacks.Contains(callback))
            {
                inviteCallbacks.Add(callback);
                TapLog.Log("TapTapRelationLite RegisterRelationLiteInviteCallback");
            }
        }

        public void UnregisterRelationLiteInviteCallback(ITapTapRelationLiteInviteCallback callback)
        {
            if (callback != null)
            {
                inviteCallbacks.Remove(callback);
                // 当引擎中清除所有回调时，移除原生 invite callback
                if (inviteCallbacks.Count == 0 && hasRegisterInviteCallBack)
                {
                    var command = new Command.Builder()
                        .Service(SERVICE_NAME)
                        .Method("unregisterRelationLiteInviteCallback")
                        .Callback(false)
                        .OnceTime(false);
                    EngineBridge.GetInstance().CallHandler(command.CommandBuilder());
                    hasRegisterInviteCallBack = false;
                    TapLog.Log("TapTapRelationLite UnregisterRelationLiteInviteCallback");
                }
            }
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
            command.Method("registerRelationLiteCallback")
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
                TapLog.Log("TapSdk4UnityDemo -->> Bridge Callback == " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var code = SafeDictionary.GetValue<int>(dic, "relation_lite_result_code");

                if (code != null)
                {
                    callbacks.ForEach((x) => x.OnResult(code));
                }

            });

            TapLog.Log("TapTapRelationLite InitRegisterCallBack");
        }

        private void InitRegisterInviteCallBack()
        {
            if (hasRegisterInviteCallBack)
            {
                return;
            }
            hasRegisterInviteCallBack = true;

            var command = new Command.Builder();
            command.Service(SERVICE_NAME);
            command.Method("registerRelationLiteInviteCallback")
                .Callback(true)
                .OnceTime(false);
            EngineBridge.GetInstance().CallHandler(command.CommandBuilder(), (result) =>
            {
                if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                {
                    return;
                }

                try
                {
                    TapLog.Log("TapTapRelationLite Invite -->> Bridge Callback == " + JsonConvert.SerializeObject(result));
                    var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                    var inviteType = SafeDictionary.GetValue<string>(dic, "invite_type");
                    var openId = SafeDictionary.GetValue<string>(dic, "open_id");
                    var unionId = SafeDictionary.GetValue<string>(dic, "union_id");
                    var teamId = SafeDictionary.GetValue<string>(dic, "team_id");

                    if (inviteType == "invite_team")
                    {
                        inviteCallbacks.ForEach(x => x.OnTeamInviteReceived(openId, unionId, teamId));
                    }
                    else if (inviteType == "invite_game")
                    {
                        inviteCallbacks.ForEach(x => x.OnGameInviteReceived(openId, unionId));
                    }
                }
                catch (Exception e)
                {
                    TapLog.Error($"TapTapRelationLite invite callback parse result error: {e.Message}");
                }
            });
        }
    }
}
