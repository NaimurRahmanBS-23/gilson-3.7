using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents a ShipHawk rate request
    /// </summary>
    public class RateRequest
    {
        [JsonProperty("items")]
        public List<RateItemRequest> Items { get; set; } = new List<RateItemRequest>();

        [JsonProperty("origin_address")]
        public ShipHawkAddress OriginAddress { get; set; }

        [JsonProperty("destination_address")]
        public ShipHawkAddress DestinationAddress { get; set; }

        [JsonProperty("apply_rules")]
        public bool ApplyRules { get; set; } = true;

        [JsonProperty("display_rate_detail")]
        public bool DisplayRateDetail { get; set; } = true;

        [JsonProperty("source_system")]
        public string SourceSystem { get; set; } = "nopcommerce";

        [JsonProperty("destination_accessorials")]
        public Dictionary<string, object> DestinationAccessorials { get; set; }

        /// <summary>
        /// Gets or sets the reference numbers (for tracking/order identification)
        /// Use for PO numbers, invoice numbers, and liftgate delivery references.
        /// Per Requirements Document Section 6.3: "reference_numbers[Liftgate Delivery] - T if Yes, F if No"
        /// </summary>
        [JsonProperty("reference_numbers")]
        public System.Collections.Generic.List<ReferenceNumber> ReferenceNumbers { get; set; }
    }
}