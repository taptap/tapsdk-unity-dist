using System.Collections.Generic;
using TapSDK.Core;
using System;
using TapSDK.Review.Internal;

namespace TapSDK.Review.Mobile
{
    public class TapReviewBridge : ITapReviewBridge
    {
        public static string TAP_REVIEW_SERVICE = "BridgeReviewService";

        public static string TDS_REVIEW_SERVICE_CLZ = "com.taptap.sdk.review.unity.BridgeReviewService";

        public static string TDS_REVIEW_SERVICE_IMPL = "com.taptap.sdk.review.unity.BridgeReviewServiceImpl";

        public TapReviewBridge()
        {
            EngineBridge.GetInstance().Register(TDS_REVIEW_SERVICE_CLZ, TDS_REVIEW_SERVICE_IMPL);
        }

        public void OpenReview()
        {
#if UNITY_ANDROID
            EngineBridge.GetInstance().CallHandler(new Command.Builder()
                .Service(TAP_REVIEW_SERVICE)
                .Method("openReview")
                .Callback(false)
                .OnceTime(true)
                .CommandBuilder());
#else
            throw new NotImplementedException("TapReview::OpenReview Only Support On Android");
#endif
        }
    }
}