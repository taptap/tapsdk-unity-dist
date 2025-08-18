namespace TapSDK.Relation
{
    public interface ITapTapRelationCallback
    {
        void OnMessengerCodeResult(int code);

        void OnNewFansCountChanged(int code, int newFansCount);

        void OnUnreadMessageCountChanged(int code, int unreadMsgCount);
    }
}