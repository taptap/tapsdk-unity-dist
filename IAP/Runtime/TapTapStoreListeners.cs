
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System.Linq;

namespace TapSDK.IAP

{
    public class ProductDetailsResponseListener : ITapTapIAPRetrieveProductCallback
    {
        private IStoreCallback _callback;
        private ITapTapIAPBridge _payment;

        public ProductDetailsResponseListener(IStoreCallback callback, ITapTapIAPBridge payment)
        {
            _callback = callback;
            _payment = payment;
        }

        public void OnQuerySuccess(ProductDetail[] productList)
        {
            if (productList == null)
            {
                Debug.LogError("productList is null");
                return;
            }

            var products = productList
                    .Where(p => p != null) // 过滤掉 null 的 ProductDetail 对象
                    .Select(p =>
                    {
                        // 提取属性
                        string formatPrice = p.OneTimePurchaseOfferDetails?.FormatterPrice;
                        string name = p.Name == null ? p.ProductId : p.Name;
                        string description = p.Description;
                        string priceCurrencyCode = p.OneTimePurchaseOfferDetails?.PriceCurrencyCode;
                        decimal priceAmountMicros = p.OneTimePurchaseOfferDetails?.PriceAmountMicros / 1000000m ?? 0;
                        string productId = p.ProductId;

                        // 检查属性是否有效
                        if (string.IsNullOrEmpty(formatPrice) || string.IsNullOrEmpty(priceCurrencyCode) || string.IsNullOrEmpty(productId))
                        {
                            Debug.LogWarning("Encountered ProductDetail with invalid properties, skipping.");
                            return null;
                        }

                        // 创建 ProductMetadata 和 ProductDescription 对象
                        ProductMetadata meta = new ProductMetadata(formatPrice, name, description, priceCurrencyCode, priceAmountMicros);
                        return new ProductDescription(productId, meta);
                    })
                    .Where(pd => pd != null) // 过滤掉 null 的 ProductDescription 对象
                    .ToList();

            if (_callback == null)
            {
                Debug.LogError("_callback is null");
                return;
            }
            _callback.OnProductsRetrieved(products);

            if (_payment == null)
            {
                Debug.LogError("_payment is null");
                return;
            }

            _payment.RestorePurchase(new RestorePurchaseListener(_callback));
        }

        public void OnQueryFailed(int errorCode, string errorMsg)
        {
            if (errorCode == 3)
            {
                _callback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable);
            }
            else if (errorCode == 4)
            {
                _callback.OnSetupFailed(InitializationFailureReason.NoProductsAvailable);
            }
            else
            {
                _callback.OnSetupFailed(InitializationFailureReason.AppNotKnown);
            }
        }
    }

    public class PurchasesUpdatedListener : ITapTapIAPPurchaseProductCallback
    {
        private IStoreCallback _callback;

        public PurchasesUpdatedListener(IStoreCallback callback)
        {
            _callback = callback;
        }

        public void OnPurchaseSuccess(string productId, string receipt, string transactionId)
        {
            _callback.OnPurchaseSucceeded(productId, receipt, transactionId);
        }

        public void OnPurchaseFailed(string productId, int errorCode, string errorMsg)
        {
            switch (errorCode)
            {
                case 1:
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(productId, PurchaseFailureReason.UserCancelled, errorMsg));
                    break;
                case 2:
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(productId, PurchaseFailureReason.PurchasingUnavailable, errorMsg));
                    break;
                case 4:
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(productId, PurchaseFailureReason.ProductUnavailable, errorMsg));
                    break;
                case 7:
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(productId, PurchaseFailureReason.ExistingPurchasePending, errorMsg));
                    break;
                default:
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(productId, PurchaseFailureReason.Unknown, errorMsg));
                    break;
            }

        }

    }

    public class RestorePurchaseListener : ITapTapIAPUnFinishPurchaseCallback
    {
        private IStoreCallback _callback;

        public RestorePurchaseListener(IStoreCallback callback)
        {
            _callback = callback;
        }

        void ITapTapIAPUnFinishPurchaseCallback.OnFetchSuccess(TransactionInfo[] transactions)
        {
            foreach (var transaction in transactions)
            {
                _callback.OnPurchaseSucceeded(transaction.ProductId, transaction.PurchaseToken, transaction.OrderId);
            }
        }

        void ITapTapIAPUnFinishPurchaseCallback.OnFetchFailed(int errorCode, string errorMsg)
        {
            throw new System.NotImplementedException();
        }
    }
}