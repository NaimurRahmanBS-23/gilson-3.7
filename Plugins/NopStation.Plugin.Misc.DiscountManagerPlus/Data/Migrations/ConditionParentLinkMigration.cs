using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-04 00:00:02", "NopStation.Plugin.Misc.DiscountManagerPlus add parent condition link")]
public class ConditionParentLinkMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleCondition));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.ParentConditionId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.ParentConditionId)).AsInt32().Nullable();
        }
    }
}
