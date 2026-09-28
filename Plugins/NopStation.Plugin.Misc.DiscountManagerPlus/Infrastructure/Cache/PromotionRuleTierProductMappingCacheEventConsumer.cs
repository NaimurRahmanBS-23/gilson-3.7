using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Services.Caching;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;

public class PromotionRuleTierProductMappingCacheEventConsumer :
    IConsumer<EntityInsertedEvent<PromotionRuleTierProductMapping>>,
    IConsumer<EntityUpdatedEvent<PromotionRuleTierProductMapping>>,
    IConsumer<EntityDeletedEvent<PromotionRuleTierProductMapping>>
{
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;

    public PromotionRuleTierProductMappingCacheEventConsumer(
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager)
    {
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<PromotionRuleTierProductMapping> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<PromotionRuleTierProductMapping> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityDeletedEvent<PromotionRuleTierProductMapping> eventMessage)
    {
        await ClearCacheAsync();
    }

    private async Task ClearCacheAsync()
    {
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);

        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
    }
}
