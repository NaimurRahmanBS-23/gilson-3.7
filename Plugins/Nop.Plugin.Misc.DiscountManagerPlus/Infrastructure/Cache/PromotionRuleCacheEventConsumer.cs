using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache
{
    public class PromotionRuleCacheEventConsumer :
        IConsumer<EntityInserted<PromotionRule>>,
        IConsumer<EntityUpdated<PromotionRule>>,
        IConsumer<EntityDeleted<PromotionRule>>
    {
        private readonly ICacheManager _cacheManager;

        public PromotionRuleCacheEventConsumer(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void HandleEvent(EntityInserted<PromotionRule> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityUpdated<PromotionRule> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityDeleted<PromotionRule> eventMessage)
        {
            ClearCache();
        }

        private void ClearCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.ActiveRulesCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKeyPrefix);
        }
    }
}
