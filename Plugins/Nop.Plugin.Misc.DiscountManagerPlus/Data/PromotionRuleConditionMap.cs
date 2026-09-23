using Nop.Data.Mapping;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class PromotionRuleConditionMap : NopEntityTypeConfiguration<PromotionRuleCondition>
    {
        public PromotionRuleConditionMap()
        {
            this.ToTable("DMP_PromotionRuleCondition");
            this.HasKey(x => x.Id);
            this.Property(x => x.MinValue).HasPrecision(18, 4);
            this.Property(x => x.MaxValue).HasPrecision(18, 4);
            this.Property(x => x.RequiredCountryCodesCsv).HasMaxLength(2000);
            this.Property(x => x.RequiredPaymentMethodsCsv).HasMaxLength(2000);
            this.Property(x => x.RequiredCouponCodesCsv).HasMaxLength(2000);
            this.Ignore(x => x.ConditionOperator);
            this.Ignore(x => x.LogicalOperator);
            this.Ignore(x => x.ConditionRestrictionType);
            this.Ignore(x => x.ConditionSourceType);
            this.Ignore(x => x.AttributeMatchMode);
        }
    }
}
