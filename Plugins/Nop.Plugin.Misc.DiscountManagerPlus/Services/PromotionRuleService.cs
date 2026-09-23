using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionRuleService : IPromotionRuleService
    {
        private readonly IRepository<PromotionRule> _promotionRuleRepository;
        private readonly IRepository<PromotionRuleProduct> _ruleProductRepository;
        private readonly IRepository<PromotionRuleCondition> _ruleConditionRepository;
        private readonly IRepository<PromotionRuleTier> _ruleTierRepository;
        private readonly IRepository<PromotionRuleTierProductMapping> _ruleTierProductMappingRepository;
        private readonly IRepository<PromotionRuleUsage> _ruleUsageRepository;
        private readonly IRepository<PromotionSocialShareEvent> _promotionSocialShareEventRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICacheManager _cacheManager;
        private readonly IStoreMappingService _storeMappingService;

        public PromotionRuleService(
            IRepository<PromotionRule> promotionRuleRepository,
            IRepository<PromotionRuleProduct> ruleProductRepository,
            IRepository<PromotionRuleCondition> ruleConditionRepository,
            IRepository<PromotionRuleTier> ruleTierRepository,
            IRepository<PromotionRuleTierProductMapping> ruleTierProductMappingRepository,
            IRepository<PromotionRuleUsage> ruleUsageRepository,
            IRepository<PromotionSocialShareEvent> promotionSocialShareEventRepository,
            IRepository<Order> orderRepository,
            ICacheManager cacheManager,
            IStoreMappingService storeMappingService)
        {
            _promotionRuleRepository = promotionRuleRepository;
            _ruleProductRepository = ruleProductRepository;
            _ruleConditionRepository = ruleConditionRepository;
            _ruleTierRepository = ruleTierRepository;
            _ruleTierProductMappingRepository = ruleTierProductMappingRepository;
            _ruleUsageRepository = ruleUsageRepository;
            _promotionSocialShareEventRepository = promotionSocialShareEventRepository;
            _orderRepository = orderRepository;
            _cacheManager = cacheManager;
            _storeMappingService = storeMappingService;
        }

        protected virtual void ClearPromotionRuleCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.ActiveRulesCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKeyPrefix);
        }

        protected virtual void ClearRuleProductCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
        }

        protected virtual void ClearRuleConditionCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKeyPrefix);
        }

        protected virtual void ClearRuleTierCache()
        {
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKeyPrefix);
            _cacheManager.RemoveByPattern(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKeyPrefix);
        }

        protected virtual IQueryable<PromotionRule> ApplyStoreScope(IQueryable<PromotionRule> query, int storeId)
        {
            if (storeId <= 0)
                return query;

            return query.Where(x => !x.LimitedToStores || x.LimitedToStore == 0 || x.LimitedToStore == storeId);
        }

        protected virtual IList<PromotionRule> FilterAuthorizedRules(IList<PromotionRule> rules, int storeId)
        {
            if (storeId <= 0 || rules == null || !rules.Any())
                return rules ?? new List<PromotionRule>();

            return rules.Where(x => !x.LimitedToStores || _storeMappingService.Authorize(x, storeId)).ToList();
        }

        public PromotionRule GetPromotionRuleById(int promotionRuleId)
        {
            if (promotionRuleId <= 0)
                return null;

            return _promotionRuleRepository.GetById(promotionRuleId);
        }

        public IPagedList<PromotionRule> GetAllPromotionRules(
            string name = null,
            int? ruleTypeId = null,
            bool? isActive = null,
            int discountId = 0,
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var query = _promotionRuleRepository.Table;

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(x => x.Name.Contains(name));

            if (ruleTypeId.HasValue)
                query = query.Where(x => x.RuleTypeId == ruleTypeId.Value);

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            if (discountId > 0)
                query = query.Where(x => x.DiscountId == discountId || x.LinkedDiscountId == discountId);

            query = ApplyStoreScope(query, storeId);
            query = query.OrderBy(x => x.Priority).ThenByDescending(x => x.Id);

            var paged = new PagedList<PromotionRule>(query, pageIndex, pageSize);
            var authorized = FilterAuthorizedRules(paged, storeId);
            if (ReferenceEquals(authorized, paged))
                return paged;

            return new PagedList<PromotionRule>(authorized, pageIndex, pageSize);
        }

        public IList<PromotionRule> GetActiveRules(int storeId = 0)
        {
            var cacheKey = string.Format(DiscountManagerPlusDefaults.ActiveRulesCacheKey, storeId);
            return _cacheManager.Get(cacheKey, () =>
            {
                var now = DateTime.UtcNow;
                var query = _promotionRuleRepository.Table
                    .Where(x => x.IsActive)
                    .Where(x => !x.StartDateUtc.HasValue || x.StartDateUtc.Value <= now)
                    .Where(x => !x.EndDateUtc.HasValue || x.EndDateUtc.Value >= now);

                query = ApplyStoreScope(query, storeId);
                query = query.OrderBy(x => x.Priority);
                return FilterAuthorizedRules(query.ToList(), storeId);
            });
        }

        public IList<PromotionRule> GetPromotionRulesByDiscountId(int discountId)
        {
            if (discountId <= 0)
                return new List<PromotionRule>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.PromotionRulesByDiscountCacheKey, discountId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _promotionRuleRepository.Table
                    .Where(x => x.DiscountId == discountId || x.LinkedDiscountId == discountId)
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.Id)
                    .ToList();
            });
        }

        public IList<PromotionRule> GetSuppressingPromotionRulesByDiscountId(int discountId)
        {
            if (discountId <= 0)
                return new List<PromotionRule>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.SuppressingPromotionRulesByDiscountCacheKey, discountId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _promotionRuleRepository.Table
                    .Where(x => (x.DiscountId == discountId || x.LinkedDiscountId == discountId) && !x.CarryDefaultDiscount)
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.Id)
                    .ToList();
            });
        }

        public IList<PromotionRule> GetPromotionRulesByLinkedDiscountId(int linkedDiscountId)
        {
            return GetPromotionRulesByDiscountId(linkedDiscountId);
        }

        public IList<PromotionRule> GetSuppressingPromotionRulesByLinkedDiscountId(int linkedDiscountId)
        {
            return GetSuppressingPromotionRulesByDiscountId(linkedDiscountId);
        }

        public void InsertPromotionRule(PromotionRule promotionRule)
        {
            if (promotionRule == null)
                throw new ArgumentNullException("promotionRule");

            if (string.IsNullOrWhiteSpace(promotionRule.Name))
                promotionRule.Name = "Promotion rule";
            if (string.IsNullOrWhiteSpace(promotionRule.SystemName))
                promotionRule.SystemName = "rule-" + Guid.NewGuid().ToString("N");

            promotionRule.CreatedOnUtc = DateTime.UtcNow;
            promotionRule.UpdatedOnUtc = DateTime.UtcNow;

            _promotionRuleRepository.Insert(promotionRule);
            ClearPromotionRuleCache();
        }

        public void UpdatePromotionRule(PromotionRule promotionRule)
        {
            if (promotionRule == null)
                throw new ArgumentNullException("promotionRule");

            if (string.IsNullOrWhiteSpace(promotionRule.Name))
                promotionRule.Name = "Promotion rule";
            if (string.IsNullOrWhiteSpace(promotionRule.SystemName))
                promotionRule.SystemName = "rule-" + Guid.NewGuid().ToString("N");

            promotionRule.UpdatedOnUtc = DateTime.UtcNow;

            _promotionRuleRepository.Update(promotionRule);
            ClearPromotionRuleCache();
        }

        public void DeletePromotionRule(PromotionRule promotionRule)
        {
            if (promotionRule == null)
                throw new ArgumentNullException("promotionRule");

            _promotionRuleRepository.Delete(promotionRule);
            ClearPromotionRuleCache();
        }

        public IList<PromotionRuleProduct> GetRuleProductsByRuleId(int promotionRuleId)
        {
            if (promotionRuleId <= 0)
                return new List<PromotionRuleProduct>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleProductsByRuleCacheKey, promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleProductRepository.Table
                    .Where(x => x.PromotionRuleId == promotionRuleId)
                    .ToList();
            });
        }

        public PromotionRuleProduct GetRuleProductById(int ruleProductId)
        {
            if (ruleProductId <= 0)
                return null;

            return _ruleProductRepository.GetById(ruleProductId);
        }

        public void InsertRuleProduct(PromotionRuleProduct ruleProduct)
        {
            if (ruleProduct == null)
                throw new ArgumentNullException("ruleProduct");
            _ruleProductRepository.Insert(ruleProduct);
            ClearRuleProductCache();
        }

        public void UpdateRuleProduct(PromotionRuleProduct ruleProduct)
        {
            if (ruleProduct == null)
                throw new ArgumentNullException("ruleProduct");
            _ruleProductRepository.Update(ruleProduct);
            ClearRuleProductCache();
        }

        public void DeleteRuleProduct(PromotionRuleProduct ruleProduct)
        {
            if (ruleProduct == null)
                throw new ArgumentNullException("ruleProduct");

            var mappings = _ruleTierProductMappingRepository.Table
                .Where(x => x.PromotionRuleProductId == ruleProduct.Id)
                .ToList();
            if (mappings.Any())
                _ruleTierProductMappingRepository.Delete(mappings);

            _ruleProductRepository.Delete(ruleProduct);
            ClearRuleProductCache();
        }

        public IList<PromotionRuleCondition> GetRuleConditionsByRuleId(int promotionRuleId)
        {
            if (promotionRuleId <= 0)
                return new List<PromotionRuleCondition>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleConditionsByRuleCacheKey, promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleConditionRepository.Table
                    .Where(x => x.PromotionRuleId == promotionRuleId)
                    .ToList();
            });
        }

        public IList<PromotionRuleCondition> GetRuleConditionsByDiscountRequirementId(int discountRequirementId)
        {
            if (discountRequirementId <= 0)
                return new List<PromotionRuleCondition>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleConditionsByRequirementCacheKey, discountRequirementId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleConditionRepository.Table
                    .Where(x => x.DiscountRequirementId == discountRequirementId)
                    .ToList();
            });
        }

        public PromotionRuleCondition GetRuleConditionById(int ruleConditionId)
        {
            if (ruleConditionId <= 0)
                return null;

            return _ruleConditionRepository.GetById(ruleConditionId);
        }

        public void InsertRuleCondition(PromotionRuleCondition ruleCondition)
        {
            if (ruleCondition == null)
                throw new ArgumentNullException("ruleCondition");
            _ruleConditionRepository.Insert(ruleCondition);
            ClearRuleConditionCache();
        }

        public void UpdateRuleCondition(PromotionRuleCondition ruleCondition)
        {
            if (ruleCondition == null)
                throw new ArgumentNullException("ruleCondition");
            _ruleConditionRepository.Update(ruleCondition);
            ClearRuleConditionCache();
        }

        public void DeleteRuleCondition(PromotionRuleCondition ruleCondition)
        {
            if (ruleCondition == null)
                throw new ArgumentNullException("ruleCondition");
            _ruleConditionRepository.Delete(ruleCondition);
            ClearRuleConditionCache();
        }

        public IList<PromotionRuleTier> GetRuleTiersByRuleId(int promotionRuleId)
        {
            if (promotionRuleId <= 0)
                return new List<PromotionRuleTier>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleTiersByRuleCacheKey, promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleTierRepository.Table
                    .Where(x => x.PromotionRuleId == promotionRuleId)
                    .OrderBy(x => x.MinQuantity)
                    .ToList();
            });
        }

        public PromotionRuleTier GetRuleTierById(int ruleTierId)
        {
            if (ruleTierId <= 0)
                return null;

            return _ruleTierRepository.GetById(ruleTierId);
        }

        public void InsertRuleTier(PromotionRuleTier ruleTier)
        {
            if (ruleTier == null)
                throw new ArgumentNullException("ruleTier");
            _ruleTierRepository.Insert(ruleTier);
            ClearRuleTierCache();
        }

        public void UpdateRuleTier(PromotionRuleTier ruleTier)
        {
            if (ruleTier == null)
                throw new ArgumentNullException("ruleTier");
            _ruleTierRepository.Update(ruleTier);
            ClearRuleTierCache();
        }

        public void DeleteRuleTier(PromotionRuleTier ruleTier)
        {
            if (ruleTier == null)
                throw new ArgumentNullException("ruleTier");

            var mappings = _ruleTierProductMappingRepository.Table
                .Where(x => x.PromotionRuleTierId == ruleTier.Id)
                .ToList();
            if (mappings.Any())
                _ruleTierProductMappingRepository.Delete(mappings);

            _ruleTierRepository.Delete(ruleTier);
            ClearRuleTierCache();
        }

        public IList<PromotionRuleTierProductMapping> GetRuleTierProductMappingsByTierId(int ruleTierId)
        {
            if (ruleTierId <= 0)
                return new List<PromotionRuleTierProductMapping>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleTierMappingsByTierCacheKey, ruleTierId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleTierProductMappingRepository.Table
                    .Where(x => x.PromotionRuleTierId == ruleTierId)
                    .ToList();
            });
        }

        public IList<PromotionRuleTierProductMapping> GetRuleTierProductMappingsByRuleId(int promotionRuleId)
        {
            if (promotionRuleId <= 0)
                return new List<PromotionRuleTierProductMapping>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.RuleTierMappingsByRuleCacheKey, promotionRuleId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleTierProductMappingRepository.Table
                    .Where(x => x.PromotionRuleId == promotionRuleId)
                    .ToList();
            });
        }

        public IList<int> GetMappedRuleProductIdsByTierId(int ruleTierId)
        {
            if (ruleTierId <= 0)
                return new List<int>();

            var cacheKey = string.Format(DiscountManagerPlusDefaults.MappedRuleProductIdsByTierCacheKey, ruleTierId);
            return _cacheManager.Get(cacheKey, () =>
            {
                return _ruleTierProductMappingRepository.Table
                    .Where(x => x.PromotionRuleTierId == ruleTierId)
                    .Select(x => x.PromotionRuleProductId)
                    .Distinct()
                    .ToList();
            });
        }

        public IList<int> GetMappedTierIdsByRuleProducts(int promotionRuleId, IList<int> ruleProductIds)
        {
            if (promotionRuleId <= 0 || ruleProductIds == null || !ruleProductIds.Any())
                return new List<int>();

            var sanitizedRuleProductIds = ruleProductIds.Where(x => x > 0).Distinct().ToList();
            if (!sanitizedRuleProductIds.Any())
                return new List<int>();

            return _ruleTierProductMappingRepository.Table
                .Where(x => x.PromotionRuleId == promotionRuleId && sanitizedRuleProductIds.Contains(x.PromotionRuleProductId))
                .Select(x => x.PromotionRuleTierId)
                .Distinct()
                .ToList();
        }

        public void SaveRuleTierProductMappings(int promotionRuleId, int ruleTierId, IList<int> ruleProductIds)
        {
            if (promotionRuleId <= 0 || ruleTierId <= 0)
                return;

            var existingMappings = _ruleTierProductMappingRepository.Table
                .Where(x => x.PromotionRuleId == promotionRuleId && x.PromotionRuleTierId == ruleTierId)
                .ToList();
            if (existingMappings.Any())
                _ruleTierProductMappingRepository.Delete(existingMappings);

            if (ruleProductIds == null || !ruleProductIds.Any())
            {
                ClearRuleTierCache();
                return;
            }

            var sanitizedRuleProductIds = ruleProductIds.Where(x => x > 0).Distinct().ToList();
            if (!sanitizedRuleProductIds.Any())
            {
                ClearRuleTierCache();
                return;
            }

            var validRuleProductIds = _ruleProductRepository.Table
                .Where(x => x.PromotionRuleId == promotionRuleId && sanitizedRuleProductIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToList();
            if (!validRuleProductIds.Any())
            {
                ClearRuleTierCache();
                return;
            }

            var mappingsToInsert = validRuleProductIds
                .Select(ruleProductId => new PromotionRuleTierProductMapping
                {
                    PromotionRuleId = promotionRuleId,
                    PromotionRuleTierId = ruleTierId,
                    PromotionRuleProductId = ruleProductId
                })
                .ToList();

            _ruleTierProductMappingRepository.Insert(mappingsToInsert);
            ClearRuleTierCache();
        }

        public void InsertRuleUsage(PromotionRuleUsage usage)
        {
            if (usage == null)
                throw new ArgumentNullException("usage");
            usage.CreatedOnUtc = DateTime.UtcNow;
            _ruleUsageRepository.Insert(usage);
        }

        public int GetRuleUsageCount(int promotionRuleId, DateTime? fromUtc = null, int? customerId = null)
        {
            if (promotionRuleId <= 0)
                return 0;

            var query = _ruleUsageRepository.Table.Where(x => x.PromotionRuleId == promotionRuleId);

            if (fromUtc.HasValue)
                query = query.Where(x => x.CreatedOnUtc >= fromUtc.Value);

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(x => x.CustomerId == customerId.Value);

            return query.Count();
        }

        public void InsertPromotionSocialShareEvent(PromotionSocialShareEvent socialShareEvent)
        {
            if (socialShareEvent == null)
                throw new ArgumentNullException("socialShareEvent");
            socialShareEvent.CreatedOnUtc = DateTime.UtcNow;
            _promotionSocialShareEventRepository.Insert(socialShareEvent);
        }

        public PromotionSocialShareEvent GetValidPromotionSocialShareEvent(int customerId, int storeId, int ruleId, string tokenHash)
        {
            if (customerId <= 0 || storeId < 0 || ruleId <= 0 || string.IsNullOrWhiteSpace(tokenHash))
                return null;

            return _promotionSocialShareEventRepository.Table
                .Where(x => !x.IsConsumed &&
                            x.CustomerId == customerId &&
                            x.StoreId == storeId &&
                            x.RuleId == ruleId &&
                            x.TokenHash == tokenHash)
                .OrderByDescending(x => x.CreatedOnUtc)
                .FirstOrDefault();
        }

        public void ConsumePromotionSocialShareEvent(PromotionSocialShareEvent socialShareEvent)
        {
            if (socialShareEvent == null)
                throw new ArgumentNullException("socialShareEvent");
            if (socialShareEvent.IsConsumed)
                return;

            socialShareEvent.IsConsumed = true;
            _promotionSocialShareEventRepository.Update(socialShareEvent);
        }

        public IPagedList<PromotionRuleUsage> GetRuleUsages(
            int? promotionRuleId = null,
            int? orderId = null,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var query = _ruleUsageRepository.Table;

            if (promotionRuleId.HasValue)
                query = query.Where(x => x.PromotionRuleId == promotionRuleId.Value);

            if (orderId.HasValue)
                query = query.Where(x => x.OrderId == orderId.Value);

            query = query.OrderByDescending(x => x.CreatedOnUtc);
            return new PagedList<PromotionRuleUsage>(query, pageIndex, pageSize);
        }

        public IPagedList<PromotionRuleAnalyticsSummary> SearchPromotionRuleAnalytics(
            DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null,
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var rows = GetAnalyticsUsageRows(createdFromUtc, createdToUtc, storeId);
            var summaries = rows
                .GroupBy(x => new
                {
                    x.PromotionRuleId,
                    x.RuleName,
                    x.RuleTypeId,
                    x.IsActive
                })
                .Select(group => new PromotionRuleAnalyticsSummary
                {
                    PromotionRuleId = group.Key.PromotionRuleId,
                    RuleName = group.Key.RuleName,
                    RuleTypeId = group.Key.RuleTypeId,
                    IsActive = group.Key.IsActive,
                    UsageCount = group.Count(),
                    ImpactedOrdersCount = group.Select(x => x.OrderId).Distinct().Count(),
                    ImpactedCustomersCount = group.Select(x => x.CustomerId).Distinct().Count(),
                    TotalDiscountAmount = group.Sum(x => x.DiscountAmountApplied),
                    TotalRevenueAmount = group.Sum(x => x.OrderTotal)
                })
                .OrderByDescending(x => x.TotalDiscountAmount)
                .ThenByDescending(x => x.UsageCount)
                .ToList();

            return new PagedList<PromotionRuleAnalyticsSummary>(summaries, pageIndex, pageSize);
        }

        public PromotionRuleAnalyticsTotals GetPromotionRuleAnalyticsTotals(
            DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null,
            int storeId = 0)
        {
            var rows = GetAnalyticsUsageRows(createdFromUtc, createdToUtc, storeId);
            if (!rows.Any())
                return new PromotionRuleAnalyticsTotals();

            return new PromotionRuleAnalyticsTotals
            {
                UsageCount = rows.Count,
                ImpactedOrdersCount = rows.Select(x => x.OrderId).Distinct().Count(),
                ImpactedCustomersCount = rows.Select(x => x.CustomerId).Distinct().Count(),
                TotalDiscountAmount = rows.Sum(x => x.DiscountAmountApplied),
                TotalRevenueAmount = rows.Sum(x => x.OrderTotal)
            };
        }

        private IList<AnalyticsUsageRow> GetAnalyticsUsageRows(DateTime? createdFromUtc, DateTime? createdToUtc, int storeId)
        {
            var usageQuery = from usage in _ruleUsageRepository.Table
                             join rule in _promotionRuleRepository.Table on usage.PromotionRuleId equals rule.Id
                             select new
                             {
                                 usage.PromotionRuleId,
                                 usage.OrderId,
                                 usage.CustomerId,
                                 usage.DiscountAmountApplied,
                                 usage.CreatedOnUtc,
                                 RuleName = rule.Name,
                                 rule.RuleTypeId,
                                 rule.IsActive
                             };

            if (createdFromUtc.HasValue)
                usageQuery = usageQuery.Where(x => x.CreatedOnUtc >= createdFromUtc.Value);

            if (createdToUtc.HasValue)
                usageQuery = usageQuery.Where(x => x.CreatedOnUtc <= createdToUtc.Value);

            var usageRows = usageQuery.ToList();
            if (!usageRows.Any())
                return new List<AnalyticsUsageRow>();

            var orders = GetOrdersByIds(usageRows.Select(x => x.OrderId).Distinct().ToList(), storeId);
            var result = new List<AnalyticsUsageRow>();
            foreach (var row in usageRows)
            {
                Order order;
                if (!orders.TryGetValue(row.OrderId, out order))
                    continue;

                result.Add(new AnalyticsUsageRow
                {
                    PromotionRuleId = row.PromotionRuleId,
                    RuleName = row.RuleName,
                    RuleTypeId = row.RuleTypeId,
                    IsActive = row.IsActive,
                    OrderId = row.OrderId,
                    CustomerId = row.CustomerId,
                    DiscountAmountApplied = row.DiscountAmountApplied,
                    OrderTotal = order.OrderTotal
                });
            }

            return result;
        }

        private IDictionary<int, Order> GetOrdersByIds(IList<int> orderIds, int storeId)
        {
            var map = new Dictionary<int, Order>();
            if (orderIds == null || !orderIds.Any())
                return map;

            const int batchSize = 1000;
            for (var i = 0; i < orderIds.Count; i += batchSize)
            {
                var batch = orderIds.Skip(i).Take(batchSize).ToList();
                var query = _orderRepository.Table.Where(o => batch.Contains(o.Id) && !o.Deleted);
                if (storeId > 0)
                    query = query.Where(o => o.StoreId == storeId);

                foreach (var order in query.ToList())
                    map[order.Id] = order;
            }

            return map;
        }

        private class AnalyticsUsageRow
        {
            public int PromotionRuleId { get; set; }
            public string RuleName { get; set; }
            public int RuleTypeId { get; set; }
            public bool IsActive { get; set; }
            public int OrderId { get; set; }
            public int CustomerId { get; set; }
            public decimal DiscountAmountApplied { get; set; }
            public decimal OrderTotal { get; set; }
        }
    }
}
