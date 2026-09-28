using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleProductBuilder : NopEntityBuilder<PromotionRuleProduct>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleProduct.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleProduct.ProductId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleProduct.CategoryId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleProduct.ManufacturerId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleProduct.VendorId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleProduct.MinQuantity)).AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn(nameof(PromotionRuleProduct.MaxQuantity)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRuleProduct.IsAllProducts)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRuleProduct.IsRewardProduct)).AsBoolean().NotNullable()
            .WithColumn(nameof(PromotionRuleProduct.RewardAttributeSelectionTypeId)).AsInt32().NotNullable().WithDefaultValue((int)RewardAttributeSelectionType.Any)
            .WithColumn(nameof(PromotionRuleProduct.RewardAttributeValueIds)).AsString(int.MaxValue).Nullable();
    }

    #endregion
}
