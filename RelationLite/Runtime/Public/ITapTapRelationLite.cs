using System;
using TapSDK.Core;

namespace TapSDK.RelationLite
{
    public interface ITapTapRelationLite
    {
        void Init(string clientId, TapTapRegionType regionType);

        void InviteGame();

        void InviteTeam(string teamId);

        void GetFriendsList(string nextPageToken, ITapTapRelationLiteRequestCallback callback);

        void GetFollowingList(string nextPageToken, ITapTapRelationLiteRequestCallback callback);

        void GetFansList(string nextPageToken, ITapTapRelationLiteRequestCallback callback);

        void SyncRelationshipWithOpenId(int action, string nickname,string friendNickname, string friendOpenId, ITapTapRelationLiteRequestCallback callback);

        void SyncRelationshipWithUnionId(int action, string nickname,string friendNickname, string friendUnionId, ITapTapRelationLiteRequestCallback callback);

        void ShowTapUserProfile(string openId, string unionId);
        
        void RegisterRelationLiteCallback(ITapTapRelationLiteCallback callback);

        void UnregisterRelationLiteCallback(ITapTapRelationLiteCallback callback);
    }
} 