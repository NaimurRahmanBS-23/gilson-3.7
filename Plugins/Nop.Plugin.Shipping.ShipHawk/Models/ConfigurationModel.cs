using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Represents the ShipHawk plugin configuration model
    /// </summary>
    public class ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            AvailableLiftGateCheckoutAttributes = new List<SelectListItem>();
        }

        public string ApiKey { get; set; }

        public bool UseSandbox { get; set; }

        public string ApiUrl { get; set; }

        public int RequestTimeout { get; set; }

        public string DefaultWarehouseCode { get; set; }

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

        public bool Tracing { get; set; }

        public string MultiWarehouseMessage { get; set; }

        public string FreightMessage { get; set; }

        public List<SelectListItem> AvailableLiftGateCheckoutAttributes { get; set; }
    }
}