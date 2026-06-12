namespace TapSDK.Relation
{
    public interface ITapTapRelationInviteCallback
    {
        void OnGameInviteReceived(string openId, string unionId);

        void OnTeamInviteReceived(string openId, string unionId, string teamId);
    }
}
