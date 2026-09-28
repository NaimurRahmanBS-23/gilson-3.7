using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Services.Caching;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;

public class PromotionRuleTierCacheEventConsumer :
    IConsumer<EntityInsertedEvent<PromotionRuleTier>>,
    IConsumer<EntityUpdatedEvent<PromotionRuleTier>>,
    IConsumer<EntityDeletedEvent<PromotionRuleTier>>
{
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;

    public PromotionRuleTierCacheEventConsumer(
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager)
    {
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<PromotionRuleTier> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<PromotionRuleTier> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityDeletedEvent<PromotionRuleTier> eventMessage)
    {
        await ClearCacheAsync();
    }

    private async Task ClearCacheAsync()
    {
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);

        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
    }
}
