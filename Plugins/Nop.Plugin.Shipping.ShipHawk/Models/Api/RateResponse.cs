using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models.Api
{
    /// <summary>
    /// Represents a ShipHawk rate response
    /// </summary>
    public class RateResponse
    {
        [JsonProperty("rates")]
        public List<Rate> Rates { get; set; } = new List<Rate>();

        [JsonProperty("errors")]
        public List<ErrorDetail> Errors { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    /// <summary>
    /// Represents an error detail from ShipHawk API
    /// </summary>
    public class ErrorDetail
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("details")]
        public object Details { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Message))
                return Message;
            if (!string.IsNullOrEmpty(Code))
                return Code;
            return "Unknown error";
        }
    }

    /// <summary>
    /// Represents an individual rate option from ShipHawk
    /// </summary>
    public class Rate
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("carrier")]
        public string Carrier { get; set; }

        [JsonProperty("carrier_code")]
        public string CarrierCode { get; set; }

        [JsonProperty("service_level")]
        public string ServiceLevel { get; set; }

        [JsonProperty("rate_display_name")]
        public string RateDisplayName { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("service_days")]
        public int? ServiceDays { get; set; }
    }
}