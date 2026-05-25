using Newtonsoft.Json;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents the suggested address details from ShipHawk address check
    /// </summary>
    public class AddressCheckResult
    {
        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("zipcode")]
        public string ZipCode { get; set; }

        [JsonProperty("street1")]
        public string Street1 { get; set; }

        [JsonProperty("street2")]
        public string Street2 { get; set; }

        /// <summary>
        /// Location type classification from ShipHawk.
        /// IMPORTANT: This is a STRING field, not a boolean.
        /// Expected values: "residential", "business", or "commercial"
        /// </summary>
        [JsonProperty("location_type")]
        public string LocationType { get; set; }

        /// <summary>
        /// Helper property to determine residential status from location_type
        /// Returns true if location_type equals "residential" (case-insensitive)
        /// </summary>
        public bool IsResidential =>
            LocationType?.ToLowerInvariant() == "residential";

        /// <summary>
        /// Helper property to determine commercial/business status
        /// Returns true if location_type indicates a business address
        /// </summary>
        public bool IsCommercial =>
            LocationType?.ToLowerInvariant() == "business" ||
            LocationType?.ToLowerInvariant() == "commercial";
    }
}
