using TapSDK.Review.Internal;

namespace TapSDK.Review
{
    public class TapTapReview
    {

        public static readonly string Version = "4.12.0-beta.1";

        public static void OpenReview()
        {
            TapTapReviewInner.OpenReview();
        }
    }
}
