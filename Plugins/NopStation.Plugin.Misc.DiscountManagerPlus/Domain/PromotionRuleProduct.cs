using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRuleProduct : BaseEntity
{
    public int PromotionRuleId { get; set; }
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }
    public int? VendorId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsAllProducts { get; set; }
    public bool IsRewardProduct { get; set; }
    public int RewardAttributeSelectionTypeId { get; set; }
    public string RewardAttributeValueIds { get; set; } = string.Empty;

    public RewardAttributeSelectionType RewardAttributeSelectionType
    {
        get => (RewardAttributeSelectionType)RewardAttributeSelectionTypeId;
        set => RewardAttributeSelectionTypeId = (int)value;
    }
}
