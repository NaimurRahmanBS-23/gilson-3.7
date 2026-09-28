using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleAnalyticsModel : BaseNopEntityModel
{
    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleName")]
    public string RuleName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleType")]
    public string RuleTypeName { get; set; } = string.Empty;

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
