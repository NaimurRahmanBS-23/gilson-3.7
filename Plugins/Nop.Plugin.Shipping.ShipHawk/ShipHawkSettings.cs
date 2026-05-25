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
    }
}