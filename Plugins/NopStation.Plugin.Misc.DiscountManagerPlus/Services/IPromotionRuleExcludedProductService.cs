using Nop.Core;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Service for managing promotion rule excluded products
/// </summary>
public interface IPromotionRuleExcludedProductService
{
    /// <summary>
    /// Gets an excluded product by identifier
    /// </summary>
    Task<PromotionRuleExcludedProduct> GetExcludedProductByIdAsync(int id);

    /// <summary>
    /// Gets all excluded products for a promotion rule
    /// </summary>
    Task<IList<PromotionRuleExcludedProduct>> GetExcludedProductsByRuleIdAsync(int promotionRuleId);

    /// <summary>
    /// Gets excluded product IDs for a promotion rule
    /// </summary>
    Task<IList<int>> GetExcludedProductIdsByRuleIdAsync(int promotionRuleId);

    /// <summary>
    /// Checks if a product is excluded from a promotion rule
    /// </summary>
    Task<bool> IsProductExcludedAsync(int promotionRuleId, int productId);

    /// <summary>
    /// Inserts an excluded product
    /// </summary>
    Task InsertExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct);

    /// <summary>
    /// Updates an excluded product
    /// </summary>
    Task UpdateExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct);

    /// <summary>
    /// Deletes an excluded product
    /// </summary>
    Task DeleteExcludedProductAsync(PromotionRuleExcludedProduct excludedProduct);

    /// <summary>
    /// Saves excluded products for a promotion rule (replaces all existing exclusions)
    /// </summary>
    Task SaveExcludedProductsAsync(int promotionRuleId, IList<int> productIds);

    /// <summary>
    /// Adds multiple products to exclusion list
    /// </summary>
    Task AddExcludedProductsAsync(int promotionRuleId, IList<int> productIds);

    /// <summary>
    /// Removes multiple products from exclusion list
    /// </summary>
    Task RemoveExcludedProductsAsync(int promotionRuleId, IList<int> productIds);

    /// <summary>
    /// Deletes all excluded products for a promotion rule
    /// </summary>
    Task DeleteExcludedProductsByRuleIdAsync(int promotionRuleId);

    /// <summary>
    /// Gets paginated list of excluded products for a promotion rule
    /// </summary>
    Task<IPagedList<PromotionRuleExcludedProduct>> GetExcludedProductsPagedAsync(
        int promotionRuleId,
        int pageIndex = 0,
        int pageSize = int.MaxValue);
}
