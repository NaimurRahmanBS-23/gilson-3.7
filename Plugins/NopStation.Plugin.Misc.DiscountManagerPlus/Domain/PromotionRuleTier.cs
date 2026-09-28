using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRuleTier : BaseEntity
{
    public int PromotionRuleId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public int RewardQuantity { get; set; }
    public int DiscountTypeId { get; set; }
    public decimal DiscountValue { get; set; }
    public bool AutoAddReward { get; set; }
    public int? RewardProductId { get; set; }

    public DiscountType DiscountType
    {
        get => (DiscountType)DiscountTypeId;
        set => DiscountTypeId = (int)value;
    }
}
