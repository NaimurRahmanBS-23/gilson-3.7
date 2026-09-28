using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Services.Caching;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;

public class PromotionRuleConditionCacheEventConsumer :
    IConsumer<EntityInsertedEvent<PromotionRuleCondition>>,
    IConsumer<EntityUpdatedEvent<PromotionRuleCondition>>,
    IConsumer<EntityDeletedEvent<PromotionRuleCondition>>
{
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;

    public PromotionRuleConditionCacheEventConsumer(
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager)
    {
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<PromotionRuleCondition> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<PromotionRuleCondition> eventMessage)
    {
        await ClearCacheAsync();
    }

    public async Task HandleEventAsync(EntityDeletedEvent<PromotionRuleCondition> eventMessage)
    {
        await ClearCacheAsync();
    }

    private async Task ClearCacheAsync()
    {
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKeyPrefix);
        _shortTermCacheManager.RemoveByPrefix(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKeyPrefix);

        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKeyPrefix);
        await _staticCacheManager.RemoveByPrefixAsync(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKeyPrefix);
    }
}
