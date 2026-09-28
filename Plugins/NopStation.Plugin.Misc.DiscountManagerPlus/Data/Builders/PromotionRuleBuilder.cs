using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleBuilder : NopEntityBuilder<PromotionRule>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRule.DiscountId)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRule.Name)).AsString(400).NotNullable()
            .WithColumn(nameof(PromotionRule.SystemName)).AsString(400).NotNullable()
            .WithColumn(nameof(PromotionRule.RuleTypeId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRule.DiscountTypeId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRule.DiscountScopeId)).AsInt32().NotNullable().WithDefaultValue((int)DiscountScope.MatchedItemsOnly)
            .WithColumn(nameof(PromotionRule.DiscountValue)).AsDecimal(18, 4).NotNullable()
            .WithColumn(nameof(PromotionRule.Priority)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRule.IsActive)).AsBoolean().NotNullable()
            .WithColumn(nameof(PromotionRule.IsExclusive)).AsBoolean().NotNullable()
            .WithColumn(nameof(PromotionRule.LinkedDiscountId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRule.CarryDefaultDiscount)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRule.IsCumulativeWithDefaultDiscounts)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRule.StopFurtherRulesForMatchedLines)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRule.UsageLimitTotal)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRule.UsageLimitPerCustomer)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRule.UsageWindowStartUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(PromotionRule.UsageWindowEndUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(PromotionRule.IsFlashEnabled)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRule.StartDateUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(PromotionRule.EndDateUtc)).AsDateTime2().Nullable()
            .WithColumn(nameof(PromotionRule.LimitedToStores)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRule.LimitedToStore)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRule.CreatedOnUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(PromotionRule.UpdatedOnUtc)).AsDateTime2().NotNullable();
    }

    #endregion
}
