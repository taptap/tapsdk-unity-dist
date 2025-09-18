using System;
using System.Threading.Tasks;
using TapSDK.Core;

namespace TapSDK.RelationLite
{
    public interface ITapTapRelationLite
    {
        void Init(string clientId, TapTapRegionType regionType);

        void InviteGame();

        void InviteTeam(string teamId);

        Task<RelationLiteUserResult> GetFriendsList(string nextPageToken);

        Task<RelationLiteUserResult> GetFollowingList(string nextPageToken);

        Task<RelationLiteUserResult> GetFansList(string nextPageToken);

        Task SyncRelationshipWithOpenId(int action, string nickname,string friendNickname, string friendOpenId);

        Task SyncRelationshipWithUnionId(int action, string nickname,string friendNickname, string friendUnionId);

        void ShowTapUserProfile(string openId, string unionId);
        
        void RegisterRelationLiteCallback(ITapTapRelationLiteCallback callback);

        void UnregisterRelationLiteCallback(ITapTapRelationLiteCallback callback);
    }
} 