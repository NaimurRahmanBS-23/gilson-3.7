using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IDiscountCoordinationService
    {
        Dictionary<int, List<DiscountAllocation>> CoordinateCheapestItemDiscounts(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart);

        IList<ShoppingCartItem> GetLowestPricedItemsExcludingAllocated(
            IList<ShoppingCartItem> items,
            IDictionary<int, int> allocatedQuantities,
            int requiredQuantity = 1);

        decimal CalculateEffectiveDiscountValue(AppliedPromotion promotion);
    }

    public class DiscountAllocation
    {
        public int LineId { get; set; }
        public decimal DiscountAmount { get; set; }
        public int PromotionRuleId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int ProductId { get; set; }
        public bool IsFreeItem { get; set; }
    }
}
