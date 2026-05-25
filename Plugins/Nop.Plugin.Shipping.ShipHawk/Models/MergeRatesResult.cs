using Nop.Core.Domain.Shipping;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Represents the result of merging rates from multiple warehouses
    /// </summary>
    public class MergeRatesResult
    {
        public List<ShippingOption> ShippingOptions { get; set; } = new List<ShippingOption>();
        public Dictionary<string, ShipHawkRateDetail> RateDetails { get; set; } = new Dictionary<string, ShipHawkRateDetail>();
        public bool ShippingFromMultipleLocations { get; set; }
    }
}