using System;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Moment.Internal;
using TapSDK.Moment;

namespace TapSDK.Moment.Standalone
{
    public class TapMomentStandalone : ITapTapMomentPlatform
    {

        public void OpenMoment()
        {
            TapLogger.Warn($"{nameof(OpenMoment)} NOT implemented.");
        }

        public void OpenScene(string sceneId)
        {
            TapLogger.Warn($"{nameof(OpenScene)} NOT implemented.");
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            TapLogger.Warn($"{nameof(Init)} NOT implemented.");
        }

        public void Close()
        {
            TapLogger.Warn($"{nameof(Close)} NOT implemented.");
        }

        public void CloseWithConfirmWindow(string title, string content)
        {
            TapLogger.Warn($"{nameof(CloseWithConfirmWindow)} NOT implemented.");
        }

        public void FetchNotification()
        {
            TapLogger.Warn($"{nameof(FetchNotification)} NOT implemented.");
        }


        public void Publish(PublishMetaData publishMetaData)
        {
            TapLogger.Warn($"{nameof(Publish)} NOT implemented.");
        }

        public void SetCallback(Action<int, string> callback)
        {
            TapLogger.Warn($"{nameof(SetCallback)} NOT implemented.");
        }

        public void SetGameScreenAutoRotate(bool isAutoRotate)
        {
            TapLogger.Warn($"{nameof(SetGameScreenAutoRotate)} NOT implemented.");
        }
    }
}
