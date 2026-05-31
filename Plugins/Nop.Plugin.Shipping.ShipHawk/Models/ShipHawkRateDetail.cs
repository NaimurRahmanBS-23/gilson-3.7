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
        /// Breakdown by warehouse for order note (existing format)
        /// </summary>
        public System.Collections.Generic.List<WarehouseRateBreakdown> WarehouseBreakdowns { get; set; } = new System.Collections.Generic.List<WarehouseRateBreakdown>();

        /// <summary>
        /// SKU-level breakdown by warehouse for NetSuite order recreation
        /// Shows: SKU, Warehouse, Carrier, Service, Rate per warehouse shipment
        /// </summary>
        public System.Collections.Generic.List<SkuWarehouseBreakdown> SkuWarehouseBreakdowns { get; set; } = new System.Collections.Generic.List<SkuWarehouseBreakdown>();

        /// <summary>
        /// Indicates if any warehouse in breakdown uses freight/LTL carrier
        /// Used by OPC to determine liftgate section visibility for this specific shipping option
        /// </summary>
        public bool HasFreightCarrier { get; set; }
    }

    /// <summary>
    /// Represents rate breakdown for a single warehouse (existing)
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

    /// <summary>
    /// Represents SKU-level breakdown for a single warehouse shipment
    /// Used for NetSuite order recreation - shows which SKUs shipped from which warehouse
    /// with which carrier/service and the shared warehouse rate
    /// </summary>
    public class SkuWarehouseBreakdown
    {
        /// <summary>
        /// Warehouse code (Name field)
        /// </summary>
        public string WarehouseCode { get; set; }

        /// <summary>
        /// Carrier name from ShipHawk (e.g., "UPS Freight", "FedEx")
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Service name shown to customer (e.g., "XPO Freight LTL", "FedEx Ground")
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Rate for this warehouse shipment (shared across all SKUs in this warehouse)
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// List of SKUs that shipped from this warehouse
        /// </summary>
        public System.Collections.Generic.List<SkuItemInfo> Skus { get; set; } = new System.Collections.Generic.List<SkuItemInfo>();
    }

    /// <summary>
    /// Represents SKU item information for order note
    /// </summary>
    public class SkuItemInfo
    {
        /// <summary>
        /// Product SKU
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Quantity ordered
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product weight in lbs
        /// </summary>
        public decimal Weight { get; set; }
    }
}