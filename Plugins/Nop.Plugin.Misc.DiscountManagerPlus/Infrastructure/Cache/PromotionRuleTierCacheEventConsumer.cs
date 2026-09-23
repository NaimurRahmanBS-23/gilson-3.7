using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache
{
    public class PromotionRuleTierCacheEventConsumer :
        IConsumer<EntityInserted<PromotionRuleTier>>,
        IConsumer<EntityUpdated<PromotionRuleTier>>,
        IConsumer<EntityDeleted<PromotionRuleTier>>
    {
        private readonly ICacheManager _cacheManager;

        public PromotionRuleTierCacheEventConsumer(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void HandleEvent(EntityInserted<PromotionRuleTier> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityUpdated<PromotionRuleTier> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityDeleted<PromotionRuleTier> eventMessage)
        {
            ClearCache();
        }

        private void ClearCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
        }
    }
}
