using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Services.Caching;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;

public class PromotionRuleProductCacheEventConsumer :
    IConsumer<EntityInsertedEvent<PromotionRuleProduct>>,
    IConsumer<EntityUpdatedEvent<PromotionRuleProduct>>,
    IConsumer<EntityDeletedEvent<PromotionRuleProduct>>
{
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;

    public PromotionRuleProductCacheEventConsumer(
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager)
    {
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<PromotionRuleProduct> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<PromotionRuleProduct> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityDeletedEvent<PromotionRuleProduct> eventMessage)
    {
        await ClearCacheAsync();
    }

    private async Task ClearCacheAsync()
    {
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);

        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
    }
}
