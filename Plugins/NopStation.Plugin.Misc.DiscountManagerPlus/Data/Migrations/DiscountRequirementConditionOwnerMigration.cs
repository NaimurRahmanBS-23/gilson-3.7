using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Data.Mapping;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-13 14:10:00", "NopStation.Plugin.Misc.DiscountManagerPlus add discount requirement condition owner")]
public class DiscountRequirementConditionOwnerMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleCondition));

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.DiscountRequirementId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.DiscountRequirementId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);
        }
    }
}
