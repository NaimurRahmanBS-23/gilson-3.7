using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Infrastructure;

public class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        CreateMap<PromotionRule, PromotionRuleModel>()
            .ForMember(m => m.ParentDiscountName, opt => opt.Ignore())
            .ForMember(m => m.IsDiscountBound, opt => opt.Ignore())
            .ForMember(m => m.CreatedOn, opt => opt.Ignore())
            .ForMember(m => m.UpdatedOn, opt => opt.Ignore())
            .ForMember(m => m.RuleTypeName, opt => opt.Ignore())
            .ForMember(m => m.DiscountTypeName, opt => opt.Ignore())
            .ForMember(m => m.DiscountScopeName, opt => opt.Ignore())
            .ForMember(m => m.StartDate, opt => opt.Ignore())
            .ForMember(m => m.EndDate, opt => opt.Ignore())
            .ForMember(m => m.UsageWindowStartUtc, opt => opt.Ignore())
            .ForMember(m => m.UsageWindowEndUtc, opt => opt.Ignore())
            .ForMember(m => m.RuleProductCount, opt => opt.Ignore())
            .ForMember(m => m.RuleTierCount, opt => opt.Ignore())
            .ForMember(m => m.RuleConditionCount, opt => opt.Ignore())
            .ForMember(m => m.SetupStatus, opt => opt.Ignore())
            .ForMember(m => m.SetupDetails, opt => opt.Ignore())
            .ForMember(m => m.AvailableRuleTypes, opt => opt.Ignore())
            .ForMember(m => m.AvailableDiscountTypes, opt => opt.Ignore())
            .ForMember(m => m.AvailableDiscountScopes, opt => opt.Ignore())
            .ForMember(m => m.AvailableLinkedDiscounts, opt => opt.Ignore())
            .ForMember(m => m.AvailableStores, opt => opt.Ignore())
            .ForMember(m => m.SelectedStoreIds, opt => opt.Ignore())
            .ForMember(m => m.RuleProductSearchModel, opt => opt.Ignore())
            .ForMember(m => m.RuleTierSearchModel, opt => opt.Ignore())
            .ForMember(m => m.RuleConditionSearchModel, opt => opt.Ignore())
            .ForMember(m => m.RuleUsageHistorySearchModel, opt => opt.Ignore())
            .ForMember(m => m.CustomProperties, opt => opt.Ignore());

        CreateMap<PromotionRuleModel, PromotionRule>()
            .ForMember(e => e.LimitedToStores, opt => opt.Ignore())
            .ForMember(e => e.CreatedOnUtc, opt => opt.Ignore())
            .ForMember(e => e.UpdatedOnUtc, opt => opt.Ignore());

        CreateMap<PromotionRuleTier, PromotionRuleTierModel>()
            .ForMember(m => m.DiscountTypeName, opt => opt.Ignore())
            .ForMember(m => m.RewardProductName, opt => opt.Ignore())
            .ForMember(m => m.SelectedRuleProductIds, opt => opt.Ignore())
            .ForMember(m => m.AppliesToRuleProductsSummary, opt => opt.Ignore())
            .ForMember(m => m.AvailableDiscountTypes, opt => opt.Ignore())
            .ForMember(m => m.AvailableProducts, opt => opt.Ignore())
            .ForMember(m => m.AvailableRuleProducts, opt => opt.Ignore())
            .ForMember(m => m.CustomProperties, opt => opt.Ignore());

        CreateMap<PromotionRuleTierModel, PromotionRuleTier>();

        CreateMap<PromotionRuleCondition, PromotionRuleConditionModel>()
            .ForMember(m => m.LogicalOperatorName, opt => opt.Ignore())
            .ForMember(m => m.Summary, opt => opt.Ignore())
            .ForMember(m => m.ConditionOperatorName, opt => opt.Ignore())
            .ForMember(m => m.ParentConditionName, opt => opt.Ignore())
            .ForMember(m => m.RequiredProductName, opt => opt.Ignore())
            .ForMember(m => m.ExcludedProductName, opt => opt.Ignore())
            .ForMember(m => m.RequiredCategoryName, opt => opt.Ignore())
            .ForMember(m => m.RequiredVendorName, opt => opt.Ignore())
            .ForMember(m => m.RequiredCustomerRoleName, opt => opt.Ignore())
            .ForMember(m => m.AttributeMatchModeName, opt => opt.Ignore())
            .ForMember(m => m.AvailableConditionOperators, opt => opt.Ignore())
            .ForMember(m => m.AvailableLogicalOperators, opt => opt.Ignore())
            .ForMember(m => m.AvailableCategories, opt => opt.Ignore())
            .ForMember(m => m.AvailableVendors, opt => opt.Ignore())
            .ForMember(m => m.AvailableCustomerRoles, opt => opt.Ignore())
            .ForMember(m => m.AvailableConditionSourceTypes, opt => opt.Ignore())
            .ForMember(m => m.AvailableConditionRestrictionTypes, opt => opt.Ignore())
            .ForMember(m => m.AvailableAttributeMatchModes, opt => opt.Ignore())
            .ForMember(m => m.AvailableParentConditions, opt => opt.Ignore())
            .ForMember(m => m.CustomProperties, opt => opt.Ignore());

        CreateMap<PromotionRuleConditionModel, PromotionRuleCondition>();
    }

    #endregion

    #region Properties

    public int Order => 0;

    #endregion
}
