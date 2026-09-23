using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleMap : NopEntityTypeConfiguration<PromotionRule>
    {
        public PromotionRuleMap()
        {
            this.ToTable("DMP_PromotionRule");
            this.HasKey(x => x.Id);
            this.Property(x => x.Name).IsRequired().HasMaxLength(400);
            this.Property(x => x.SystemName).IsRequired().HasMaxLength(400);
            this.Property(x => x.DiscountValue).HasPrecision(18, 4);
            this.Ignore(x => x.RuleType);
            this.Ignore(x => x.DiscountType);
            this.Ignore(x => x.DiscountScope);
            this.Ignore(x => x.DiscountTargetType);
        }
    }
}
