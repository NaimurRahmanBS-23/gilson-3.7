using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-03 00:00:02", "NopStation.Plugin.Misc.DiscountManagerPlus add rule product attribute fields")]
public class RuleProductAttributeFieldsMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleProduct));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleProduct.MaxQuantity)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleProduct.MaxQuantity))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleProduct.RewardAttributeSelectionTypeId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleProduct.RewardAttributeSelectionTypeId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)RewardAttributeSelectionType.Any);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleProduct.RewardAttributeValueIds)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleProduct.RewardAttributeValueIds))
                .AsString(int.MaxValue)
                .Nullable();
    }
}
