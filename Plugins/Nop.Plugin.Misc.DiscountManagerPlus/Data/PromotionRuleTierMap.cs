using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleTierMap : NopEntityTypeConfiguration<PromotionRuleTier>
    {
        public PromotionRuleTierMap()
        {
            this.ToTable("DMP_PromotionRuleTier");
            this.HasKey(x => x.Id);
            this.Property(x => x.DiscountValue).HasPrecision(18, 4);
            this.Ignore(x => x.DiscountType);
        }
    }
}
