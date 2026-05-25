using Newtonsoft.Json;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents a reference number in a ShipHawk rate request
    /// </summary>
    public class ReferenceNumber
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "other_id";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}