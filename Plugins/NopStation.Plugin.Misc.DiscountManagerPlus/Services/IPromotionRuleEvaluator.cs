using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IPromotionRuleEvaluator
{
    Task<AppliedPromotion> EvaluateRuleAsync(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionEvaluationContext context);
}
