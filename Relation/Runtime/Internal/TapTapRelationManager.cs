using System;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Relation.Internal
{
    public class TapTapRelationManager
    {
        private static TapTapRelationManager instance;
        private ITapTapRelation platformWrapper;

        private TapTapRelationManager()
        {
            platformWrapper = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapRelation),
                "TapSDK.Relation") as ITapTapRelation;
        }

        public static TapTapRelationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapRelationManager();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType, int screenOrientation)
        {
            platformWrapper?.Init(clientId, regionType, screenOrientation);
        }

        public void StartMessenger()
        {
            platformWrapper?.StartMessenger();
        }

        public void Prepare()
        {
            platformWrapper?.Prepare();
        }

        public void InviteGame()
        {
            platformWrapper?.InviteGame();
        }

        public void InviteTeam(string teamId)
        {
            platformWrapper?.InviteTeam(teamId);
        }

        public void ShowTapUserProfile(string openId, string unionId)
        {
            platformWrapper?.ShowTapUserProfile(openId, unionId);
        }

        public void GetNewFansCount(Action<int> callback)
        {
            platformWrapper?.GetNewFansCount(callback);
        }

        public void GetUnreadMessageCount(Action<int> callback)
        {
            platformWrapper?.GetUnreadMessageCount(callback);
        }

        public void RegisterRelationCallback(ITapTapRelationCallback callback)
        {
            platformWrapper?.RegisterRelationCallback(callback);
        }

        public void UnregisterRelationCallback(ITapTapRelationCallback callback)
        {
            platformWrapper?.UnregisterRelationCallback(callback);
        }

        public void Destroy()
        {
            platformWrapper?.Destroy();
        }
    }

}