using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using TapSDK.Core.Internal.Utils;
using System.Linq;
using TapSDK.IAP;

namespace TapSDK.IAP

{
    public class TapTapStore : IStore, ITapTapStoreConfiguration
    {

        public TapTapStore()
        {
            payment = BridgeUtils.CreateBridgeImplementation(typeof(ITapTapIAPBridge), "TapSDK.IAP") as ITapTapIAPBridge;
        }

        private IStoreCallback _storeCallback;
        private string _clientId;
        private string _clientToken;
        private int _regionCode;
        private bool _isRNDMode;
        private bool _enableLog;

        private static ITapTapIAPBridge payment;

        public void Initialize(IStoreCallback callback)
        {
            _storeCallback = callback;
            payment.Initialize(_clientId, _clientToken, _regionCode, _enableLog, _isRNDMode);
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            string[] productIds = products.Select(item => item.id).ToArray();
            payment.RetrieveProducts(productIds, new ProductDetailsResponseListener(_storeCallback, payment));
        }

        public void Purchase(ProductDefinition product, string developerPayload211)
        {
            payment.PurchaseProduct(product.storeSpecificId, developerPayload211, new PurchasesUpdatedListener(_storeCallback));
        }

        public void FinishTransaction(ProductDefinition product, string transactionId)
        {
            payment.FinishTransaction(transactionId);
        }

        public void SetClientId(string clientId)
        {
            _clientId = clientId;
        }

        public void SetClientToken(string clientToken)
        {
            _clientToken = clientToken;
        }

        public void SetRegionCode(int regionCode)
        {
            _regionCode = regionCode;
        }

        public void SetEnableLog(bool enableLog)
        {
            _enableLog = enableLog;
        }

        public void SetIsRNDMode(bool isRNDMode)
        {
            _isRNDMode = isRNDMode;
        }

    }
}