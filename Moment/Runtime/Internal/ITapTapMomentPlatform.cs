using System.Threading.Tasks;
using TapSDK.Core;
using System;
using System.Collections.Generic;

namespace TapSDK.Moment.Internal
{
    public interface ITapTapMomentPlatform
    {
        void Init(string clientId, TapTapRegionType regionType);

        void Close();
        
        void CloseWithConfirmWindow(String title, String content);

        void OpenMoment();

        void OpenScene(string sceneId);

        void Publish(PublishMetaData publishMetaData);

        void SetCallback(Action<int, string> callback);

        void FetchNotification();
    }
}