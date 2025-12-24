using System;
using System.Collections.Generic;
using TapSDK.Relation.Internal;
using TapSDK.Core;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Relation
{
    public class TapTapRelationConstants
    {
        //
        public static readonly int CALLBACK_CODE_ON_STOP = 500;
    }

    public class TapTapRelation
    {

        public static void Init(string clientId, TapTapRegionType regionType, int screenOrientation)
        {
            TapTapRelationManager.Instance.Init(clientId, regionType, screenOrientation);
        }

        public static void StartMessenger()
        {
            TapTapRelationManager.Instance.StartMessenger();
        }

        public static void Prepare()
        {
            TapTapRelationManager.Instance.Prepare();
        }

        public static void InviteGame()
        {
            TapTapRelationManager.Instance.InviteGame();
        }

        public static void InviteTeam(string teamId)
        {
            TapTapRelationManager.Instance.InviteTeam(teamId);
        }

        public static void ShowTapUserProfile(string openId, string unionId)
        {
            TapTapRelationManager.Instance.ShowTapUserProfile(openId, unionId);
        }

        public static void GetNewFansCount(Action<int> callback)
        {
            TapTapRelationManager.Instance.GetNewFansCount(callback);
        }

        public static void GetUnreadMessageCount(Action<int> callback)
        {
            TapTapRelationManager.Instance.GetUnreadMessageCount(callback);
        }

        public static void RegisterRelationCallback(ITapTapRelationCallback callback)
        {
            TapTapRelationManager.Instance.RegisterRelationCallback(callback);
        }

        public static void UnregisterRelationCallback(ITapTapRelationCallback callback)
        {
            TapTapRelationManager.Instance.UnregisterRelationCallback(callback);
        }

        public static void Destroy()
        {
            TapTapRelationManager.Instance.Destroy();
        }


        public static readonly string Version = "4.10.0-beta.1";


    }
}
