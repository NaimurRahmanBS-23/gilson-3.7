using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Services.Stores;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class PromotionRuleService : IPromotionRuleService
{
    #region Fields

    private readonly IRepository<PromotionRule> _promotionRuleRepository;
    private readonly IRepository<PromotionRuleProduct> _ruleProductRepository;
    private readonly IRepository<PromotionRuleCondition> _ruleConditionRepository;
    private readonly IRepository<PromotionRuleTier> _ruleTierRepository;
    private readonly IRepository<PromotionRuleTierProductMapping> _ruleTierProductMappingRepository;
    private readonly IRepository<PromotionRuleUsage> _ruleUsageRepository;
    private readonly IRepository<PromotionSocialShareEvent> _promotionSocialShareEventRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IStoreMappingService _storeMappingService;
    private readonly ILogger _logger;

    #endregion

    #region Ctor

    public PromotionRuleService(
        IRepository<PromotionRule> promotionRuleRepository,
        IRepository<PromotionRuleProduct> ruleProductRepository,
        IRepository<PromotionRuleCondition> ruleConditionRepository,
        IRepository<PromotionRuleTier> ruleTierRepository,
        IRepository<PromotionRuleTierProductMapping> ruleTierProductMappingRepository,
        IRepository<PromotionRuleUsage> ruleUsageRepository,
        IRepository<PromotionSocialShareEvent> promotionSocialShareEventRepository,
        IRepository<Order> orderRepository,
        IStaticCacheManager staticCacheManager,
        IStoreMappingService storeMappingService,
        ILogger logger)
    {
        _promotionRuleRepository = promotionRuleRepository;
        _ruleProductRepository = ruleProductRepository;
        _ruleConditionRepository = ruleConditionRepository;
        _ruleTierRepository = ruleTierRepository;
        _ruleTierProductMappingRepository = ruleTierProductMappingRepository;
        _ruleUsageRepository = ruleUsageRepository;
        _promotionSocialShareEventRepository = promotionSocialShareEventRepository;
        _orderRepository = orderRepository;
        _staticCacheManager = staticCacheManager;
        _storeMappingService = storeMappingService;
        _logger = logger;
    }

    #endregion

    #region Utilities

    protected virtual CacheKey GetActiveRulesCacheKey(int storeId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.ActiveRulesCacheKey, storeId);

    protected virtual CacheKey GetPromotionRulesByDiscountCacheKey(int discountId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKey, discountId);

    protected virtual CacheKey GetSuppressingPromotionRulesByDiscountCacheKey(int discountId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKey, discountId);

    protected virtual CacheKey GetRuleProductsByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKey, promotionRuleId);

    protected virtual CacheKey GetRuleConditionsByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKey, promotionRuleId);

    protected virtual CacheKey GetRuleConditionsByRequirementCacheKey(int discountRequirementId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKey, discountRequirementId);

    protected virtual CacheKey GetRuleTiersByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKey, promotionRuleId);

    protected virtual CacheKey GetRuleTierMappingsByTierCacheKey(int ruleTierId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKey, ruleTierId);

    protected virtual CacheKey GetRuleTierMappingsByRuleCacheKey(int promotionRuleId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKey, promotionRuleId);

    protected virtual CacheKey GetMappedRuleProductIdsByTierCacheKey(int ruleTierId) =>
        _staticCacheManager.PrepareKeyForDefaultCache(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKey, ruleTierId);

    protected virtual async Task ClearPromotionRuleCacheAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.ActiveRulesCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKeyPrefix);
    }

    protected virtual async Task ClearRuleProductCacheAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
    }

    protected virtual async Task ClearRuleConditionCacheAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKeyPrefix);
    }

    protected virtual async Task ClearRuleTierCacheAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
    }

    protected virtual async Task<IQueryable<PromotionRule>> ApplyStoreScopeAsync(IQueryable<PromotionRule> query, int storeId)
    {
        if (storeId <= 0)
            return query;

        query = query.Where(x => x.LimitedToStores || x.LimitedToStore == 0 || x.LimitedToStore == storeId);
        return await _storeMappingService.ApplyStoreMapping(query, storeId);
    }

    #endregion

    #region Methods

    public async Task<PromotionRule> GetPromotionRuleByIdAsync(int promotionRuleId)
    {
        if (promotionRuleId <= 0)
            return null;

        return await _promotionRuleRepository.GetByIdAsync(promotionRuleId);
    }

    public async Task<IPagedList<PromotionRule>> GetAllPromotionRulesAsync(
        string name = null,
        int? ruleTypeId = null,
        bool? isActive = null,
        int discountId = 0,
        int storeId = 0,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        return await _promotionRuleRepository.GetAllPagedAsync(async query =>
        {
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(x => x.Name.Contains(name));

            if (ruleTypeId.HasValue)
                query = query.Where(x => x.RuleTypeId == ruleTypeId.Value);

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            if (discountId > 0)
                query = query.Where(x => x.DiscountId == discountId || x.LinkedDiscountId == discountId);

            query = await ApplyStoreScopeAsync(query, storeId);

            query = query.OrderBy(x => x.Priority).ThenByDescending(x => x.Id);
            return query;
        }, pageIndex, pageSize);
    }

    public async Task<IList<PromotionRule>> GetActiveRulesAsync(int storeId = 0)
    {
        var cacheKey = GetActiveRulesCacheKey(storeId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            return await _promotionRuleRepository.GetAllAsync(async query =>
            {
                query = query.Where(x => x.IsActive);
                query = query.Where(x => !x.StartDateUtc.HasValue || x.StartDateUtc.Value <= now);
                query = query.Where(x => !x.EndDateUtc.HasValue || x.EndDateUtc.Value >= now);
                query = await ApplyStoreScopeAsync(query, storeId);

                query = query.OrderBy(x => x.Priority);
                return query;
            });
        });
    }

    public async Task<IList<PromotionRule>> GetPromotionRulesByDiscountIdAsync(int discountId)
    {
        if (discountId <= 0)
            return new List<PromotionRule>();

        return await _staticCacheManager.GetAsync(GetPromotionRulesByDiscountCacheKey(discountId), async () =>
            await _promotionRuleRepository.GetAllAsync(query =>
                query.Where(x => x.DiscountId == discountId || x.LinkedDiscountId == discountId)
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.Id)));
    }

    public async Task<IList<PromotionRule>> GetSuppressingPromotionRulesByDiscountIdAsync(int discountId)
    {
        if (discountId <= 0)
            return new List<PromotionRule>();

        return await _staticCacheManager.GetAsync(GetSuppressingPromotionRulesByDiscountCacheKey(discountId), async () =>
            await _promotionRuleRepository.GetAllAsync(query =>
                query.Where(x => (x.DiscountId == discountId || x.LinkedDiscountId == discountId) && !x.CarryDefaultDiscount)
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.Id)));
    }

    public async Task<IList<PromotionRule>> GetPromotionRulesByLinkedDiscountIdAsync(int linkedDiscountId)
    {
        return await GetPromotionRulesByDiscountIdAsync(linkedDiscountId);
    }

    public async Task<IList<PromotionRule>> GetSuppressingPromotionRulesByLinkedDiscountIdAsync(int linkedDiscountId)
    {
        return await GetSuppressingPromotionRulesByDiscountIdAsync(linkedDiscountId);
    }

    public async Task InsertPromotionRuleAsync(PromotionRule promotionRule)
    {
        ArgumentNullException.ThrowIfNull(promotionRule);

        promotionRule.CreatedOnUtc = DateTime.UtcNow;
        promotionRule.UpdatedOnUtc = DateTime.UtcNow;

        await _promotionRuleRepository.InsertAsync(promotionRule);
        await ClearPromotionRuleCacheAsync();
    }

    public async Task UpdatePromotionRuleAsync(PromotionRule promotionRule)
    {
        ArgumentNullException.ThrowIfNull(promotionRule);

        promotionRule.UpdatedOnUtc = DateTime.UtcNow;

        await _promotionRuleRepository.UpdateAsync(promotionRule);
        await ClearPromotionRuleCacheAsync();
    }

    public async Task DeletePromotionRuleAsync(PromotionRule promotionRule)
    {
        ArgumentNullException.ThrowIfNull(promotionRule);

        await _promotionRuleRepository.DeleteAsync(promotionRule);
        await ClearPromotionRuleCacheAsync();
    }

    public async Task<IList<PromotionRuleProduct>> GetRuleProductsByRuleIdAsync(int promotionRuleId)
    {
        if (promotionRuleId <= 0)
            return new List<PromotionRuleProduct>();

        return await _staticCacheManager.GetAsync(GetRuleProductsByRuleCacheKey(promotionRuleId), async () =>
            await _ruleProductRepository.GetAllAsync(query =>
                query.Where(x => x.PromotionRuleId == promotionRuleId)));
    }

    public async Task<PromotionRuleProduct> GetRuleProductByIdAsync(int ruleProductId)
    {
        if (ruleProductId <= 0)
            return null;

        return await _ruleProductRepository.GetByIdAsync(ruleProductId);
    }

    public async Task InsertRuleProductAsync(PromotionRuleProduct ruleProduct)
    {
        ArgumentNullException.ThrowIfNull(ruleProduct);
        await _ruleProductRepository.InsertAsync(ruleProduct);
        await ClearRuleProductCacheAsync();
    }

    public async Task UpdateRuleProductAsync(PromotionRuleProduct ruleProduct)
    {
        ArgumentNullException.ThrowIfNull(ruleProduct);
        await _ruleProductRepository.UpdateAsync(ruleProduct);
        await ClearRuleProductCacheAsync();
    }

    public async Task DeleteRuleProductAsync(PromotionRuleProduct ruleProduct)
    {
        ArgumentNullException.ThrowIfNull(ruleProduct);

        var mappings = await _ruleTierProductMappingRepository.GetAllAsync(query =>
            query.Where(x => x.PromotionRuleProductId == ruleProduct.Id));
        if (mappings.Any())
            await _ruleTierProductMappingRepository.DeleteAsync(mappings);

        await _ruleProductRepository.DeleteAsync(ruleProduct);
        await ClearRuleProductCacheAsync();
    }

    public async Task<IList<PromotionRuleCondition>> GetRuleConditionsByRuleIdAsync(int promotionRuleId)
    {
        if (promotionRuleId <= 0)
            return new List<PromotionRuleCondition>();

        return await _staticCacheManager.GetAsync(GetRuleConditionsByRuleCacheKey(promotionRuleId), async () =>
            await _ruleConditionRepository.GetAllAsync(query =>
                query.Where(x => x.PromotionRuleId == promotionRuleId)));
    }

    public async Task<IList<PromotionRuleCondition>> GetRuleConditionsByDiscountRequirementIdAsync(int discountRequirementId)
    {
        if (discountRequirementId <= 0)
            return new List<PromotionRuleCondition>();

        return await _staticCacheManager.GetAsync(GetRuleConditionsByRequirementCacheKey(discountRequirementId), async () =>
            await _ruleConditionRepository.GetAllAsync(query =>
                query.Where(x => x.DiscountRequirementId == discountRequirementId)));
    }

    public async Task<PromotionRuleCondition> GetRuleConditionByIdAsync(int ruleConditionId)
    {
        if (ruleConditionId <= 0)
            return null;

        return await _ruleConditionRepository.GetByIdAsync(ruleConditionId);
    }

    public async Task InsertRuleConditionAsync(PromotionRuleCondition ruleCondition)
    {
        ArgumentNullException.ThrowIfNull(ruleCondition);
        await _ruleConditionRepository.InsertAsync(ruleCondition);
        await ClearRuleConditionCacheAsync();
    }

    public async Task UpdateRuleConditionAsync(PromotionRuleCondition ruleCondition)
    {
        ArgumentNullException.ThrowIfNull(ruleCondition);
        await _ruleConditionRepository.UpdateAsync(ruleCondition);
        await ClearRuleConditionCacheAsync();
    }

    public async Task DeleteRuleConditionAsync(PromotionRuleCondition ruleCondition)
    {
        ArgumentNullException.ThrowIfNull(ruleCondition);
        await _ruleConditionRepository.DeleteAsync(ruleCondition);
        await ClearRuleConditionCacheAsync();
    }

    public async Task<IList<PromotionRuleTier>> GetRuleTiersByRuleIdAsync(int promotionRuleId)
    {
        if (promotionRuleId <= 0)
            return new List<PromotionRuleTier>();

        return await _staticCacheManager.GetAsync(GetRuleTiersByRuleCacheKey(promotionRuleId), async () =>
            await _ruleTierRepository.GetAllAsync(query =>
                query.Where(x => x.PromotionRuleId == promotionRuleId).OrderBy(x => x.MinQuantity)));
    }

    public async Task<PromotionRuleTier> GetRuleTierByIdAsync(int ruleTierId)
    {
        if (ruleTierId <= 0)
            return null;

        return await _ruleTierRepository.GetByIdAsync(ruleTierId);
    }

    public async Task InsertRuleTierAsync(PromotionRuleTier ruleTier)
    {
        ArgumentNullException.ThrowIfNull(ruleTier);
        await _ruleTierRepository.InsertAsync(ruleTier);
        await ClearRuleTierCacheAsync();
    }

    public async Task UpdateRuleTierAsync(PromotionRuleTier ruleTier)
    {
        ArgumentNullException.ThrowIfNull(ruleTier);
        await _ruleTierRepository.UpdateAsync(ruleTier);
        await ClearRuleTierCacheAsync();
    }

    public async Task DeleteRuleTierAsync(PromotionRuleTier ruleTier)
    {
        ArgumentNullException.ThrowIfNull(ruleTier);

        var mappings = await _ruleTierProductMappingRepository.GetAllAsync(query =>
            query.Where(x => x.PromotionRuleTierId == ruleTier.Id));
        if (mappings.Any())
            await _ruleTierProductMappingRepository.DeleteAsync(mappings);

        await _ruleTierRepository.DeleteAsync(ruleTier);
        await ClearRuleTierCacheAsync();
    }

    public async Task<IList<PromotionRuleTierProductMapping>> GetRuleTierProductMappingsByTierIdAsync(int ruleTierId)
    {
        if (ruleTierId <= 0)
            return new List<PromotionRuleTierProductMapping>();

        return await _staticCacheManager.GetAsync(GetRuleTierMappingsByTierCacheKey(ruleTierId), async () =>
            await _ruleTierProductMappingRepository.GetAllAsync(query =>
                query.Where(x => x.PromotionRuleTierId == ruleTierId)));
    }

    public async Task<IList<PromotionRuleTierProductMapping>> GetRuleTierProductMappingsByRuleIdAsync(int promotionRuleId)
    {
        if (promotionRuleId <= 0)
            return new List<PromotionRuleTierProductMapping>();

        return await _staticCacheManager.GetAsync(GetRuleTierMappingsByRuleCacheKey(promotionRuleId), async () =>
            await _ruleTierProductMappingRepository.GetAllAsync(query =>
                query.Where(x => x.PromotionRuleId == promotionRuleId)));
    }

    public async Task<IList<int>> GetMappedRuleProductIdsByTierIdAsync(int ruleTierId)
    {
        if (ruleTierId <= 0)
            return new List<int>();

        return await _staticCacheManager.GetAsync(GetMappedRuleProductIdsByTierCacheKey(ruleTierId), async () =>
            await _ruleTierProductMappingRepository.Table
                .Where(x => x.PromotionRuleTierId == ruleTierId)
                .Select(x => x.PromotionRuleProductId)
                .Distinct()
                .ToListAsync());
    }

    public async Task<IList<int>> GetMappedTierIdsByRuleProductsAsync(int promotionRuleId, IList<int> ruleProductIds)
    {
        if (promotionRuleId <= 0 || ruleProductIds == null || !ruleProductIds.Any())
            return new List<int>();

        var sanitizedRuleProductIds = ruleProductIds.Where(x => x > 0).Distinct().ToList();
        if (!sanitizedRuleProductIds.Any())
            return new List<int>();

        return await _ruleTierProductMappingRepository.Table
            .Where(x => x.PromotionRuleId == promotionRuleId && sanitizedRuleProductIds.Contains(x.PromotionRuleProductId))
            .Select(x => x.PromotionRuleTierId)
            .Distinct()
            .ToListAsync();
    }

    public async Task SaveRuleTierProductMappingsAsync(int promotionRuleId, int ruleTierId, IList<int> ruleProductIds)
    {
        if (promotionRuleId <= 0 || ruleTierId <= 0)
            return;

        var existingMappings = await _ruleTierProductMappingRepository.GetAllAsync(query =>
            query.Where(x => x.PromotionRuleId == promotionRuleId && x.PromotionRuleTierId == ruleTierId));
        if (existingMappings.Any())
            await _ruleTierProductMappingRepository.DeleteAsync(existingMappings);

        if (ruleProductIds == null || !ruleProductIds.Any())
        {
            await ClearRuleTierCacheAsync();
            return;
        }

        var sanitizedRuleProductIds = ruleProductIds.Where(x => x > 0).Distinct().ToList();
        if (!sanitizedRuleProductIds.Any())
        {
            await ClearRuleTierCacheAsync();
            return;
        }

        var validRuleProductIds = await _ruleProductRepository.Table
            .Where(x => x.PromotionRuleId == promotionRuleId && sanitizedRuleProductIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        if (!validRuleProductIds.Any())
        {
            await ClearRuleTierCacheAsync();
            return;
        }

        var mappingsToInsert = validRuleProductIds
            .Select(ruleProductId => new PromotionRuleTierProductMapping
            {
                PromotionRuleId = promotionRuleId,
                PromotionRuleTierId = ruleTierId,
                PromotionRuleProductId = ruleProductId
            })
            .ToList();

        await _ruleTierProductMappingRepository.InsertAsync(mappingsToInsert);
        await ClearRuleTierCacheAsync();
    }

    public async Task InsertRuleUsageAsync(PromotionRuleUsage usage)
    {
        ArgumentNullException.ThrowIfNull(usage);
        usage.CreatedOnUtc = DateTime.UtcNow;
        await _ruleUsageRepository.InsertAsync(usage);
    }

    public async Task<int> GetRuleUsageCountAsync(int promotionRuleId, DateTime? fromUtc = null, int? customerId = null)
    {
        if (promotionRuleId <= 0)
            return 0;

        var query = _ruleUsageRepository.Table.Where(x => x.PromotionRuleId == promotionRuleId);

        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedOnUtc >= fromUtc.Value);

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(x => x.CustomerId == customerId.Value);

        return await query.CountAsync();
    }

    public async Task InsertPromotionSocialShareEventAsync(PromotionSocialShareEvent socialShareEvent)
    {
        ArgumentNullException.ThrowIfNull(socialShareEvent);
        socialShareEvent.CreatedOnUtc = DateTime.UtcNow;
        await _promotionSocialShareEventRepository.InsertAsync(socialShareEvent);
    }

    public async Task<PromotionSocialShareEvent> GetValidPromotionSocialShareEventAsync(int customerId, int storeId, int ruleId, string tokenHash)
    {
        if (customerId <= 0 || storeId < 0 || ruleId <= 0 || string.IsNullOrWhiteSpace(tokenHash))
            return null;

        return await _promotionSocialShareEventRepository.Table
            .Where(x => !x.IsConsumed &&
                        x.CustomerId == customerId &&
                        x.StoreId == storeId &&
                        x.RuleId == ruleId &&
                        x.TokenHash == tokenHash)
            .OrderByDescending(x => x.CreatedOnUtc)
            .FirstOrDefaultAsync();
    }

    public async Task ConsumePromotionSocialShareEventAsync(PromotionSocialShareEvent socialShareEvent)
    {
        ArgumentNullException.ThrowIfNull(socialShareEvent);
        if (socialShareEvent.IsConsumed)
            return;

        socialShareEvent.IsConsumed = true;
        await _promotionSocialShareEventRepository.UpdateAsync(socialShareEvent);
    }

    public async Task<IPagedList<PromotionRuleUsage>> GetRuleUsagesAsync(
        int? promotionRuleId = null,
        int? orderId = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        return await _ruleUsageRepository.GetAllPagedAsync(query =>
        {
            if (promotionRuleId.HasValue)
                query = query.Where(x => x.PromotionRuleId == promotionRuleId.Value);

            if (orderId.HasValue)
                query = query.Where(x => x.OrderId == orderId.Value);

            query = query.OrderByDescending(x => x.CreatedOnUtc);
            return query;
        }, pageIndex, pageSize);
    }

    public async Task<IPagedList<PromotionRuleAnalyticsSummary>> SearchPromotionRuleAnalyticsAsync(
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int storeId = 0,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = from usage in _ruleUsageRepository.Table
                    join rule in _promotionRuleRepository.Table on usage.PromotionRuleId equals rule.Id
                    join order in _orderRepository.Table on usage.OrderId equals order.Id
                    where !order.Deleted
                    select new { usage, rule, order };

        if (createdFromUtc.HasValue)
            query = query.Where(x => x.usage.CreatedOnUtc >= createdFromUtc.Value);

        if (createdToUtc.HasValue)
            query = query.Where(x => x.usage.CreatedOnUtc <= createdToUtc.Value);

        if (storeId > 0)
            query = query.Where(x => x.order.StoreId == storeId);

        var analyticsQuery = query
            .GroupBy(x => new
            {
                x.rule.Id,
                x.rule.Name,
                x.rule.RuleTypeId,
                x.rule.IsActive
            })
            .Select(group => new PromotionRuleAnalyticsSummary
            {
                PromotionRuleId = group.Key.Id,
                RuleName = group.Key.Name,
                RuleTypeId = group.Key.RuleTypeId,
                IsActive = group.Key.IsActive,
                UsageCount = group.Count(),
                ImpactedOrdersCount = group.Select(x => x.usage.OrderId).Distinct().Count(),
                ImpactedCustomersCount = group.Select(x => x.usage.CustomerId).Distinct().Count(),
                TotalDiscountAmount = group.Sum(x => x.usage.DiscountAmountApplied),
                TotalRevenueAmount = group.Sum(x => x.order.OrderTotal)
            })
            .OrderByDescending(x => x.TotalDiscountAmount)
            .ThenByDescending(x => x.UsageCount);

        return await analyticsQuery.ToPagedListAsync(pageIndex, pageSize);
    }

    public async Task<PromotionRuleAnalyticsTotals> GetPromotionRuleAnalyticsTotalsAsync(
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int storeId = 0)
    {
        var query = from usage in _ruleUsageRepository.Table
                    join order in _orderRepository.Table on usage.OrderId equals order.Id
                    where !order.Deleted
                    select new { usage, order };

        if (createdFromUtc.HasValue)
            query = query.Where(x => x.usage.CreatedOnUtc >= createdFromUtc.Value);

        if (createdToUtc.HasValue)
            query = query.Where(x => x.usage.CreatedOnUtc <= createdToUtc.Value);

        if (storeId > 0)
            query = query.Where(x => x.order.StoreId == storeId);

        var usageCount = await query.CountAsync();
        if (usageCount <= 0)
            return new PromotionRuleAnalyticsTotals();

        var totalDiscountAmount = await query.SumAsync(x => (decimal?)x.usage.DiscountAmountApplied) ?? 0m;
        var impactedOrdersCount = await query.Select(x => x.usage.OrderId).Distinct().CountAsync();
        var impactedCustomersCount = await query.Select(x => x.usage.CustomerId).Distinct().CountAsync();
        var totalRevenueAmount = await query.SumAsync(x => (decimal?)x.order.OrderTotal) ?? 0m;

        return new PromotionRuleAnalyticsTotals
        {
            UsageCount = usageCount,
            ImpactedOrdersCount = impactedOrdersCount,
            ImpactedCustomersCount = impactedCustomersCount,
            TotalDiscountAmount = totalDiscountAmount,
            TotalRevenueAmount = totalRevenueAmount
        };
    }

    #endregion
}
