using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure
{
    public static class AdminMappingHelper
    {
        public static PromotionRuleModel ToModel(this PromotionRule entity)
        {
            if (entity == null)
                return null;

            return new PromotionRuleModel
            {
                Id = entity.Id,
                DiscountId = entity.DiscountId,
                Name = entity.Name,
                SystemName = entity.SystemName,
                RuleTypeId = entity.RuleTypeId,
                DiscountTypeId = entity.DiscountTypeId,
                DiscountScopeId = entity.DiscountScopeId,
                DiscountValue = entity.DiscountValue,
                Priority = entity.Priority,
                IsActive = entity.IsActive,
                IsExclusive = entity.IsExclusive,
                LinkedDiscountId = entity.LinkedDiscountId,
                CarryDefaultDiscount = entity.CarryDefaultDiscount,
                StopFurtherRulesForMatchedLines = entity.StopFurtherRulesForMatchedLines,
                EnableStackedCumulativeMode = entity.EnableStackedCumulativeMode,
                IsFlashEnabled = entity.IsFlashEnabled,
                UsageLimitTotal = entity.UsageLimitTotal,
                UsageLimitPerCustomer = entity.UsageLimitPerCustomer,
                UsageWindowStartUtc = entity.UsageWindowStartUtc,
                UsageWindowEndUtc = entity.UsageWindowEndUtc
            };
        }

        public static PromotionRule ToEntity(this PromotionRuleModel model)
        {
            if (model == null)
                return null;

            return model.ToEntity(new PromotionRule());
        }

        public static PromotionRule ToEntity(this PromotionRuleModel model, PromotionRule entity)
        {
            if (model == null || entity == null)
                return entity;

            entity.DiscountId = model.DiscountId;
            entity.Name = model.Name;
            entity.SystemName = model.SystemName;
            entity.RuleTypeId = model.RuleTypeId;
            entity.DiscountTypeId = model.DiscountTypeId;
            entity.DiscountScopeId = model.DiscountScopeId;
            entity.DiscountValue = model.DiscountValue;
            entity.Priority = model.Priority;
            entity.IsActive = model.IsActive;
            entity.IsExclusive = model.IsExclusive;
            entity.LinkedDiscountId = model.LinkedDiscountId;
            entity.CarryDefaultDiscount = model.CarryDefaultDiscount;
            entity.StopFurtherRulesForMatchedLines = model.StopFurtherRulesForMatchedLines;
            entity.EnableStackedCumulativeMode = model.EnableStackedCumulativeMode;
            entity.IsFlashEnabled = model.IsFlashEnabled;
            entity.UsageLimitTotal = model.UsageLimitTotal;
            entity.UsageLimitPerCustomer = model.UsageLimitPerCustomer;
            entity.UsageWindowStartUtc = model.UsageWindowStartUtc;
            entity.UsageWindowEndUtc = model.UsageWindowEndUtc;
            entity.StartDateUtc = model.StartDate;
            entity.EndDateUtc = model.EndDate;
            return entity;
        }

        public static PromotionRuleTierModel ToModel(this PromotionRuleTier entity)
        {
            if (entity == null)
                return null;

            return new PromotionRuleTierModel
            {
                Id = entity.Id,
                PromotionRuleId = entity.PromotionRuleId,
                MinQuantity = entity.MinQuantity,
                MaxQuantity = entity.MaxQuantity,
                RewardQuantity = entity.RewardQuantity,
                DiscountTypeId = entity.DiscountTypeId,
                DiscountValue = entity.DiscountValue,
                AutoAddReward = entity.AutoAddReward,
                RewardProductId = entity.RewardProductId
            };
        }

        public static PromotionRuleTier ToEntity(this PromotionRuleTierModel model)
        {
            if (model == null)
                return null;

            return model.ToEntity(new PromotionRuleTier());
        }

        public static PromotionRuleTier ToEntity(this PromotionRuleTierModel model, PromotionRuleTier entity)
        {
            if (model == null || entity == null)
                return entity;

            entity.PromotionRuleId = model.PromotionRuleId;
            entity.MinQuantity = model.MinQuantity;
            entity.MaxQuantity = model.MaxQuantity;
            entity.RewardQuantity = model.RewardQuantity;
            entity.DiscountTypeId = model.DiscountTypeId;
            entity.DiscountValue = model.DiscountValue;
            entity.AutoAddReward = model.AutoAddReward;
            entity.RewardProductId = model.RewardProductId;
            return entity;
        }

        public static PromotionRuleConditionModel ToModel(this PromotionRuleCondition entity)
        {
            if (entity == null)
                return null;

            return new PromotionRuleConditionModel
            {
                Id = entity.Id,
                PromotionRuleId = entity.PromotionRuleId,
                DiscountRequirementId = entity.DiscountRequirementId,
                ConditionGroup = entity.ConditionGroup,
                ParentConditionId = entity.ParentConditionId,
                LogicalOperatorId = entity.LogicalOperatorId,
                ConditionOperatorId = entity.ConditionOperatorId,
                MinValue = entity.MinValue,
                MaxValue = entity.MaxValue,
                RequiredProductId = entity.RequiredProductId,
                ExcludedProductId = entity.ExcludedProductId,
                RequiredCategoryId = entity.RequiredCategoryId,
                RequiredVendorId = entity.RequiredVendorId,
                RequiredCustomerRoleId = entity.RequiredCustomerRoleId,
                IsFirstOrderOnly = entity.IsFirstOrderOnly,
                IsNewCustomerOnly = entity.IsNewCustomerOnly,
                ConditionRestrictionTypeId = entity.ConditionRestrictionTypeId,
                ConditionSourceTypeId = entity.ConditionSourceTypeId,
                ConditionSourceData = entity.ConditionSourceData,
                QuantityMin = entity.QuantityMin,
                QuantityMax = entity.QuantityMax,
                RequiredCountryCodesCsv = entity.RequiredCountryCodesCsv,
                RequiredPaymentMethodsCsv = entity.RequiredPaymentMethodsCsv,
                RequiredCouponCodesCsv = entity.RequiredCouponCodesCsv,
                RequiredOrderCountMin = entity.RequiredOrderCountMin,
                RequiredOrderCountMax = entity.RequiredOrderCountMax,
                RequireSameLineMatch = entity.RequireSameLineMatch,
                AttributeMatchModeId = entity.AttributeMatchModeId
            };
        }

        public static PromotionRuleCondition ToEntity(this PromotionRuleConditionModel model)
        {
            if (model == null)
                return null;

            return model.ToEntity(new PromotionRuleCondition());
        }

        public static PromotionRuleCondition ToEntity(this PromotionRuleConditionModel model, PromotionRuleCondition entity)
        {
            if (model == null || entity == null)
                return entity;

            entity.PromotionRuleId = model.PromotionRuleId;
            entity.DiscountRequirementId = model.DiscountRequirementId;
            entity.ConditionGroup = model.ConditionGroup;
            entity.ParentConditionId = model.ParentConditionId;
            entity.LogicalOperatorId = model.LogicalOperatorId;
            entity.ConditionOperatorId = model.ConditionOperatorId;
            entity.MinValue = model.MinValue;
            entity.MaxValue = model.MaxValue;
            entity.RequiredProductId = model.RequiredProductId;
            entity.ExcludedProductId = model.ExcludedProductId;
            entity.RequiredCategoryId = model.RequiredCategoryId;
            entity.RequiredVendorId = model.RequiredVendorId;
            entity.RequiredCustomerRoleId = model.RequiredCustomerRoleId;
            entity.IsFirstOrderOnly = model.IsFirstOrderOnly;
            entity.IsNewCustomerOnly = model.IsNewCustomerOnly;
            entity.ConditionRestrictionTypeId = model.ConditionRestrictionTypeId;
            entity.ConditionSourceTypeId = model.ConditionSourceTypeId;
            entity.ConditionSourceData = model.ConditionSourceData;
            entity.QuantityMin = model.QuantityMin;
            entity.QuantityMax = model.QuantityMax;
            entity.RequiredCountryCodesCsv = model.RequiredCountryCodesCsv;
            entity.RequiredPaymentMethodsCsv = model.RequiredPaymentMethodsCsv;
            entity.RequiredCouponCodesCsv = model.RequiredCouponCodesCsv;
            entity.RequiredOrderCountMin = model.RequiredOrderCountMin;
            entity.RequiredOrderCountMax = model.RequiredOrderCountMax;
            entity.RequireSameLineMatch = model.RequireSameLineMatch;
            entity.AttributeMatchModeId = model.AttributeMatchModeId;
            return entity;
        }
    }
}
