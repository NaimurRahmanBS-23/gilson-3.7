using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionRuleExcludedProductService : IPromotionRuleExcludedProductService
    {
        private readonly IRepository<PromotionRuleExcludedProduct> _excludedProductRepository;
        private readonly ICacheManager _cacheManager;

        public PromotionRuleExcludedProductService(
            IRepository<PromotionRuleExcludedProduct> excludedProductRepository,
            ICacheManager cacheManager)
        {
            _excludedProductRepository = excludedProductRepository;
            _cacheManager = cacheManager;
        }

        protected virtual string GetExcludedProductsByRuleCacheKey(int promotionRuleId)
        {
            return string.Format(DiscountManagerPlusDefaults.ExcludedProductsByRuleCacheKey, promotionRuleId);
        }

        protected virtual string GetExcludedProductIdsByRuleCacheKey(int promotionRuleId)
        {
            return string.Format(DiscountManagerPlusDefaults.ExcludedProductIdsByRuleCacheKey, promotionRuleId);
        }

        public virtual PromotionRuleExcludedProduct GetExcludedProductById(int id)
        {
            return _excludedProductRepository.GetById(id);
        }

        public virtual IList<PromotionRuleExcludedProduct> GetExcludedProductsByRuleId(int promotionRuleId)
        {
            var cacheKey = GetExcludedProductsByRuleCacheKey(promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _excludedProductRepository.Table
                    .Where(ep => ep.PromotionRuleId == promotionRuleId)
                    .ToList();
            });
        }

        public virtual IList<int> GetExcludedProductIdsByRuleId(int promotionRuleId)
        {
            var cacheKey = GetExcludedProductIdsByRuleCacheKey(promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _excludedProductRepository.Table
                    .Where(ep => ep.PromotionRuleId == promotionRuleId)
                    .Select(ep => ep.ProductId)
                    .Distinct()
                    .ToList();
            });
        }

        public virtual bool IsProductExcluded(int promotionRuleId, int productId)
        {
            var excludedIds = GetExcludedProductIdsByRuleId(promotionRuleId);
            return excludedIds.Contains(productId);
        }

        public virtual void InsertExcludedProduct(PromotionRuleExcludedProduct excludedProduct)
        {
            _excludedProductRepository.Insert(excludedProduct);
            _cacheManager.Remove(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
            _cacheManager.Remove(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
        }

        public virtual void UpdateExcludedProduct(PromotionRuleExcludedProduct excludedProduct)
        {
            _excludedProductRepository.Update(excludedProduct);
            _cacheManager.Remove(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
            _cacheManager.Remove(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
        }

        public virtual void DeleteExcludedProduct(PromotionRuleExcludedProduct excludedProduct)
        {
            _excludedProductRepository.Delete(excludedProduct);
            _cacheManager.Remove(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
            _cacheManager.Remove(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
        }

        public virtual void SaveExcludedProducts(int promotionRuleId, IList<int> productIds)
        {
            var existingExclusions = GetExcludedProductsByRuleId(promotionRuleId);
            var existingProductIds = new HashSet<int>(existingExclusions.Select(ep => ep.ProductId));
            if (productIds == null)
                productIds = new List<int>();

            var newProductIds = productIds.Where(id => !existingProductIds.Contains(id)).ToList();
            foreach (var productId in newProductIds)
            {
                var excludedProduct = new PromotionRuleExcludedProduct
                {
                    PromotionRuleId = promotionRuleId,
                    ProductId = productId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                InsertExcludedProduct(excludedProduct);
            }

            var productIdSet = new HashSet<int>(productIds);
            var exclusionsToRemove = existingExclusions
                .Where(ep => !productIdSet.Contains(ep.ProductId))
                .ToList();

            foreach (var exclusion in exclusionsToRemove)
                DeleteExcludedProduct(exclusion);
        }

        public virtual void AddExcludedProducts(int promotionRuleId, IList<int> productIds)
        {
            var existingExcludedIds = new HashSet<int>(GetExcludedProductIdsByRuleId(promotionRuleId));
            var newProductIds = productIds.Where(id => !existingExcludedIds.Contains(id)).ToList();

            foreach (var productId in newProductIds)
            {
                var excludedProduct = new PromotionRuleExcludedProduct
                {
                    PromotionRuleId = promotionRuleId,
                    ProductId = productId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                InsertExcludedProduct(excludedProduct);
            }
        }

        public virtual void RemoveExcludedProducts(int promotionRuleId, IList<int> productIds)
        {
            var existingExclusions = GetExcludedProductsByRuleId(promotionRuleId);
            var exclusionsToRemove = existingExclusions
                .Where(ep => productIds.Contains(ep.ProductId))
                .ToList();

            foreach (var exclusion in exclusionsToRemove)
                DeleteExcludedProduct(exclusion);
        }

        public virtual void DeleteExcludedProductsByRuleId(int promotionRuleId)
        {
            var existingExclusions = GetExcludedProductsByRuleId(promotionRuleId);
            foreach (var exclusion in existingExclusions)
                _excludedProductRepository.Delete(exclusion);

            _cacheManager.Remove(GetExcludedProductsByRuleCacheKey(promotionRuleId));
            _cacheManager.Remove(GetExcludedProductIdsByRuleCacheKey(promotionRuleId));
        }

        public virtual IPagedList<PromotionRuleExcludedProduct> GetExcludedProductsPaged(
            int promotionRuleId,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var query = _excludedProductRepository.Table
                .Where(ep => ep.PromotionRuleId == promotionRuleId)
                .OrderBy(ep => ep.Id);

            return new PagedList<PromotionRuleExcludedProduct>(query, pageIndex, pageSize);
        }
    }
}
