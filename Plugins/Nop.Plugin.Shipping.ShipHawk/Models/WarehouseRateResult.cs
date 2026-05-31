using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Represents rate results from a single warehouse
    /// </summary>
    public class WarehouseRateResult
    {
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public List<WarehouseRate> Rates { get; set; } = new List<WarehouseRate>();

        /// <summary>
        /// SKU items that were included in this warehouse rate request.
        /// Used for SKU-level order note breakdown for NetSuite.
        /// </summary>
        public List<SkuItemInfo> SkuItems { get; set; } = new List<SkuItemInfo>();

        /// <summary>
        /// Debug information captured during parallel rate request.
        /// Logged AFTER Task.WaitAll completes (sequential context).
        /// </summary>
        public RateRequestDebugInfo DebugInfo { get; set; }
    }

    /// <summary>
    /// Represents a single rate from a warehouse
    /// </summary>
    public class WarehouseRate
    {
        public string ServiceName { get; set; }
        public string OriginalServiceName { get; set; }
        public string Carrier { get; set; }
        public decimal Price { get; set; }
        public string RateId { get; set; }
        public int? ServiceDays { get; set; }
    }
}