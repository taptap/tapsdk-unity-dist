using TapSDK.Review.Internal;

namespace TapSDK.Review
{
    public class TapTapReview
    {

        public static readonly string Version = "4.7.2-beta.0";

        public static void OpenReview()
        {
            TapTapReviewInner.OpenReview();
        }
    }
}
