using Nop.Data.Mapping;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data;

public class BaseNameCompatibility : INameCompatibility
{
    public Dictionary<Type, string> TableNames => new()
    {
        { typeof(PromotionRule), "NS_PE_PromotionRule" },
        { typeof(PromotionRuleProduct), "NS_PE_PromotionRuleProduct" },
        { typeof(PromotionRuleCondition), "NS_PE_PromotionRuleCondition" },
        { typeof(PromotionRuleTier), "NS_PE_PromotionRuleTier" },
        { typeof(PromotionRuleTierProductMapping), "NS_PE_PromotionRuleTierProductMapping" },
        { typeof(PromotionRuleUsage), "NS_PE_PromotionRuleUsage" },
        { typeof(PromotionSocialShareEvent), "NS_PE_PromotionSocialShareEvent" },
        { typeof(PromotionRuleExcludedProduct), "NS_PE_PromotionRuleExcludedProduct" }
    };

    public Dictionary<(Type, string), string> ColumnName => new();
}
