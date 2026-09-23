using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionRuleEvaluator
    {
        AppliedPromotion EvaluateRule(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionEvaluationContext context);
    }
}
