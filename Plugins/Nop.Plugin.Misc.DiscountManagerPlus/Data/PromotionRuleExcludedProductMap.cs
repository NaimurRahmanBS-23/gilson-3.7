using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleExcludedProductMap : NopEntityTypeConfiguration<PromotionRuleExcludedProduct>
    {
        public PromotionRuleExcludedProductMap()
        {
            this.ToTable("DMP_PromotionRuleExcludedProduct");
            this.HasKey(x => x.Id);
        }
    }
}
