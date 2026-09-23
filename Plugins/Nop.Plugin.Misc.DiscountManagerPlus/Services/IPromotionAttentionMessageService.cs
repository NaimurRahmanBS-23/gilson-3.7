using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionAttentionMessageService
    {
        IList<PromotionAttentionMessage> GenerateDualOfferMessages(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations);

        DualOfferExplanation GenerateDualOfferExplanation(
            IList<AppliedPromotion> appliedPromotions,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations,
            IList<ShoppingCartItem> cart);

        IList<SavingsMaximizationTip> GenerateMaximizationTips(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart,
            IList<int> eligibleProductIds);

        ScenarioType DetectScenarioType(
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> allocations);
    }
}
