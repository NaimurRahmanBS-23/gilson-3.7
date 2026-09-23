using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache
{
    public class PromotionRuleConditionCacheEventConsumer :
        IConsumer<EntityInserted<PromotionRuleCondition>>,
        IConsumer<EntityUpdated<PromotionRuleCondition>>,
        IConsumer<EntityDeleted<PromotionRuleCondition>>
    {
        private readonly ICacheManager _cacheManager;

        public PromotionRuleConditionCacheEventConsumer(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void HandleEvent(EntityInserted<PromotionRuleCondition> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityUpdated<PromotionRuleCondition> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityDeleted<PromotionRuleCondition> eventMessage)
        {
            ClearCache();
        }

        private void ClearCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKeyPrefix);
        }
    }
}
