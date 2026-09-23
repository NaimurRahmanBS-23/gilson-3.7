using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionEvaluationContextFactory
    {
        PromotionEvaluationContext BuildDefaultEvaluationContext(IList<ShoppingCartItem> cart, int storeId = 0);

        int ResolveEvaluationStoreId(IList<ShoppingCartItem> cart, PromotionEvaluationContext context);
    }
}
