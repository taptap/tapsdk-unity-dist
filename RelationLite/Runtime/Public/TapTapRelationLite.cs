using System;
using System.Collections.Generic;
using TapSDK.RelationLite.Internal;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

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

        public static void GetFriendsList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapTapRelationLiteManager.Instance.GetFriendsList(nextPageToken, callback);
        }

        public static void GetFollowingList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapTapRelationLiteManager.Instance.GetFollowingList(nextPageToken, callback);
        }

        public static void GetFansList(string nextPageToken, ITapTapRelationLiteRequestCallback callback)
        {
            TapTapRelationLiteManager.Instance.GetFansList(nextPageToken, callback);
        }

        public static void SyncRelationshipWithOpenId(int action, string nickname, string friendNickname, string friendOpenId, ITapTapRelationLiteRequestCallback callback)
        {
            TapTapRelationLiteManager.Instance.SyncRelationshipWithOpenId(action, nickname, friendNickname, friendOpenId, callback);
        }

        public static void SyncRelationshipWithUnionId(int action, string nickname, string friendNickname, string friendUnionId, ITapTapRelationLiteRequestCallback callback)
        {
            TapTapRelationLiteManager.Instance.SyncRelationshipWithUnionId(action, nickname, friendNickname, friendUnionId, callback);
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

        public static readonly string Version = "4.7.2-beta.2";
    }
} 