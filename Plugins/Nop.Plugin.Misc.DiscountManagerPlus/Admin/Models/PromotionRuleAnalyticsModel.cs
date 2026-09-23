using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleAnalyticsModel : BaseNopEntityModel
    {
        public PromotionRuleAnalyticsModel()
        {
            RuleName = string.Empty;
            RuleTypeName = string.Empty;
        }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleName")]
        public string RuleName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleType")]
        public string RuleTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.IsActive")]
        public bool IsActive { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.UsageCount")]
        public int UsageCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.ImpactedOrders")]
        public int ImpactedOrdersCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.ImpactedCustomers")]
        public int ImpactedCustomersCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.TotalDiscount")]
        public decimal TotalDiscountAmount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.TotalRevenue")]
        public decimal TotalRevenueAmount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.AverageDiscountPerOrder")]
        public decimal AverageDiscountPerOrder { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.OrderImpactRate")]
        public decimal OrderImpactRatePercent { get; set; }
    }
}
