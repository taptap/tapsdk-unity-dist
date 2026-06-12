namespace TapSDK.RelationLite
{
    public interface ITapTapRelationLiteInviteCallback
    {
        void OnGameInviteReceived(string openId, string unionId);

        void OnTeamInviteReceived(string openId, string unionId, string teamId);
    }
}
