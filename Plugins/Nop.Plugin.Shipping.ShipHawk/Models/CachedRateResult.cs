using Newtonsoft.Json;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Wrapper class for caching both response and rate details.
    /// Ensures RateDetails are preserved when cached response is returned (Phase 4 fix).
    /// Without this, Phase 3 (Order Notes) would fail because attributes were cleared at start of call.
    /// </summary>
    public class CachedRateResult
    {
        /// <summary>
        /// The shipping option response
        /// </summary>
        [JsonProperty("response")]
        public GetShippingOptionResponse Response { get; set; }

        /// <summary>
        /// Rate details dictionary keyed by sanitized service name
        /// Used to restore generic attributes on cache hit for order notes
        /// </summary>
        [JsonProperty("rateDetails")]
        public System.Collections.Generic.Dictionary<string, ShipHawkRateDetail> RateDetails { get; set; }
    }
}
