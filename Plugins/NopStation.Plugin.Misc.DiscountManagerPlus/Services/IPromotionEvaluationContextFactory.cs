using Nop.Core.Domain.Orders;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IPromotionEvaluationContextFactory
{
    Task<PromotionEvaluationContext> BuildDefaultEvaluationContextAsync(IList<ShoppingCartItem> cart, int storeId = 0);

    int ResolveEvaluationStoreId(IList<ShoppingCartItem> cart, PromotionEvaluationContext context);
}
