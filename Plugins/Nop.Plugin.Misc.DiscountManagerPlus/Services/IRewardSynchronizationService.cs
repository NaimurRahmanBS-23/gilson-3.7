using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IRewardSynchronizationService
    {
        string BuildRewardAttributesXml(PromotionRuleProduct rewardProduct);

        bool RequiresRewardSelection(PromotionRuleProduct rewardRuleProduct, Product rewardProduct);

        IList<ShoppingCartItem> FilterRewardItemsByAttributes(IList<ShoppingCartItem> items, string attributesXml);

        void TrackManualReward(Customer customer, int storeId, int promotionRuleId, int productId, string attributesXml, int quantityAdded);

        void SynchronizeAutoAddedRewards(
            Customer customer,
            IList<ShoppingCartItem> cart,
            IList<AppliedPromotion> appliedPromotions,
            int storeId = 0);
    }
}
