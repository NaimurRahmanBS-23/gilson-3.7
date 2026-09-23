using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IDiscountManagerPlusService
    {
        IList<AppliedPromotion> EvaluateCart(PromotionEvaluationContext context);
        IList<AppliedPromotion> EvaluateCart(IList<ShoppingCartItem> cart, int storeId = 0);
        IList<AppliedPromotion> GetRequestAppliedPromotions(int customerId, int storeId);

        IDictionary<int, decimal> BuildLineDiscountMap(PromotionEvaluationContext context);
        IDictionary<int, decimal> BuildLineDiscountMap(IList<ShoppingCartItem> cart, int storeId = 0);
        IDictionary<int, decimal> BuildRuleDiscountMap(PromotionEvaluationContext context);
        IDictionary<int, decimal> BuildRuleDiscountMap(IList<ShoppingCartItem> cart, int storeId = 0);

        IList<DiscountAllocation> GetCoordinatedAllocations(IList<ShoppingCartItem> cart, int storeId);

        AppliedPromotion EvaluateRule(PromotionRule rule, IList<ShoppingCartItem> cart);

        bool EvaluateDiscountRequirement(int discountRequirementId, Customer customer, int storeId = 0);
        bool EvaluateLinkedDiscountRequirement(int discountRequirementId, Customer customer, int storeId = 0);
        bool IsLinkedDiscountEligible(PromotionRule rule, PromotionEvaluationContext context);
        bool ShouldSuppressLinkedDiscount(int discountId, Customer customer, int storeId = 0);

        decimal GetCartSubtotal(IList<ShoppingCartItem> cart);

        void SynchronizeAutoAddedRewards(Customer customer, int storeId = 0);
    }
}
