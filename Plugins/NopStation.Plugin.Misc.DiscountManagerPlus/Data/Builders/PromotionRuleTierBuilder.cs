using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleTierBuilder : NopEntityBuilder<PromotionRuleTier>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleTier.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTier.MinQuantity)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTier.MaxQuantity)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTier.RewardQuantity)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRuleTier.DiscountTypeId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleTier.DiscountValue)).AsDecimal(18, 4).NotNullable()
            .WithColumn(nameof(PromotionRuleTier.AutoAddReward)).AsBoolean().NotNullable()
            .WithColumn(nameof(PromotionRuleTier.RewardProductId)).AsInt32().Nullable();
    }

    #endregion
}
