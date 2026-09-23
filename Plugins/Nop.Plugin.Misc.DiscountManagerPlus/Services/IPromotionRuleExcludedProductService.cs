using System.Collections.Generic;
using Nop.Core;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionRuleExcludedProductService
    {
        PromotionRuleExcludedProduct GetExcludedProductById(int id);
        IList<PromotionRuleExcludedProduct> GetExcludedProductsByRuleId(int promotionRuleId);
        IList<int> GetExcludedProductIdsByRuleId(int promotionRuleId);
        bool IsProductExcluded(int promotionRuleId, int productId);
        void InsertExcludedProduct(PromotionRuleExcludedProduct excludedProduct);
        void UpdateExcludedProduct(PromotionRuleExcludedProduct excludedProduct);
        void DeleteExcludedProduct(PromotionRuleExcludedProduct excludedProduct);
        void SaveExcludedProducts(int promotionRuleId, IList<int> productIds);
        void AddExcludedProducts(int promotionRuleId, IList<int> productIds);
        void RemoveExcludedProducts(int promotionRuleId, IList<int> productIds);
        void DeleteExcludedProductsByRuleId(int promotionRuleId);
        IPagedList<PromotionRuleExcludedProduct> GetExcludedProductsPaged(
            int promotionRuleId,
            int pageIndex = 0,
            int pageSize = int.MaxValue);
    }
}
