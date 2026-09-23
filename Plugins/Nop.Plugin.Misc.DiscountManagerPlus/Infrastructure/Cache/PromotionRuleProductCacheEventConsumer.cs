using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache
{
    public class PromotionRuleProductCacheEventConsumer :
        IConsumer<EntityInserted<PromotionRuleProduct>>,
        IConsumer<EntityUpdated<PromotionRuleProduct>>,
        IConsumer<EntityDeleted<PromotionRuleProduct>>
    {
        private readonly ICacheManager _cacheManager;

        public PromotionRuleProductCacheEventConsumer(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void HandleEvent(EntityInserted<PromotionRuleProduct> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityUpdated<PromotionRuleProduct> eventMessage)
        {
            ClearCache();
        }

        public void HandleEvent(EntityDeleted<PromotionRuleProduct> eventMessage)
        {
            ClearCache();
        }

        private void ClearCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
        }
    }
}
