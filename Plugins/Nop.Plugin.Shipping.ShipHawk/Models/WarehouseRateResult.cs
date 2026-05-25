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