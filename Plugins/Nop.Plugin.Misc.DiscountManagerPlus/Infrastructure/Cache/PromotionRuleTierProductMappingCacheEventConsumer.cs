using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache
{
    public class PromotionRuleTierProductMappingCacheEventConsumer :
        IConsumer<EntityInserted<PromotionRuleTierProductMapping>>,
        IConsumer<EntityUpdated<PromotionRuleTierProductMapping>>,
        IConsumer<EntityDeleted<PromotionRuleTierProductMapping>>
    {
        private readonly ICacheManager _cacheManager;

        public PromotionRuleTierProductMappingCacheEventConsumer(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void HandleEvent(EntityInserted<PromotionRuleTierProductMapping> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityUpdated<PromotionRuleTierProductMapping> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityDeleted<PromotionRuleTierProductMapping> eventMessage)
        {
            ClearCache();
        }

        private void ClearCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
        }
    }
}
