using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

internal static class PromotionRuntimeHelper
{
    public static bool IsItemLevelRuleType(PromotionRuleType ruleType)
    {
        return ruleType == PromotionRuleType.ProductBased ||
               ruleType == PromotionRuleType.ComboPricing ||
               ruleType == PromotionRuleType.BuyXGetY;
    }

    public static void SetEligibleCartItems(AppliedPromotion appliedPromotion, IList<ShoppingCartItem> items)
    {
        if (appliedPromotion == null)
            return;

        if (items == null || !items.Any())
        {
            appliedPromotion.EligibleShoppingCartItemIds = new List<int>();
            return;
        }

        appliedPromotion.EligibleShoppingCartItemIds = items
            .Where(x => x != null)
            .Select(x => x.Id)
            .Distinct()
            .ToList();
    }

    public static int GetParentDiscountId(PromotionRule rule)
    {
        if (rule == null)
            return 0;

        return rule.DiscountId > 0 ? rule.DiscountId : rule.LinkedDiscountId.GetValueOrDefault();
    }
}
