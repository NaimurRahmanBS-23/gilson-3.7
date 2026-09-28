using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

/// <summary>
/// Builder for the PromotionRuleExcludedProduct entity
/// </summary>
public class PromotionRuleExcludedProductBuilder : NopEntityBuilder<PromotionRuleExcludedProduct>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleExcludedProduct.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleExcludedProduct.ProductId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleExcludedProduct.CreatedOnUtc)).AsDateTime().NotNullable();
    }

    #endregion
}
