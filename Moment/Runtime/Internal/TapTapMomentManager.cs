using System;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Moment.Internal
{
    public class TapTapMomentManager
    {
        private static TapTapMomentManager instance;
        private ITapTapMomentPlatform platformWrapper;

        private TapTapMomentManager()
        {
            platformWrapper = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapMomentPlatform),
                "TapSDK.Moment") as ITapTapMomentPlatform;
        }

        public static TapTapMomentManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapMomentManager();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType) => platformWrapper.Init(clientId, regionType);

        public void OpenMoment()
        {
            platformWrapper.OpenMoment();
        }

        public void OpenScene(string sceneId)
        {
            platformWrapper.OpenScene(sceneId);
        }

        public void Publish(PublishMetaData publishMetaData)
        {
            platformWrapper.Publish(publishMetaData);
        }

        public void Close()
        {
            platformWrapper.Close();
        }

        public void CloseWithConfirmWindow(String title, String content)
        {
            platformWrapper.CloseWithConfirmWindow(title, content);
        }

        public void SetCallback(Action<int, string> callback)
        {
            platformWrapper.SetCallback(callback);
        }

        public void FetchNotification()
        {
            platformWrapper.FetchNotification();
        }
    }

}