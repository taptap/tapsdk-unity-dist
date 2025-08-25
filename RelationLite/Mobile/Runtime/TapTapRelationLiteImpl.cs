using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Utils;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal.Log;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace TapSDK.RelationLite
{
    public class TapTapRelationLiteImpl : ITapTapRelationLite
    {
        private const string SERVICE_NAME = "BridgeRelationLiteService";
        private static List<ITapTapRelationLiteCallback> callbacks = new List<ITapTapRelationLiteCallback>();
        private static bool hasRegisterCallBack = false;
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
            TapLog.Log($"TapTapRelationLite Init with clientId: {clientId}, regionType: {regionType}");
        }

        public void InviteGame()
        {
            TapLog.Log("TapTapRelationLite InviteGame");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("inviteGame")
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
                .Args("teamId", teamId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void GetFriendsList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapLog.Log($"TapTapRelationLite GetFriendsList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFriendsList")
                .Args("nextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback.OnFriendsListResult("", new List<RelationLiteUserItem>());
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("friends_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "friends_list");
                            List<RelationLiteUserItem> friendsList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            callback.OnFriendsListResult(nextPage, friendsList);
                        }
                        else
                        {
                            callback.OnFriendsListResult("", new List<RelationLiteUserItem>());
                        }
                    }
                    catch (Exception e)
                    {
                        callback.OnFriendsListResult("", new List<RelationLiteUserItem>());
                        TapLog.Error($"GetFriendsList parse result error: {e.Message}");
                    }
                });

        }

        public void GetFollowingList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapLog.Log($"TapTapRelationLite GetFollowingList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFollowingList")
                .Args("nextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback.OnFollowingListResult("", new List<RelationLiteUserItem>());
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("following_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "following_list");
                            List<RelationLiteUserItem> followingList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            callback.OnFollowingListResult(nextPage, followingList);
                        }
                        else
                        {
                            callback.OnFollowingListResult("", new List<RelationLiteUserItem>());
                        }
                    }
                    catch (Exception e)
                    {
                        callback.OnFollowingListResult("", new List<RelationLiteUserItem>());
                        TapLog.Error($"GetFollowingList parse result error: {e.Message}");
                    }
                });

        }

        public void GetFansList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapLog.Log($"TapTapRelationLite GetFansList with nextPageToken: {nextPageToken}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getFansList")
                .Args("nextPageToken", nextPageToken)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback.OnFansListResult("", new List<RelationLiteUserItem>());
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("next_page_token") && dic.ContainsKey("fans_list"))
                        {
                            string nextPage = SafeDictionary.GetValue<string>(dic, "next_page_token");
                            string jsonStr = SafeDictionary.GetValue<string>(dic, "fans_list");
                            List<RelationLiteUserItem> fansList = JsonConvert.DeserializeObject<List<RelationLiteUserItem>>(jsonStr);
                            callback.OnFansListResult(nextPage, fansList);
                        }
                        else
                        {
                            callback.OnFansListResult("", new List<RelationLiteUserItem>());
                        }
                    }
                    catch (Exception e)
                    {
                        callback.OnFansListResult("", new List<RelationLiteUserItem>());
                        TapLog.Error($"GetFansList parse result error: {e.Message}");
                    }
                });

        }

        public void SyncRelationshipWithOpenId(int action, string nickname, string friendNickname, string friendOpenId, ITapTapRelationLiteRequestCallback callback)
        {
            TapLog.Log($"TapTapRelationLite SyncRelationshipWithOpenId with action: {action}, nickname: {nickname}, friendNickname: {friendNickname}, friendOpenId: {friendOpenId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("syncRelationshipWithOpenId")
                .Args("action", action)
                .Args("nickname", nickname)
                .Args("friendNickname", friendNickname)
                .Args("friendOpenId", friendOpenId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback.OnSyncRelationshipFail("", "", "");
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("success") && dic.ContainsKey("open_id"))
                        {
                            string openId = SafeDictionary.GetValue<string>(dic, "open_id");
                            bool success = SafeDictionary.GetValue<bool>(dic, "success");
                            if (success)
                            {
                                callback.OnSyncRelationshipSuccess(openId, "");
                            }
                            else
                            {
                                string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message");
                                callback.OnSyncRelationshipFail(errorMessage, openId, "");
                            }
                        }
                        else
                        {
                            callback.OnSyncRelationshipFail("", "", "");
                        }
                    }
                    catch (Exception e)
                    {
                        callback.OnSyncRelationshipFail(e.Message, "", "");
                        TapLog.Error($"SyncRelationshipWithOpenId parse result error: {e.Message}");
                    }
                });

        }

        public void SyncRelationshipWithUnionId(int action, string nickname, string friendNickname, string friendUnionId, ITapTapRelationLiteRequestCallback callback)
        {
            TapLog.Log($"TapTapRelationLite SyncRelationshipWithUnionId with action: {action}, nickname: {nickname}, friendNickname: {friendNickname}, friendUnionId: {friendUnionId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("syncRelationshipWithUnionId")
                .Args("action", action)
                .Args("nickname", nickname)
                .Args("friendNickname", friendNickname)
                .Args("friendUnionId", friendUnionId)
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback.OnSyncRelationshipFail("request error", "", "");
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("success") && dic.ContainsKey("union_id"))
                        {
                            string unionId = SafeDictionary.GetValue<string>(dic, "union_id");
                            bool success = SafeDictionary.GetValue<bool>(dic, "success");
                            if (success)
                            {
                                callback.OnSyncRelationshipSuccess("", unionId);
                            }
                            else
                            {
                                string errorMessage = SafeDictionary.GetValue<string>(dic, "error_message");
                                callback.OnSyncRelationshipFail(errorMessage, "", unionId);
                            }
                        }
                        else
                        {
                            callback.OnSyncRelationshipFail("json format error", "", "");
                        }
                    }
                    catch (Exception e)
                    {
                        callback.OnSyncRelationshipFail(e.Message, "", "");
                        TapLog.Error($"SyncRelationshipWithOpenId parse result error: {e.Message}");
                    }
                });

        }

        public void ShowTapUserProfile(string openId, string unionId)
        {
            TapLog.Log($"TapTapRelationLite ShowTapUserProfile with openId: {openId}, unionId: {unionId}");

            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("showTapUserProfile")
                .Args("openId", openId)
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
                var command = new Command.Builder();
                command.Service(SERVICE_NAME);
                command.Method("unregisterRelationLiteCallback")
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

                });
                TapLog.Log("TapTapRelationLite UnregisterRelationLiteCallback");
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
    }
}