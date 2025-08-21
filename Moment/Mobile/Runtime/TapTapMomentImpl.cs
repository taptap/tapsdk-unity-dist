using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Moment.Internal;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace TapSDK.Moment.Mobile
{
    public class TapTapMomentImpl : ITapTapMomentPlatform
    {
        private const string SERVICE_NAME = "BridgeMomentService";

        public TapTapMomentImpl(){
            EngineBridge.GetInstance().Register(
                "com.taptap.sdk.moment.unity.BridgeMomentService",
                "com.taptap.sdk.moment.unity.BridgeMomentServiceImpl");
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            
#if UNITY_IOS
            if (Platform.IsIOS())
            {
                if (Device.deferSystemGesturesMode != SystemGestureDeferMode.None)
                {
                    NeedDeferSystemGestures();
                }
            }
#endif
        }

        public void Close()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("close")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void CloseWithConfirmWindow(string title, string content)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("closeWithConfirmWindow")
                .Args("title", title)
                .Args("content", content)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void OpenMoment()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("openMoment")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void OpenScene(string sceneId)
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("openScene")
                .Args("sceneId", sceneId)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void Publish(PublishMetaData publishMetaData)
        {
            string json = JsonConvert.SerializeObject(publishMetaData);
            Debug.Log("TapSdk4UnityDemo -->> PublishMetaData json = " + json);
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("publish")
                .Args("publishMetaData", json)
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void FetchNotification()
        {
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(SERVICE_NAME)
                .Method("fetchNotification")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
        }

        public void SetCallback(Action<int, string> callback)
        {

            var command = new Command.Builder();
            command.Service(SERVICE_NAME);
            command.Method("setCallback")
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
                var dic = Json.Deserialize(result.content) as Dictionary<string, object>;
                var code = SafeDictionary.GetValue<int>(dic, "code");
                var msg = SafeDictionary.GetValue<string>(dic, "msg");
                // 
                Debug.Log("TapSdk4UnityDemo -->> Callback code = " + code + " , msg = " + msg);
                callback(code, msg);
            });
        }

        public void NeedDeferSystemGestures()
        {
            if (Platform.IsIOS())
            {
                EngineBridge.GetInstance().CallHandler(new Command.Builder()
                    .Service(SERVICE_NAME)
                    .Method("needDeferSystemGestures")
                    .Callback(false)
                    .OnceTime(true)
                    .CommandBuilder());
            }
        }
    }
}