using TapSDK.Review.Internal;

namespace TapSDK.Review
{
    public class TapTapReview
    {

        public static readonly string Version = "4.9.0";

        public static void OpenReview()
        {
            TapTapReviewInner.OpenReview();
        }
    }
}
