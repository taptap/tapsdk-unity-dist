using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.RelationLite.Internal
{
    public class TapTapRelationLiteManager
    {
        private ITapTapRelationLite _impl;
        private List<ITapTapRelationLiteCallback> _relationCallbacks;

        private static TapTapRelationLiteManager instance;

        private TapTapRelationLiteManager()
        {
            _impl = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapRelationLite),
                "TapSDK.RelationLite") as ITapTapRelationLite;
            _relationCallbacks = new List<ITapTapRelationLiteCallback>();
        }

        public static TapTapRelationLiteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapTapRelationLiteManager();
                }
                return instance;
            }
        }

        public void Init(string clientId, TapTapRegionType regionType)
        {
            _impl?.Init(clientId, regionType);
        }

        public void InviteGame()
        {
            _impl?.InviteGame();
        }

        public void InviteTeam(string teamId)
        {
            _impl?.InviteTeam(teamId);
        }

        public Task<RelationLiteUserResult> GetFriendsList(string nextPageToken)
        {
           return _impl?.GetFriendsList(nextPageToken);
        }

        public Task<RelationLiteUserResult> GetFollowingList(string nextPageToken)
        {
            return _impl?.GetFollowingList(nextPageToken);
        }

        public Task<RelationLiteUserResult> GetFansList(string nextPageToken)
        {
            return _impl?.GetFansList(nextPageToken);
        }

        public Task SyncRelationshipWithOpenId(int action, string nickname, string friendNickname, string friendOpenId)
        {
            return _impl?.SyncRelationshipWithOpenId(action, nickname, friendNickname, friendOpenId);
        }

        public Task SyncRelationshipWithUnionId(int action, string nickname, string friendNickname, string friendUnionId)
        {
            return _impl?.SyncRelationshipWithUnionId(action, nickname, friendNickname, friendUnionId);
        }

        public void ShowTapUserProfile(string openId, string unionId)
        {
            _impl?.ShowTapUserProfile(openId, unionId);
        }


        public void RegisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            if (callback != null && !_relationCallbacks.Contains(callback))
            {
                _relationCallbacks.Add(callback);
                _impl?.RegisterRelationLiteCallback(callback);
            }
        }

        public void UnregisterRelationLiteCallback(ITapTapRelationLiteCallback callback)
        {
            if (callback != null && _relationCallbacks.Contains(callback))
            {
                _relationCallbacks.Remove(callback);
                _impl?.UnregisterRelationLiteCallback(callback);
            }
        }
    }
} 