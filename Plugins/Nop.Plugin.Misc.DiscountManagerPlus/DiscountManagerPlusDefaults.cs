namespace Nop.Plugin.Misc.DiscountManagerPlus
{
    public static class DiscountManagerPlusDefaults
    {
        public static string SystemName
        {
            get { return "Misc.DiscountManagerPlus"; }
        }

        public static string DiscountRequirementRuleSystemName
        {
            get { return SystemName; }
        }

        public static string DiscountRequirementHtmlFieldPrefix
        {
            get { return "DiscountManagerPlusRequirement{0}"; }
        }

        public static string DiscountRequirementKindSettingKey
        {
            get { return "DiscountManagerPlus.Requirement.Kind-{0}"; }
        }

        public static string LinkedDiscountWrapperSystemName
        {
            get { return "DiscountManagerPlus linked requirements"; }
        }

        public static string LinkedDiscountCarrySystemName
        {
            get { return "DiscountManagerPlus linked discount carry"; }
        }

        public static string ActiveRulesCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ActiveRules.{0}"; }
        }

        public static string ActiveRulesCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ActiveRules."; }
        }

        public static string PromotionRulesByDiscountCacheKey
        {
            get { return "Nop.DiscountManagerPlus.PromotionRules.ByDiscount.{0}"; }
        }

        public static string PromotionRulesByDiscountCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.PromotionRules.ByDiscount."; }
        }

        public static string SuppressingPromotionRulesByDiscountCacheKey
        {
            get { return "Nop.DiscountManagerPlus.PromotionRules.Suppressing.ByDiscount.{0}"; }
        }

        public static string SuppressingPromotionRulesByDiscountCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.PromotionRules.Suppressing.ByDiscount."; }
        }

        public static string RuleProductsByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleProducts.ByRule.{0}"; }
        }

        public static string RuleProductsByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleProducts.ByRule."; }
        }

        public static string RuleConditionsByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleConditions.ByRule.{0}"; }
        }

        public static string RuleConditionsByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleConditions.ByRule."; }
        }

        public static string RuleConditionsByRequirementCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleConditions.ByRequirement.{0}"; }
        }

        public static string RuleConditionsByRequirementCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleConditions.ByRequirement."; }
        }

        public static string RuleTiersByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleTiers.ByRule.{0}"; }
        }

        public static string RuleTiersByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleTiers.ByRule."; }
        }

        public static string RuleTierMappingsByTierCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ByTier.{0}"; }
        }

        public static string RuleTierMappingsByTierCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ByTier."; }
        }

        public static string RuleTierMappingsByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ByRule.{0}"; }
        }

        public static string RuleTierMappingsByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ByRule."; }
        }

        public static string MappedRuleProductIdsByTierCacheKey
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ProductIds.ByTier.{0}"; }
        }

        public static string MappedRuleProductIdsByTierCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.RuleTierMappings.ProductIds.ByTier."; }
        }

        public static string ProductCategoryIdsByProductCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductCategories.{0}"; }
        }

        public static string ProductCategoryIdsByProductCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductCategories."; }
        }

        public static string ProductManufacturerIdsByProductCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductManufacturers.{0}"; }
        }

        public static string ProductManufacturerIdsByProductCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductManufacturers."; }
        }

        public static string ProductSpecificationOptionIdsByProductCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductSpecificationOptions.{0}"; }
        }

        public static string ProductSpecificationOptionIdsByProductCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.ProductSpecificationOptions."; }
        }

        public static string CartAttributeValuesByCartItemCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.CartAttributeValues.{0}"; }
        }

        public static string CartAttributeValuesByCartItemCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.CartAttributeValues."; }
        }

        public static string AttributeValueIdsByMappingCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.AttributeValueIds.ByMapping.{0}"; }
        }

        public static string AttributeValueIdsByMappingCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ShortTerm.AttributeValueIds.ByMapping."; }
        }

        public static string OffersRouteName
        {
            get { return "Plugin.Misc.DiscountManagerPlus.Offers"; }
        }

        public static string ExcludedProductsByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ExcludedProducts.ByRule.{0}"; }
        }

        public static string ExcludedProductsByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ExcludedProducts.ByRule."; }
        }

        public static string ExcludedProductIdsByRuleCacheKey
        {
            get { return "Nop.DiscountManagerPlus.ExcludedProductIds.ByRule.{0}"; }
        }

        public static string ExcludedProductIdsByRuleCacheKeyPrefix
        {
            get { return "Nop.DiscountManagerPlus.ExcludedProductIds.ByRule."; }
        }
    }
}
