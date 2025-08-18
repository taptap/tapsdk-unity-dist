using TapSDK.Core.Internal.Utils;

namespace TapSDK.Review.Internal
{
    internal class TapTapReviewInner
    {

        static readonly ITapReviewBridge reviewBridge;

        static TapTapReviewInner()
        {
            reviewBridge = BridgeUtils.CreateBridgeImplementation(typeof(ITapReviewBridge), "TapSDK.Review")
                as ITapReviewBridge;
        }

        internal static void OpenReview()
        {
            if (reviewBridge == null)
            {
                return;
            }
            reviewBridge.OpenReview();
        }
    }
}