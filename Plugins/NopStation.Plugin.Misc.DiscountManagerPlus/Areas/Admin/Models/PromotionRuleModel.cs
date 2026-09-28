using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleModel : BaseNopEntityModel, IStoreMappingSupportedModel
{
    public int DiscountId { get; set; }

    public string ParentDiscountName { get; set; } = string.Empty;
    public string ParentDiscountLimitationSummary { get; set; } = string.Empty;
    public string ParentDiscountMaximumDiscountAmountDisplay { get; set; } = string.Empty;
    public string ParentDiscountMaximumDiscountedQuantityDisplay { get; set; } = string.Empty;
    public bool ParentDiscountUsesNativeCustomerLimit { get; set; }

    public bool IsDiscountBound { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name")]
    public string Name { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName")]
    public string SystemName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType")]
    public int RuleTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType")]
    public string RuleTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType")]
    public int DiscountTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType")]
    public string DiscountTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope")]
    public int DiscountScopeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope")]
    public string DiscountScopeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue")]
    public decimal DiscountValue { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Priority")]
    public int Priority { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsActive")]
    public bool IsActive { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsExclusive")]
    public bool IsExclusive { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount")]
    public int? LinkedDiscountId { get; set; }

    public string LinkedDiscountName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.CarryDefaultDiscount")]
    public bool CarryDefaultDiscount { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StopFurtherRulesForMatchedLines")]
    public bool StopFurtherRulesForMatchedLines { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EnableStackedCumulativeMode")]
    public bool EnableStackedCumulativeMode { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsFlashEnabled")]
    public bool IsFlashEnabled { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal")]
    public int UsageLimitTotal { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer")]
    public int UsageLimitPerCustomer { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowStartUtc")]
    [UIHint("DateTimeNullable")]
    public DateTime? UsageWindowStartUtc { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc")]
    [UIHint("DateTimeNullable")]
    public DateTime? UsageWindowEndUtc { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StartDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? StartDate { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? EndDate { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SelectedStoreIds")]
    public IList<int> SelectedStoreIds { get; set; } = new List<int>();

    public int RuleProductCount { get; set; }
    public int RuleTierCount { get; set; }
    public int RuleConditionCount { get; set; }
    public int ExcludedProductCount { get; set; }
    public string SetupStatus { get; set; } = string.Empty;
    public string SetupDetails { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }

    public IList<SelectListItem> AvailableRuleTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableDiscountTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableDiscountScopes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableLinkedDiscounts { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public PromotionRuleProductSearchModel RuleProductSearchModel { get; set; } = new();
    public PromotionRuleTierSearchModel RuleTierSearchModel { get; set; } = new();
    public PromotionRuleConditionSearchModel RuleConditionSearchModel { get; set; } = new();
    public PromotionRuleUsageHistorySearchModel RuleUsageHistorySearchModel { get; set; } = new();
    public PromotionRuleExcludedProductSearchModel ExcludedProductSearchModel { get; set; } = new();
}
