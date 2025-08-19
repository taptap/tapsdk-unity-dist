using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Relation;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using TapSDK.Core.Internal.Log;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace TapSDK.Relation.Mobile
{
    public class TapTapRelationImpl : ITapTapRelation
    {
        private const string SERVICE_NAME = "BridgeRelationService";
        private static List<ITapTapRelationCallback> callbacks = new List<ITapTapRelationCallback>();
        private static bool hasRegisterCallBack = false;

        public TapTapRelationImpl()
        {
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.relation.unity.BridgeRelationService",
                "com.taptap.sdk.relation.unity.BridgeRelationServiceImpl");
        }

        public void Init(string clientId, TapTapRegionType regionType, int screenOrientation)
        {

        }

        public void StartMessenger()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("startMessenger")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void Prepare()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("prepare")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void InviteGame()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("inviteGame")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void InviteTeam(string teamId)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("inviteTeam")
                .Args("teamId", teamId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void ShowTapUserProfile(string openId, string unionId)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("showTapUserProfile")
                .Args("openId", openId)
                .Args("unionId", unionId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void GetNewFansCount(Action<int> callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getNewFansCount")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback(0);
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("new_fans_count"))
                        {
                            int fansCount = SafeDictionary.GetValue<int>(dic, "new_fans_count");
                            callback(fansCount);
                        }
                        else
                        {
                            callback(0);
                        }
                    }
                    catch (Exception e)
                    {
                        TapLog.Error($"GetNewFansCount parse result error: {e.Message}");
                        callback(0);
                    }
                });
        }

        public void GetUnreadMessageCount(Action<int> callback)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("getUnreadMessageCount")
                .Callback(true)
                .OnceTime(true)
                .CommandBuilder(), (result) =>
                {
                    if (callback == null) return;

                    try
                    {
                        if (result.code != Result.RESULT_SUCCESS || string.IsNullOrEmpty(result.content))
                        {
                            callback(0);
                            return;
                        }

                        var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                        if (dic != null && dic.ContainsKey("unread_message_count"))
                        {
                            int unreadCount = SafeDictionary.GetValue<int>(dic, "unread_message_count");
                            callback(unreadCount);
                        }
                        else
                        {
                            callback(0);
                        }
                    }
                    catch (Exception e)
                    {
                        TapLog.Error($"GetUnreadMessageCount parse result error: {e.Message}");
                        callback(0);
                    }
                });
        }

        public void RegisterRelationCallback(ITapTapRelationCallback callback)
        {
            if (callback == null) return;
            InitRegisterCallBack();
            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }

        public void UnregisterRelationCallback(ITapTapRelationCallback callback)
        {
            if (callback != null)
            {
                callbacks.Remove(callback);
            }
        }

        public void Destroy()
        {
            callbacks.Clear();
            hasRegisterCallBack = false;
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("destroy")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
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
            command.Method("registerRelationCallback")
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
                TapLog.Log("Relation -->> Bridge Callback == " + JsonConvert.SerializeObject(result));
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var code = SafeDictionary.GetValue<int>(dic, "relation_code");
                var newFansCount = SafeDictionary.GetValue<int>(dic, "new_fans_count");
                var unreadMessageCount = SafeDictionary.GetValue<int>(dic, "unread_message_count");

                if (code != null)
                {
                    callbacks.ForEach((x) => x.OnMessengerCodeResult(code));
                }

                if (newFansCount != null)
                {
                    callbacks.ForEach((x) => x.OnNewFansCountChanged(code, newFansCount));
                }

                if (unreadMessageCount != null)
                {
                    callbacks.ForEach((x) => x.OnUnreadMessageCountChanged(code, unreadMessageCount));
                }

            });
        }

    }
}