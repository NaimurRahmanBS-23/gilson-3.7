using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleTierProductMappingMap : NopEntityTypeConfiguration<PromotionRuleTierProductMapping>
    {
        public PromotionRuleTierProductMappingMap()
        {
            this.ToTable("DMP_PromotionRuleTierProductMapping");
            this.HasKey(x => x.Id);
        }
    }
}
