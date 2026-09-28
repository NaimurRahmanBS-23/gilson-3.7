using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleUsageBuilder : NopEntityBuilder<PromotionRuleUsage>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleUsage.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleUsage.OrderId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleUsage.CustomerId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleUsage.DiscountAmountApplied)).AsDecimal(18, 4).NotNullable()
            .WithColumn(nameof(PromotionRuleUsage.CreatedOnUtc)).AsDateTime2().NotNullable();
    }

    #endregion
}
