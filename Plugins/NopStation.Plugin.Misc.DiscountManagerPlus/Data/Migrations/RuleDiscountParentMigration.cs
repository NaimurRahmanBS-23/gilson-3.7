using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-13 18:40:00", "NopStation.Plugin.Misc.DiscountManagerPlus add discount parent field")]
public class RuleDiscountParentMigration : Migration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.DiscountId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.DiscountId)).AsInt32().NotNullable()
                .WithDefaultValue(0);
        }

        if (Schema.Table(tableName).Column(nameof(PromotionRule.LinkedDiscountId)).Exists())
        {
            Execute.Sql($@"
UPDATE [{tableName}]
SET [{nameof(PromotionRule.DiscountId)}] = [{nameof(PromotionRule.LinkedDiscountId)}]
WHERE [{nameof(PromotionRule.DiscountId)}] = 0
  AND [{nameof(PromotionRule.LinkedDiscountId)}] IS NOT NULL
  AND [{nameof(PromotionRule.LinkedDiscountId)}] > 0");
        }
    }

    public override void Down()
    {
    }
}
