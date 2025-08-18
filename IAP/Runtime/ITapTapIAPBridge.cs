using System.Collections.Generic;

namespace TapSDK.IAP
{
    public interface ITapTapIAPBridge
    {
        void Initialize(string clientId, string clientToken, int regionCode, bool enableLog, bool isRNDMode);

        void RetrieveProducts(string[] productIds, ITapTapIAPRetrieveProductCallback callback);

        void PurchaseProduct(string productId, string payload, ITapTapIAPPurchaseProductCallback callback);

        void FinishTransaction(string transactionId);

        void RestorePurchase(ITapTapIAPUnFinishPurchaseCallback callback);

    }

    public interface ITapTapIAPRetrieveProductCallback
    {
        void OnQuerySuccess(ProductDetail[] productList);

        void OnQueryFailed(int errorCode, string errorMsg);
    }


    public interface ITapTapIAPPurchaseProductCallback
    {
        void OnPurchaseSuccess(string productId, string receipt, string transactionId);

        void OnPurchaseFailed(string productId, int errorCode, string errorMsg);
    }

    public interface ITapTapIAPUnFinishPurchaseCallback
    {
        void OnFetchSuccess(TransactionInfo[] transactions);

        void OnFetchFailed(int errorCode, string errorMsg);
    }
}