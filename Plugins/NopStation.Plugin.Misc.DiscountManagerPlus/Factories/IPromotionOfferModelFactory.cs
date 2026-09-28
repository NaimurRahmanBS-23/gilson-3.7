using NopStation.Plugin.Misc.DiscountManagerPlus.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Factories;

public interface IPromotionOfferModelFactory
{
    Task<PromotionOfferListModel> PreparePromotionOfferListModelAsync();
}
