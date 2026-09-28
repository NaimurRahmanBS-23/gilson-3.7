using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-05 00:00:02", "NopStation.Plugin.Misc.DiscountManagerPlus add rule tier product mapping")]
public class RuleTierProductMappingMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleTierProductMapping));
        if (!Schema.Table(tableName).Exists())
            Create.TableFor<PromotionRuleTierProductMapping>();
    }
}
