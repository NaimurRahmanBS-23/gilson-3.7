using System.Collections.Generic;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Validators;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleTierSearchModel : BaseNopModel
    {
        public PromotionRuleTierSearchModel()
        {
            PageIndex = 0;
            PageSize = 15;
        }

        public int PromotionRuleId { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    [Validator(typeof(PromotionRuleTierValidator))]
    public partial class PromotionRuleTierModel : BaseNopEntityModel
    {
        public PromotionRuleTierModel()
        {
            DiscountTypeName = string.Empty;
            RewardProductName = string.Empty;
            SelectedRuleProductIds = new List<int>();
            AppliesToRuleProductsSummary = string.Empty;
            AvailableDiscountTypes = new List<SelectListItem>();
            AvailableProducts = new List<SelectListItem>();
            AvailableRuleProducts = new List<SelectListItem>();
        }

        public int PromotionRuleId { get; set; }
        public int RuleTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity")]
        public int MinQuantity { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity")]
        public int MaxQuantity { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity")]
        public int RewardQuantity { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType")]
        public int DiscountTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType")]
        public string DiscountTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue")]
        public decimal DiscountValue { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward")]
        public bool AutoAddReward { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct")]
        public int? RewardProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct")]
        public string RewardProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts")]
        public IList<int> SelectedRuleProductIds { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts")]
        public string AppliesToRuleProductsSummary { get; set; }

        public IList<SelectListItem> AvailableDiscountTypes { get; set; }
        public IList<SelectListItem> AvailableProducts { get; set; }
        public IList<SelectListItem> AvailableRuleProducts { get; set; }
    }
}
