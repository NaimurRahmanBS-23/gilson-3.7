using Nop.Core;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
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
        public string RewardAttributeValueIds { get; set; }

        public RewardAttributeSelectionType RewardAttributeSelectionType
        {
            get { return (RewardAttributeSelectionType)RewardAttributeSelectionTypeId; }
            set { RewardAttributeSelectionTypeId = (int)value; }
        }
    }
}
