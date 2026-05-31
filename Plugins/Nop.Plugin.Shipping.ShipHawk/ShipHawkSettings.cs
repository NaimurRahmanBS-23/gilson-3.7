using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.ShipHawk
{
    /// <summary>
    /// Represents settings of the ShipHawk shipping plugin
    /// </summary>
    public class ShipHawkSettings : ISettings
    {
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string DefaultWarehouseCode { get; set; }
        public int RequestTimeout { get; set; }
        public bool Tracing { get; set; }
        public bool UseSandbox { get; set; }
        public decimal MarkupPercentage { get; set; }
        public decimal MarkupFixedAmount { get; set; }
        public string ExcludedCarriers { get; set; }
        public int MaxShippingOptions { get; set; }
        public bool HideTransitTime { get; set; }
        public bool SortByPrice { get; set; }
        public bool ApplyRules { get; set; }
        public string BlendedRateLabel { get; set; }
        public bool EnableCaching { get; set; }
        public int CacheDurationMinutes { get; set; }
        public int? LiftGateCheckoutAttributeId { get; set; }

        /// <summary>
        /// Message shown for Gilson Best (Scenario B) option when shipping from multiple warehouses.
        /// If empty/null, falls back to breakdown HTML showing warehouse/service/rate details.
        /// Example: "Your shopping cart contains items that ship from multiple locations. These items will arrive separately."
        /// </summary>
        public string MultiWarehouseMessage { get; set; }

        /// <summary>
        /// Additional message appended when freight/LTL carrier is detected for Gilson Best.
        /// Only shown if this setting has a value AND freight carrier is in the shipment.
        /// IMPORTANT: Must contain "freight" or "ltl" keyword for OPC liftgate detection to work.
        /// Example: "Some of the items are large in size and/or weight and require motor freight."
        /// </summary>
        public string FreightMessage { get; set; }
    }
}