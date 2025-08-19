using TapSDK.Review.Internal;

namespace TapSDK.Review
{
    public class TapTapReview
    {

        public static readonly string Version = "4.8.1-beta.4";

        public static void OpenReview()
        {
            TapTapReviewInner.OpenReview();
        }
    }
}
