using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IDiscountManagerPlusService
{
    Task<IList<AppliedPromotion>> EvaluateCartAsync(PromotionEvaluationContext context);
    Task<IList<AppliedPromotion>> EvaluateCartAsync(IList<ShoppingCartItem> cart, int storeId = 0);
    Task<IList<AppliedPromotion>> GetRequestAppliedPromotionsAsync(int customerId, int storeId);

    Task<IDictionary<int, decimal>> BuildLineDiscountMapAsync(PromotionEvaluationContext context);
    Task<IDictionary<int, decimal>> BuildLineDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId = 0);
    Task<IDictionary<int, decimal>> BuildRuleDiscountMapAsync(PromotionEvaluationContext context);
    Task<IDictionary<int, decimal>> BuildRuleDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId = 0);

    /// <summary>
    /// Gets coordinated discount allocations for dual-offer scenarios
    /// </summary>
    Task<IList<DiscountAllocation>> GetCoordinatedAllocationsAsync(IList<ShoppingCartItem> cart, int storeId);

    Task<AppliedPromotion> EvaluateRuleAsync(PromotionRule rule, IList<ShoppingCartItem> cart);

    Task<bool> EvaluateDiscountRequirementAsync(int discountRequirementId, Customer customer, int storeId = 0);
    Task<bool> EvaluateLinkedDiscountRequirementAsync(int discountRequirementId, Customer customer, int storeId = 0);
    Task<bool> IsLinkedDiscountEligibleAsync(PromotionRule rule, PromotionEvaluationContext context);
    Task<bool> ShouldSuppressLinkedDiscountAsync(int discountId, Customer customer, int storeId = 0);

    Task<decimal> GetCartSubtotalAsync(IList<ShoppingCartItem> cart);

    Task SynchronizeAutoAddedRewardsAsync(Customer customer, int storeId = 0);
}
