using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents an item in a ShipHawk rate request
    /// </summary>
    public class RateItemRequest
    {
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("weight")]
        public decimal Weight { get; set; }

        [JsonProperty("weight_uom")]
        public string WeightUom { get; set; } = "lb";

        [JsonProperty("length")]
        public decimal? Length { get; set; }

        [JsonProperty("width")]
        public decimal? Width { get; set; }

        [JsonProperty("height")]
        public decimal? Height { get; set; }

        [JsonProperty("dimension_uom")]
        public string DimensionUom { get; set; }

        [JsonProperty("product_sku")]
        public string ProductSku { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("warehouse_code")]
        public string WarehouseCode { get; set; }
    }
}