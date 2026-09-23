using System;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleUsageHistoryModel : BaseNopEntityModel
    {
        public PromotionRuleUsageHistoryModel()
        {
            CustomOrderNumber = string.Empty;
            CustomerEmail = string.Empty;
            OrderTotal = string.Empty;
            DiscountAmountApplied = string.Empty;
            HistorySource = string.Empty;
        }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Order")]
        public int OrderId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Order")]
        public string CustomOrderNumber { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Customer")]
        public string CustomerEmail { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.OrderTotal")]
        public string OrderTotal { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Discount")]
        public string DiscountAmountApplied { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Source")]
        public string HistorySource { get; set; }
    }
}
