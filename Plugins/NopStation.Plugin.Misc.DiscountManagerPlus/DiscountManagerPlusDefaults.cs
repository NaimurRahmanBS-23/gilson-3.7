using Nop.Core.Caching;

namespace NopStation.Plugin.Misc.DiscountManagerPlus;

public static class DiscountManagerPlusDefaults
{
    public static string SystemName => "NopStation.Plugin.Misc.DiscountManagerPlus";
    public static string DiscountRequirementRuleSystemName => SystemName;
    public static string DiscountRequirementHtmlFieldPrefix => "DiscountManagerPlusRequirement{0}";
    public static string DiscountRequirementKindSettingKey => "DiscountManagerPlus.Requirement.Kind-{0}";
    public static string LinkedDiscountWrapperSystemName => "DiscountManagerPlus linked requirements";
    public static string LinkedDiscountCarrySystemName => "DiscountManagerPlus linked discount carry";

    public static CacheKey ActiveRulesCacheKey => new("NopStation.DiscountManagerPlus.ActiveRules.{0}");

    public static string ActiveRulesCacheKeyPrefix => "NopStation.DiscountManagerPlus.ActiveRules.";

    public static CacheKey PromotionRulesByDiscountCacheKey => new("NopStation.DiscountManagerPlus.PromotionRules.ByDiscount.{0}");

    public static string PromotionRulesByDiscountCacheKeyPrefix => "NopStation.DiscountManagerPlus.PromotionRules.ByDiscount.";

    public static CacheKey SuppressingPromotionRulesByDiscountCacheKey => new("NopStation.DiscountManagerPlus.PromotionRules.Suppressing.ByDiscount.{0}");

    public static string SuppressingPromotionRulesByDiscountCacheKeyPrefix => "NopStation.DiscountManagerPlus.PromotionRules.Suppressing.ByDiscount.";

    public static CacheKey RuleProductsByRuleCacheKey => new("NopStation.DiscountManagerPlus.RuleProducts.ByRule.{0}");

    public static string RuleProductsByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleProducts.ByRule.";

    public static CacheKey RuleConditionsByRuleCacheKey => new("NopStation.DiscountManagerPlus.RuleConditions.ByRule.{0}");

    public static string RuleConditionsByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleConditions.ByRule.";

    public static CacheKey RuleConditionsByRequirementCacheKey => new("NopStation.DiscountManagerPlus.RuleConditions.ByRequirement.{0}");

    public static string RuleConditionsByRequirementCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleConditions.ByRequirement.";

    public static CacheKey RuleTiersByRuleCacheKey => new("NopStation.DiscountManagerPlus.RuleTiers.ByRule.{0}");

    public static string RuleTiersByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleTiers.ByRule.";

    public static CacheKey RuleTierMappingsByTierCacheKey => new("NopStation.DiscountManagerPlus.RuleTierMappings.ByTier.{0}");

    public static string RuleTierMappingsByTierCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleTierMappings.ByTier.";

    public static CacheKey RuleTierMappingsByRuleCacheKey => new("NopStation.DiscountManagerPlus.RuleTierMappings.ByRule.{0}");

    public static string RuleTierMappingsByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleTierMappings.ByRule.";

    public static CacheKey MappedRuleProductIdsByTierCacheKey => new("NopStation.DiscountManagerPlus.RuleTierMappings.ProductIds.ByTier.{0}");

    public static string MappedRuleProductIdsByTierCacheKeyPrefix => "NopStation.DiscountManagerPlus.RuleTierMappings.ProductIds.ByTier.";

    public static CacheKey ProductCategoryIdsByProductCacheKey => new("NopStation.DiscountManagerPlus.ShortTerm.ProductCategories.{0}");

    public static string ProductCategoryIdsByProductCacheKeyPrefix => "NopStation.DiscountManagerPlus.ShortTerm.ProductCategories.";

    public static CacheKey ProductManufacturerIdsByProductCacheKey => new("NopStation.DiscountManagerPlus.ShortTerm.ProductManufacturers.{0}");

    public static string ProductManufacturerIdsByProductCacheKeyPrefix => "NopStation.DiscountManagerPlus.ShortTerm.ProductManufacturers.";

    public static CacheKey ProductSpecificationOptionIdsByProductCacheKey => new("NopStation.DiscountManagerPlus.ShortTerm.ProductSpecificationOptions.{0}");

    public static string ProductSpecificationOptionIdsByProductCacheKeyPrefix => "NopStation.DiscountManagerPlus.ShortTerm.ProductSpecificationOptions.";

    public static CacheKey CartAttributeValuesByCartItemCacheKey => new("NopStation.DiscountManagerPlus.ShortTerm.CartAttributeValues.{0}");

    public static string CartAttributeValuesByCartItemCacheKeyPrefix => "NopStation.DiscountManagerPlus.ShortTerm.CartAttributeValues.";

    public static CacheKey AttributeValueIdsByMappingCacheKey => new("NopStation.DiscountManagerPlus.ShortTerm.AttributeValueIds.ByMapping.{0}");

    public static string AttributeValueIdsByMappingCacheKeyPrefix => "NopStation.DiscountManagerPlus.ShortTerm.AttributeValueIds.ByMapping.";

    public static string OffersRouteName => "NopStation.DiscountManagerPlus.Offers";

    public static CacheKey ExcludedProductsByRuleCacheKey => new("NopStation.DiscountManagerPlus.ExcludedProducts.ByRule.{0}");

    public static string ExcludedProductsByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.ExcludedProducts.ByRule.";

    public static CacheKey ExcludedProductIdsByRuleCacheKey => new("NopStation.DiscountManagerPlus.ExcludedProductIds.ByRule.{0}");

    public static string ExcludedProductIdsByRuleCacheKeyPrefix => "NopStation.DiscountManagerPlus.ExcludedProductIds.ByRule.";
}
