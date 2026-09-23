using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleUsageMap : NopEntityTypeConfiguration<PromotionRuleUsage>
    {
        public PromotionRuleUsageMap()
        {
            this.ToTable("DMP_PromotionRuleUsage");
            this.HasKey(x => x.Id);
            this.Property(x => x.DiscountAmountApplied).HasPrecision(18, 4);
        }
    }
}
