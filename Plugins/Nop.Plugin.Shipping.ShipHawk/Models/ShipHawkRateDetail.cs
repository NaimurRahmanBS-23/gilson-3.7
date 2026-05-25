namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Represents detailed rate information for order notes
    /// Stored as generic attribute and used in order processing
    /// Also used by OPC for liftgate visibility (HasFreightCarrier flag)
    /// </summary>
    public class ShipHawkRateDetail
    {
        /// <summary>
        /// Service name shown to customer
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Total rate (sum across all warehouses, before markup)
        /// </summary>
        public decimal TotalRate { get; set; }

        /// <summary>
        /// Breakdown by warehouse for order note
        /// </summary>
        public System.Collections.Generic.List<WarehouseRateBreakdown> WarehouseBreakdowns { get; set; } = new System.Collections.Generic.List<WarehouseRateBreakdown>();

        /// <summary>
        /// Indicates if any warehouse in breakdown uses freight/LTL carrier
        /// Used by OPC to determine liftgate section visibility for this specific shipping option
        /// </summary>
        public bool HasFreightCarrier { get; set; }
    }

    /// <summary>
    /// Represents rate breakdown for a single warehouse
    /// </summary>
    public class WarehouseRateBreakdown
    {
        /// <summary>
        /// Warehouse code (Name field)
        /// </summary>
        public string WarehouseCode { get; set; }

        /// <summary>
        /// Service name used from this warehouse
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Rate from this warehouse (before markup)
        /// </summary>
        public decimal Rate { get; set; }
    }
}