using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.Vertex
{
    // Root request for Vertex Calculate Tax
    public class VertexSaleRequest
    {
        // QUOTATION or INVOICE
        [JsonProperty("saleMessageType")]
        public string saleMessageType { get; set; }

        // SALE for seller side
        [JsonProperty("transactionType")]
        public string transactionType { get; set; }

        // yyyy-MM-dd
        [JsonProperty("documentDate")]
        public string documentDate { get; set; }

        [JsonProperty("documentNumber")]
        public string documentNumber { get; set; }

        // Optional block if you need to force the currency
        [JsonProperty("currency", NullValueHandling = NullValueHandling.Ignore)]
        public CurrencyBlock currency { get; set; }

        [JsonProperty("seller")]
        public Seller seller { get; set; }

        [JsonProperty("customer")]
        public CustomerBlock customer { get; set; }

        [JsonProperty("lineItems")]
        public List<LineItem> lineItems { get; set; }
    }

    public class CurrencyBlock
    {
        // Example USD
        [JsonProperty("isoCurrencyCodeAlpha")]
        public string isoCurrencyCodeAlpha { get; set; }
    }

    public class Seller
    {
        [JsonProperty("company")]
        public string company { get; set; }

        // Warehouse or ship from
        [JsonProperty("physicalOrigin")]
        public VxAddress physicalOrigin { get; set; }
    }

    public class CustomerBlock
    {
        [JsonProperty("customerCode")]
        public CustomerCode customerCode { get; set; }

        // Ship to
        [JsonProperty("destination")]
        public VxAddress destination { get; set; }
    }

    public class CustomerCode
    {
        // Often Taxable or Exempt, map from your logic if needed
        [JsonProperty("classCode")]
        public string classCode { get; set; }

        // Arbitrary customer identifier, safe to use CustomerGuid string
        [JsonProperty("value")]
        public string value { get; set; }
    }

    // Use VxAddress to avoid collisions with Nop.Core.Domain.Common.Address
    public class VxAddress
    {
        [JsonProperty("streetAddress1")]
        public string streetAddress1 { get; set; }

        [JsonProperty("streetAddress2", NullValueHandling = NullValueHandling.Ignore)]
        public string streetAddress2 { get; set; }

        [JsonProperty("city")]
        public string city { get; set; }

        // State or province two letter abbreviation for US, full name accepted by Vertex too
        [JsonProperty("mainDivision")]
        public string mainDivision { get; set; }

        [JsonProperty("postalCode")]
        public string postalCode { get; set; }

        // USA for US. For others use ISO alpha 2 or their expected literal
        [JsonProperty("country")]
        public string country { get; set; }
    }

    public class LineItem
    {
        // 1 based line number
        [JsonProperty("lineItemNumber")]
        public int lineItemNumber { get; set; }

        [JsonProperty("product")]
        public ProductBlock product { get; set; }

        [JsonProperty("quantity")]
        public Quantity quantity { get; set; }

        // Extended line price for this call, decimal currency amount
        [JsonProperty("extendedPrice")]
        public decimal extendedPrice { get; set; }        

        //public LineType lineType { get; set; } 
    }

    //public class LineType
    //{
    //    public string value { get; set; }
    //}

    public class ProductBlock
    {
        // For shipping use "Shipping". For items use your tax class such as STANDARD
        [JsonProperty("productClass")]
        public string productClass { get; set; }

        // SKU or any product code string
        [JsonProperty("value")]
        public string value { get; set; }
    }

    public class Quantity
    {
        [JsonProperty("unitOfMeasure")]
        public string unitOfMeasure { get; set; }

        [JsonProperty("value")]
        public decimal value { get; set; }
    }
}
