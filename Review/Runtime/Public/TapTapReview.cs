using TapSDK.Review.Internal;

namespace TapSDK.Review
{
    public class TapTapReview
    {

        public static readonly string Version = "4.10.0-beta.5";

        public static void OpenReview()
        {
            TapTapReviewInner.OpenReview();
        }
    }
}
