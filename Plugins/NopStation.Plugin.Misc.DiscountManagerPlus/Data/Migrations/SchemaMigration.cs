using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2025-01-01 00:00:00", "NopStation.Plugin.Misc.DiscountManagerPlus base schema")]
public class SchemaMigration : AutoReversingMigration
{
    #region Methods

    public override void Up()
    {
        if (!Schema.Table<PromotionRule>().Exists())
            Create.TableFor<PromotionRule>();

        if (!Schema.Table<PromotionRuleProduct>().Exists())
            Create.TableFor<PromotionRuleProduct>();

        if (!Schema.Table<PromotionRuleCondition>().Exists())
            Create.TableFor<PromotionRuleCondition>();

        if (!Schema.Table<PromotionRuleTier>().Exists())
            Create.TableFor<PromotionRuleTier>();

        if (!Schema.Table<PromotionRuleTierProductMapping>().Exists())
            Create.TableFor<PromotionRuleTierProductMapping>();

        if (!Schema.Table<PromotionRuleUsage>().Exists())
            Create.TableFor<PromotionRuleUsage>();

        if (!Schema.Table<PromotionSocialShareEvent>().Exists())
            Create.TableFor<PromotionSocialShareEvent>();
    }

    #endregion
}
