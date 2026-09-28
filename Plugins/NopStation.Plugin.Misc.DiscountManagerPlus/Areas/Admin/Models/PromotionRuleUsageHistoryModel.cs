using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleUsageHistoryModel : BaseNopEntityModel
{
    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Order")]
    public int OrderId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Order")]
    public string CustomOrderNumber { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Customer")]
    public string CustomerEmail { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.OrderTotal")]
    public string OrderTotal { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Discount")]
    public string DiscountAmountApplied { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Source")]
    public string HistorySource { get; set; } = string.Empty;
}
