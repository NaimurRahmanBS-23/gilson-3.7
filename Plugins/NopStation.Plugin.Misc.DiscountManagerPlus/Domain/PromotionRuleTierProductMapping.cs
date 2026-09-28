using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRuleTierProductMapping : BaseEntity
{
    public int PromotionRuleId { get; set; }
    public int PromotionRuleTierId { get; set; }
    public int PromotionRuleProductId { get; set; }
}
