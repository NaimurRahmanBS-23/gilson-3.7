using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionSocialShareEventBuilder : NopEntityBuilder<PromotionSocialShareEvent>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionSocialShareEvent.CustomerId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionSocialShareEvent.StoreId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionSocialShareEvent.RuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionSocialShareEvent.TokenHash)).AsString(512).NotNullable()
            .WithColumn(nameof(PromotionSocialShareEvent.Channel)).AsString(200).Nullable()
            .WithColumn(nameof(PromotionSocialShareEvent.CreatedOnUtc)).AsDateTime2().NotNullable()
            .WithColumn(nameof(PromotionSocialShareEvent.IsConsumed)).AsBoolean().NotNullable().WithDefaultValue(false);
    }
}
