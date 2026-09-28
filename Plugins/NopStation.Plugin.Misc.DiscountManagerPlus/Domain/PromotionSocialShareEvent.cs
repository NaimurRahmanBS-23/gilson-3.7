using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionSocialShareEvent : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public int RuleId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public bool IsConsumed { get; set; }
}
