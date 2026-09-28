using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Services.Caching;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;

public class PromotionRuleCacheEventConsumer :
    IConsumer<EntityInsertedEvent<PromotionRule>>,
    IConsumer<EntityUpdatedEvent<PromotionRule>>,
    IConsumer<EntityDeletedEvent<PromotionRule>>
{
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;

    public PromotionRuleCacheEventConsumer(
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager)
    {
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<PromotionRule> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<PromotionRule> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityDeletedEvent<PromotionRule> eventMessage)
    {
        await ClearCacheAsync();
    }

    private async Task ClearCacheAsync()
    {
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.ActiveRulesCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKeyPrefix);

        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.ActiveRulesCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKeyPrefix);
    }
}
