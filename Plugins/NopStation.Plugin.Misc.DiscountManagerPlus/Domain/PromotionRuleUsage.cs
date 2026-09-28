using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRuleUsage : BaseEntity
{
    public int PromotionRuleId { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal DiscountAmountApplied { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
