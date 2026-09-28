using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleTierProductMappingBuilder : NopEntityBuilder<PromotionRuleTierProductMapping>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleTierProductMapping.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTierProductMapping.PromotionRuleTierId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTierProductMapping.PromotionRuleProductId)).AsInt32().NotNullable();
    }
}
