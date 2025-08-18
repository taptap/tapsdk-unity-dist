using System;
using System.Collections;
using System.Collections.Generic;

namespace TapSDK.RelationLite
{
    public interface ITapTapRelationLiteRequestCallback
    {
        void OnFriendsListResult(string nextPageToken, List<RelationLiteUserItem> friendsList);
        void OnFollowingListResult(string nextPageToken, List<RelationLiteUserItem> followingList);
        void OnFansListResult(string nextPageToken, List<RelationLiteUserItem> fansList);
        void OnSyncRelationshipSuccess(string openId, string unionId);
        void OnSyncRelationshipFail(string errorMessage, string openId, string unionId);
        void OnRequestError(string errorMessage);
    }
}