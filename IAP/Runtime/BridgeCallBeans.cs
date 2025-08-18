
using System.Collections.Generic;

namespace TapSDK.IAP
{

    public class ApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<ProductDetail> Content { get; set; }
    }

    public class PurchaseApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public TransactionInfo Content { get; set; }
    }


    public class RestorePurchaseApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<TransactionInfo> Content { get; set; }
    }

    public class ProductDetail
    {
        //商品类型
        public string ProductType { get; set; }
        //商品ID
        public string ProductId { get; set; }
        //商品名称
        public string Name { get; set; }
        //商品价格
        public OneTimePurchaseOfferDetails OneTimePurchaseOfferDetails { get; set; }
        //商品描述
        public string Description { get; set; }
        //商品地区
        public string RegionId { get; set; }


    }

    public class OneTimePurchaseOfferDetails
    {
        public string FormatterPrice { get; set; }
        public long PriceAmountMicros { get; set; }
        public string PriceCurrencyCode { get; set; }

    }

    public class PurchaseInfo
    {

        //商品ID
        public string ProductId { get; set; }
        //交易Id
        public string TransactionId { get; set; }
        //收据
        public string Receipt { get; set; }


    }

    public class TransactionInfo
    {
        //商品ID
        public string ProductId { get; set; }
        //订单Id
        public string OrderId { get; set; }
        //token
        public string PurchaseToken { get; set; }
        public string ObfuscatedAccountId { get; set; }
        public string PurchaseState { get; set; }
        public string Quantity { get; set; }
        public string OrderToken { get; set; }
        public string Acknowledged { get; set; }
    }

}
