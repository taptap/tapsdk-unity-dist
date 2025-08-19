using System;
using System.Collections.Generic;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Moment.Internal;
using TapSDK.Moment;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Moment.Standalone
{
    public class TapMomentStandalone : ITapTapMomentPlatform
    {

        public void OpenMoment()
        {
            TapLog.Warning($" Moment {nameof(OpenMoment)} NOT implemented.");
        }

        public void OpenScene(string sceneId)
        {
            TapLog.Warning($" Moment {nameof(OpenScene)} NOT implemented.");
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            TapLog.Warning($" Moment {nameof(Init)} NOT implemented.");
        }

        public void Close()
        {
            TapLog.Warning($" Moment {nameof(Close)} NOT implemented.");
        }

        public void CloseWithConfirmWindow(string title, string content)
        {
            TapLog.Warning($" Moment {nameof(CloseWithConfirmWindow)} NOT implemented.");
        }

        public void FetchNotification()
        {
            TapLog.Warning($" Moment {nameof(FetchNotification)} NOT implemented.");
        }


        public void Publish(PublishMetaData publishMetaData)
        {
            TapLog.Warning($" Moment {nameof(Publish)} NOT implemented.");
        }

        public void SetCallback(Action<int, string> callback)
        {
            TapLog.Warning($" Moment {nameof(SetCallback)} NOT implemented.");
        }

        public void SetGameScreenAutoRotate(bool isAutoRotate)
        {
            TapLog.Warning($" Moment {nameof(SetGameScreenAutoRotate)} NOT implemented.");
        }
    }
}
