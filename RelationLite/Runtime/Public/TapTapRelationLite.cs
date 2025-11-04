using System;
using System.Collections.Generic;
using TapSDK.RelationLite.Internal;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;
using System.Threading.Tasks;

namespace TapSDK.RelationLite
{
    public class TapTapRelationLiteConstants
    {
        public static readonly int CALLBACK_CODE_ON_STOP = 500;
        
        // Relation action constants
        public static readonly int ACTION_FOLLOW = 1;
        public static readonly int ACTION_UNFOLLOW = 2;
    }

    public class TapTapRelationLite
    {
        public static void Init(string clientId, TapTapRegionType regionType)
        {
            TapTapRelationLiteManager.Instance.Init(clientId, regionType);
        }

        public static void InviteGame()
        {
            TapTapRelationLiteManager.Instance.InviteGame();
        }

        public static void InviteTeam(string teamId)
        {
            TapTapRelationLiteManager.Instance.InviteTeam(teamId);
        }

        public static Task<RelationLiteUserResult> GetFriendsList(string nextPageToken)
        {
           return TapTapRelationLiteManager.Instance.GetFriendsList(nextPageToken);
        }

        public static Task<RelationLiteUserResult> GetFollowingList(string nextPageToken)
        {
            return TapTapRelationLiteManager.Instance.GetFollowingList(nextPageToken);
        }

        public static Task<RelationLiteUserResult> GetFansList(string nextPageToken)
        {
            return TapTapRelationLiteManager.Instance.GetFansList(nextPageToken);
        }

        public static Task SyncRelationshipWithOpenId(int action, string nickname, string friendNickname, string friendOpenId)
        {
           return TapTapRelationLiteManager.Instance.SyncRelationshipWithOpenId(action, nickname, friendNickname, friendOpenId);
        }

        public static Task SyncRelationshipWithUnionId(int action, string nickname, string friendNickname, string friendUnionId)
        {
            return TapTapRelationLiteManager.Instance.SyncRelationshipWithUnionId(action, nickname, friendNickname, friendUnionId);
        }

        public static void ShowTapUserProfile(string openId, string unionId)
        {
            TapTapRelationLiteManager.Instance.ShowTapUserProfile(openId, unionId);
        }

        public static void RegisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            TapTapRelationLiteManager.Instance.RegisterRelationLiteCallback(callback);
        }

        public static void UnregisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            TapTapRelationLiteManager.Instance.UnregisterRelationLiteCallback(callback);
        }

        public static readonly string Version = "4.8.4";
    }
} 