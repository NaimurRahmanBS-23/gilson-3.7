using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Service for managing promotion rule excluded products
/// </summary>
public class PromotionRuleExcludedProductService : IPromotionRuleExcludedProductService
{
    #region Fields

    private readonly IRepository<PromotionRuleExcludedProduct> _excludedProductRepository;
    private readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Ctor

    public PromotionRuleExcludedProductService(
        IRepository<PromotionRuleExcludedProduct> excludedProductRepository,
        IStaticCacheManager staticCacheManager)
    {
        _excludedProductRepository = excludedProductRepository;
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Utilities

    protected virtual CacheKey GetExcludedProductsByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.ExcludedProductsByRuleCacheKey, promotionRuleId);

    protected virtual CacheKey GetExcludedProductIdsByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.ExcludedProductIdsByRuleCacheKey, promotionRuleId);

    #endregion

    #region Methods

    /// <summary>
    /// Gets an excluded product by identifier
    /// </summary>
    public virtual async Task<PromotionRuleExcludedProduct> GetExcludedProductByIdAsync(int id)
    {
        return await _excludedProductRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets all excluded products for a promotion rule
    /// </summary>
    public virtual async Task<IList<PromotionRuleExcludedProduct>> GetExcludedProductsByRuleIdAsync(int promotionRuleId)
    {
        var cacheKey = GetExcludedProductsByRuleCacheKey(promotionRuleId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            return await _excludedProductRepository.Table
                .Where(ep => ep.PromotionRuleId == promotionRuleId)
                .ToListAsync();
        });
    }

    /// <summary>
    /// Gets excluded product IDs for a promotion rule
    /// </summary>
    public virtual async Task<IList<int>> GetExcludedProductIdsByRuleIdAsync(int promotionRuleId)
    {
        var cacheKey = GetExcludedProductIdsByRuleCacheKey(promotionRuleId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            return await _excludedProductRepository.Table
                .Where(ep => ep.PromotionRuleId == promotionRuleId)
                .Select(ep => ep.ProductId)
                .Distinct()
                .ToListAsync();
        });
    }

    /// <summary>
    /// Checks if a product is excluded from a promotion rule
    /// </summary>
    public virtual async Task<bool> IsProductExcludedAsync(int promotionRuleId, int productId)
    {
        var excludedIds = await GetExcludedProductIdsByRuleIdAsync(promotionRuleId);
        return excludedIds.Contains(productId);
    }

    /// <summary>
    /// Inserts an excluded product
    /// </summary>
    public virtual async Task InsertExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct)
    {
        await _excludedProductRepository.InsertAsync(excludedProduct);

        // Clear cache
        await _staticCacheManager.RemoveAsync(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
        await _staticCacheManager.RemoveAsync(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
    }

    /// <summary>
    /// Updates an excluded product
    /// </summary>
    public virtual async Task UpdateExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct)
    {
        await _excludedProductRepository.UpdateAsync(excludedProduct);

        // Clear cache
        await _staticCacheManager.RemoveAsync(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
        await _staticCacheManager.RemoveAsync(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
    }

    /// <summary>
    /// Deletes an excluded product
    /// </summary>
    public virtual async Task DeleteExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct)
    {
        await _excludedProductRepository.DeleteAsync(excludedProduct);

        // Clear cache
        await _staticCacheManager.RemoveAsync(GetExcludedProductsByRuleCacheKey(excludedProduct.PromotionRuleId));
        await _staticCacheManager.RemoveAsync(GetExcludedProductIdsByRuleCacheKey(excludedProduct.PromotionRuleId));
    }

    /// <summary>
    /// Saves excluded products for a promotion rule (replaces all existing exclusions)
    /// </summary>
    public virtual async Task SaveExcludedProductsAsync(int promotionRuleId, IList<int> productIds)
    {
        // Get existing exclusions
        var existingExclusions = await GetExcludedProductsByRuleIdAsync(promotionRuleId);
        var existingProductIds = existingExclusions.Select(ep => ep.ProductId).ToHashSet();

        // Add new exclusions
        var newProductIds = productIds.Except(existingProductIds).ToList();
        foreach (var productId in newProductIds)
        {
            var excludedProduct = new PromotionRuleExcludedProduct
            {
                PromotionRuleId = promotionRuleId,
                ProductId = productId,
                CreatedOnUtc = DateTime.UtcNow
            };
            await InsertExcludedProductAsync(excludedProduct);
        }

        // Remove old exclusions
        var removedProductIds = existingProductIds.Except(productIds).ToHashSet();
        var exclusionsToRemove = existingExclusions
            .Where(ep => removedProductIds.Contains(ep.ProductId))
            .ToList();

        foreach (var exclusion in exclusionsToRemove)
        {
            await DeleteExcludedProductAsync(exclusion);
        }
    }

    /// <summary>
    /// Adds multiple products to exclusion list
    /// </summary>
    public virtual async Task AddExcludedProductsAsync(int promotionRuleId, IList<int> productIds)
    {
        var existingExcludedIds = await GetExcludedProductIdsByRuleIdAsync(promotionRuleId);
        var newProductIds = productIds.Except(existingExcludedIds).ToList();

        foreach (var productId in newProductIds)
        {
            var excludedProduct = new PromotionRuleExcludedProduct
            {
                PromotionRuleId = promotionRuleId,
                ProductId = productId,
                CreatedOnUtc = DateTime.UtcNow
            };
            await InsertExcludedProductAsync(excludedProduct);
        }
    }

    /// <summary>
    /// Removes multiple products from exclusion list
    /// </summary>
    public virtual async Task RemoveExcludedProductsAsync(int promotionRuleId, IList<int> productIds)
    {
        var existingExclusions = await GetExcludedProductsByRuleIdAsync(promotionRuleId);
        var exclusionsToRemove = existingExclusions
            .Where(ep => productIds.Contains(ep.ProductId))
            .ToList();

        foreach (var exclusion in exclusionsToRemove)
        {
            await DeleteExcludedProductAsync(exclusion);
        }
    }

    /// <summary>
    /// Deletes all excluded products for a promotion rule
    /// </summary>
    public virtual async Task DeleteExcludedProductsByRuleIdAsync(int promotionRuleId)
    {
        var existingExclusions = await GetExcludedProductsByRuleIdAsync(promotionRuleId);

        foreach (var exclusion in existingExclusions)
        {
            await _excludedProductRepository.DeleteAsync(exclusion);
        }

        // Clear cache
        await _staticCacheManager.RemoveAsync(GetExcludedProductsByRuleCacheKey(promotionRuleId));
        await _staticCacheManager.RemoveAsync(GetExcludedProductIdsByRuleCacheKey(promotionRuleId));
    }

    /// <summary>
    /// Gets paginated list of excluded products for a promotion rule
    /// </summary>
    public virtual async Task<IPagedList<PromotionRuleExcludedProduct>> GetExcludedProductsPagedAsync(
        int promotionRuleId,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = _excludedProductRepository.Table
            .Where(ep => ep.PromotionRuleId == promotionRuleId);

        return await query.ToPagedListAsync(pageIndex, pageSize);
    }

    #endregion
}
