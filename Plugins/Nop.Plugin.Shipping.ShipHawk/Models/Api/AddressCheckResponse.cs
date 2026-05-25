using Newtonsoft.Json;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents the response from ShipHawk /api/v4/addresses/check endpoint
    /// </summary>
    public class AddressCheckResponse
    {
        [JsonProperty("corrected")]
        public bool Corrected { get; set; }

        [JsonProperty("deliverable")]
        public bool Deliverable { get; set; }

        [JsonProperty("address")]
        public AddressCheckResult Address { get; set; }
    }
}