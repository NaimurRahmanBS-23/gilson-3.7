using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleAnalyticsSummaryModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalUsage")]
        public int TotalUsageCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.ImpactedOrders")]
        public int TotalImpactedOrdersCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.ImpactedCustomers")]
        public int TotalImpactedCustomersCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalDiscount")]
        public decimal TotalDiscountAmount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalRevenue")]
        public decimal TotalRevenueAmount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalOrders")]
        public int TotalOrdersCount { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Summary.OrderImpactRate")]
        public decimal OrderImpactRatePercent { get; set; }
    }
}
