using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionSocialShareEventMap : NopEntityTypeConfiguration<PromotionSocialShareEvent>
    {
        public PromotionSocialShareEventMap()
        {
            this.ToTable("DMP_PromotionSocialShareEvent");
            this.HasKey(x => x.Id);
            this.Property(x => x.TokenHash).IsRequired().HasMaxLength(512);
            this.Property(x => x.Channel).HasMaxLength(200);
        }
    }
}
