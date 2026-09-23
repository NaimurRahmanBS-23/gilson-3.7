using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Validators;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    [Validator(typeof(PromotionRuleValidator))]
    public partial class PromotionRuleModel : BaseNopEntityModel
    {
        public PromotionRuleModel()
        {
            ParentDiscountName = string.Empty;
            ParentDiscountLimitationSummary = string.Empty;
            ParentDiscountMaximumDiscountAmountDisplay = string.Empty;
            ParentDiscountMaximumDiscountedQuantityDisplay = string.Empty;
            Name = string.Empty;
            SystemName = string.Empty;
            RuleTypeName = string.Empty;
            DiscountTypeName = string.Empty;
            DiscountScopeName = string.Empty;
            LinkedDiscountName = string.Empty;
            SelectedStoreIds = new List<int>();
            SetupStatus = string.Empty;
            SetupDetails = string.Empty;
            AvailableRuleTypes = new List<SelectListItem>();
            AvailableDiscountTypes = new List<SelectListItem>();
            AvailableDiscountScopes = new List<SelectListItem>();
            AvailableLinkedDiscounts = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            RuleProductSearchModel = new PromotionRuleProductSearchModel();
            RuleTierSearchModel = new PromotionRuleTierSearchModel();
            RuleConditionSearchModel = new PromotionRuleConditionSearchModel();
            RuleUsageHistorySearchModel = new PromotionRuleUsageHistorySearchModel();
            ExcludedProductSearchModel = new PromotionRuleExcludedProductSearchModel();
        }

        public int DiscountId { get; set; }

        public string ParentDiscountName { get; set; }
        public string ParentDiscountLimitationSummary { get; set; }
        public string ParentDiscountMaximumDiscountAmountDisplay { get; set; }
        public string ParentDiscountMaximumDiscountedQuantityDisplay { get; set; }
        public bool ParentDiscountUsesNativeCustomerLimit { get; set; }

        public bool IsDiscountBound { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name")]
        [AllowHtml]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName")]
        [AllowHtml]
        public string SystemName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType")]
        public int RuleTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType")]
        public string RuleTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType")]
        public int DiscountTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType")]
        public string DiscountTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope")]
        public int DiscountScopeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope")]
        public string DiscountScopeName { get; set; }

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

        public string LinkedDiscountName { get; set; }

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
        public IList<int> SelectedStoreIds { get; set; }

        public int RuleProductCount { get; set; }
        public int RuleTierCount { get; set; }
        public int RuleConditionCount { get; set; }
        public int ExcludedProductCount { get; set; }
        public string SetupStatus { get; set; }
        public string SetupDetails { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public IList<SelectListItem> AvailableRuleTypes { get; set; }
        public IList<SelectListItem> AvailableDiscountTypes { get; set; }
        public IList<SelectListItem> AvailableDiscountScopes { get; set; }
        public IList<SelectListItem> AvailableLinkedDiscounts { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        public PromotionRuleProductSearchModel RuleProductSearchModel { get; set; }
        public PromotionRuleTierSearchModel RuleTierSearchModel { get; set; }
        public PromotionRuleConditionSearchModel RuleConditionSearchModel { get; set; }
        public PromotionRuleUsageHistorySearchModel RuleUsageHistorySearchModel { get; set; }
        public PromotionRuleExcludedProductSearchModel ExcludedProductSearchModel { get; set; }
    }
}
