using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IRewardSynchronizationService
{
    Task<string> BuildRewardAttributesXmlAsync(PromotionRuleProduct rewardProduct);

    Task<bool> RequiresRewardSelectionAsync(PromotionRuleProduct rewardRuleProduct, Product rewardProduct);

    Task<IList<ShoppingCartItem>> FilterRewardItemsByAttributesAsync(IList<ShoppingCartItem> items, string attributesXml);

    Task TrackManualRewardAsync(Customer customer, int storeId, int promotionRuleId, int productId, string attributesXml, int quantityAdded);

    Task SynchronizeAutoAddedRewardsAsync(
        Customer customer,
        IList<ShoppingCartItem> cart,
        IList<AppliedPromotion> appliedPromotions,
        int storeId = 0);
}
