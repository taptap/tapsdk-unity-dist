using System;
using TapSDK.Core;

namespace TapSDK.Relation
{
    public interface ITapTapRelation
    {
        void Init(string clientId, TapTapRegionType regionType, int orientation);

        void StartMessenger();

        void Prepare();

        void InviteGame();

        void InviteTeam(string teamId);

        void ShowTapUserProfile(string openId, string unionId);
        
        void GetNewFansCount(Action<int> callback);

        void GetUnreadMessageCount(Action<int> callback);

        void RegisterRelationCallback(ITapTapRelationCallback callback);

        void UnregisterRelationCallback(ITapTapRelationCallback callback);

        void Destroy();
    }
}