using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleProductMap : NopEntityTypeConfiguration<PromotionRuleProduct>
    {
        public PromotionRuleProductMap()
        {
            this.ToTable("DMP_PromotionRuleProduct");
            this.HasKey(x => x.Id);
            this.Ignore(x => x.RewardAttributeSelectionType);
        }
    }
}
