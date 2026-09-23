using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using PluginDiscountType = Nop.Plugin.Misc.DiscountManagerPlus.Domain.DiscountType;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{

    public class PromotionDiscountAllocator : IPromotionDiscountAllocator
    {
        private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ILogger _logger;

        public PromotionDiscountAllocator(
            IPromotionConditionEvaluator promotionConditionEvaluator,
            IPromotionRuleService promotionRuleService,
            IProductService productService,
            IDiscountService discountService,
            IPriceCalculationService priceCalculationService,
            ILogger logger)
        {
            _promotionConditionEvaluator = promotionConditionEvaluator;
            _promotionRuleService = promotionRuleService;
            _productService = productService;
            _discountService = discountService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
        }

        public decimal GetCartSubtotal(IList<ShoppingCartItem> cart)
        {
            var subtotal = 0m;
            foreach (var item in cart)
                subtotal += GetItemSubtotal(item);

            return subtotal;
        }

        public decimal GetItemsSubtotal(IList<ShoppingCartItem> items)
        {
            if (items == null || !items.Any())
                return 0m;

            var subtotal = 0m;
            foreach (var item in items)
                subtotal += GetItemSubtotal(item);

            return subtotal;
        }

        public decimal GetItemsSubtotal(IList<ShoppingCartItem> items, IDictionary<int, int> quantitiesByLineId)
        {
            int overriddenQuantity;
            if (items == null || !items.Any())
                return 0m;

            var subtotal = 0m;
            foreach (var item in items)
            {
                if (item == null)
                    continue;

                var quantity = item.Quantity;
                if (quantitiesByLineId != null)
                {
                    if (!quantitiesByLineId.TryGetValue(item.Id, out overriddenQuantity))
                        quantity = 0;
                    else
                        quantity = Math.Min(item.Quantity, Math.Max(0, overriddenQuantity));
                }

                if (quantity <= 0)
                    continue;

                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                subtotal += unitPrice * quantity;
            }

            return subtotal;
        }

        public decimal GetApplicableSubtotal(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionRuleTier tier = null)
        {
            if (rule == null || cart == null || !cart.Any())
                return 0m;

            if (rule.RuleType == PromotionRuleType.CartCondition ||
                rule.RuleType == PromotionRuleType.SubtotalBased ||
                (!PromotionRuntimeHelper.IsItemLevelRuleType(rule.RuleType) && rule.DiscountScope == DiscountScope.WholeCart))
            {
                return GetCartSubtotal(cart);
            }

            switch (rule.RuleType)
            {
                case PromotionRuleType.ProductBased:
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
                    var matchedItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
                    return GetItemsSubtotal(matchedItems);
                }

                case PromotionRuleType.ComboPricing:
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    var matchedItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, ruleProducts);
                    return GetItemsSubtotal(matchedItems);
                }

                case PromotionRuleType.BuyXGetY:
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
                    var matchedItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
                    return GetItemsSubtotal(matchedItems);
                }

                default:
                    return GetCartSubtotal(cart);
            }
        }

        public AppliedPromotion BuildAppliedPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionRuleTier tier = null,
            decimal? overrideDiscountAmount = null)
        {
            var discountType = tier != null ? tier.DiscountType : rule.DiscountType;
            var discountValue = tier != null ? tier.DiscountValue : rule.DiscountValue;
            var rewardProductId = tier?.RewardProductId;
            decimal discountAmount;

            if (overrideDiscountAmount.HasValue)
            {
                discountAmount = overrideDiscountAmount.Value;
            }
            else if (discountType == DiscountType.Percentage)
            {
                var subtotal = GetApplicableSubtotal(rule, cart, tier);
                discountAmount = subtotal * (discountValue / 100m);
            }
            else if (discountType == DiscountType.FixedAmount || discountType == DiscountType.FixedBundlePrice)
            {
                discountAmount = discountValue;
            }
            else if (discountType == DiscountType.FreeItem)
            {
                if (!rewardProductId.HasValue)
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    rewardProductId = ruleProducts.Where(x => x.IsRewardProduct && x.ProductId > 0).Select(x => (int?)x.ProductId).FirstOrDefault();
                }

                discountAmount = 0m;
                if (rewardProductId.HasValue)
                {
                    var rewardQty = tier?.RewardQuantity > 0 ? tier.RewardQuantity : 1;
                    var rewardItem = cart.FirstOrDefault(x => x.ProductId == rewardProductId.Value);
                    if (rewardItem != null && rewardItem.Quantity > 0)
                    {
                        var rewardUnitPrice = _priceCalculationService.GetUnitPrice(rewardItem, false);
                        discountAmount = rewardUnitPrice * rewardQty;
                    }
                    else
                    {
                        var rewardProduct = _productService.GetProductById(rewardProductId.Value);
                        discountAmount = (rewardProduct?.Price ?? 0m) * rewardQty;
                    }
                }
            }
            else
            {
                discountAmount = 0m;
            }

            var rewardQuantity = tier?.RewardQuantity ?? (discountType == DiscountType.FreeItem ? 1 : 0);
            return new AppliedPromotion
            {
                PromotionRuleId = rule.Id,
                RuleName = rule.Name,
                RuleTypeId = (int)rule.RuleType,
                DiscountTypeId = (int)discountType,
                DiscountAmount = discountAmount,
                DiscountValue = discountValue, // NEW: Store the original discount value
                IsExclusive = rule.IsExclusive,
                RewardProductId = rewardProductId,
                RewardQuantity = rewardQuantity,
                AutoAddReward = tier?.AutoAddReward ?? false
            };
        }

        public AppliedPromotion ApplyLinkedMaximumDiscountedQuantity(
            PromotionRule rule,
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart)
        {
            if (rule == null || appliedPromotion == null || cart == null || !cart.Any())
                return appliedPromotion;

            var maximumDiscountedQuantity = GetLinkedMaximumDiscountedQuantity(rule);
            if (!maximumDiscountedQuantity.HasValue || maximumDiscountedQuantity.Value <= 0)
                return appliedPromotion;

            if (appliedPromotion.RewardQuantity > 0)
                appliedPromotion.RewardQuantity = Math.Min(appliedPromotion.RewardQuantity, maximumDiscountedQuantity.Value);

            if (appliedPromotion.LineDiscounts != null && appliedPromotion.LineDiscounts.Any())
            {
                var capResult = CapLineDiscountsByQuantity(
                    cart,
                    appliedPromotion.LineDiscounts,
                    maximumDiscountedQuantity.Value,
                    appliedPromotion.RewardQuantity > 0);

                appliedPromotion.LineDiscounts = capResult.LineDiscounts;
                appliedPromotion.DiscountedQuantitiesByLineId = capResult.QuantitiesByLineId;
                appliedPromotion.DiscountAmount = capResult.LineDiscounts.Sum(x => x.Value);
                appliedPromotion.EligibleShoppingCartItemIds = cart
                    .Where(x => capResult.LineDiscounts.ContainsKey(x.Id))
                    .Select(x => x.Id)
                    .ToList();

                return appliedPromotion;
            }

            if (appliedPromotion.DiscountAmount <= 0)
                return appliedPromotion;

            var eligibleItems = GetEligibleCartItemsForAppliedPromotion(rule, appliedPromotion, cart);
            if (!eligibleItems.Any())
                return appliedPromotion;

            var discountedQuantities = BuildDiscountedQuantitiesMap(eligibleItems, maximumDiscountedQuantity.Value);
            appliedPromotion.DiscountedQuantitiesByLineId = discountedQuantities;
            appliedPromotion.EligibleShoppingCartItemIds = eligibleItems
                .Where(x => discountedQuantities.ContainsKey(x.Id))
                .Select(x => x.Id)
                .ToList();

            if (!discountedQuantities.Any())
            {
                appliedPromotion.DiscountAmount = 0m;
                return appliedPromotion;
            }

            var totalEligibleSubtotal = GetItemsSubtotal(eligibleItems);
            var cappedEligibleSubtotal = GetItemsSubtotal(eligibleItems, discountedQuantities);
            if (cappedEligibleSubtotal <= 0)
            {
                appliedPromotion.DiscountAmount = 0m;
                return appliedPromotion;
            }

            var discountType = (DiscountType)appliedPromotion.DiscountTypeId;
            if (discountType == DiscountType.Percentage && totalEligibleSubtotal > 0)
            {
                appliedPromotion.DiscountAmount *= cappedEligibleSubtotal / totalEligibleSubtotal;
            }
            else
            {
                appliedPromotion.DiscountAmount = Math.Min(appliedPromotion.DiscountAmount, cappedEligibleSubtotal);
            }

            return appliedPromotion;
        }

        public RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity)
        {
            return CalculateRewardDiscounts(rewardItems, rewardQuantity, null);
        }

        public RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId)
        {
            int overriddenQuantity;
            var discounts = new Dictionary<int, decimal>();
            if (rewardQuantity <= 0 || rewardItems == null || rewardItems.Count == 0)
                return new RewardDiscountResult(0m, discounts);

            var normalizedQuantities = NormalizeAvailableRewardQuantities(rewardItems, availableQuantitiesByLineId);
            var itemsWithPrice = new List<PricedCartLine>();
            foreach (var item in rewardItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                itemsWithPrice.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var remaining = rewardQuantity;
            foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
            {
                if (remaining <= 0)
                    break;

                var availableQuantity = normalizedQuantities.TryGetValue(item.Item.Id, out overriddenQuantity)
                    ? overriddenQuantity
                    : item.Item.Quantity;
                if (availableQuantity <= 0)
                    continue;

                var takeQty = Math.Min(availableQuantity, remaining);
                if (takeQty <= 0)
                    continue;

                var discount = item.UnitPrice * takeQty;
                if (discount > 0)
                    discounts[item.Item.Id] = discount;

                remaining -= takeQty;
            }

            return new RewardDiscountResult(discounts.Sum(x => x.Value), discounts);
        }

        public RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            DiscountType discountType,
            decimal discountValue)
        {
            return CalculateRewardDiscounts(rewardItems, rewardQuantity, null, discountType, discountValue);
        }

        public RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId,
            DiscountType discountType,
            decimal discountValue)
        {
            int overriddenQuantity;
            var discounts = new Dictionary<int, decimal>();
            if (rewardQuantity <= 0 || rewardItems == null || rewardItems.Count == 0)
                return new RewardDiscountResult(0m, discounts);

            var normalizedQuantities = NormalizeAvailableRewardQuantities(rewardItems, availableQuantitiesByLineId);
            var itemsWithPrice = new List<PricedCartLine>();
            foreach (var item in rewardItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                itemsWithPrice.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var remaining = rewardQuantity;
            foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
            {
                if (remaining <= 0)
                    break;

                var availableQuantity = normalizedQuantities.TryGetValue(item.Item.Id, out overriddenQuantity)
                    ? overriddenQuantity
                    : item.Item.Quantity;
                if (availableQuantity <= 0)
                    continue;

                var takeQty = Math.Min(availableQuantity, remaining);
                if (takeQty <= 0)
                    continue;

                decimal discount;
                if (discountType == DiscountType.Percentage)
                {
                    discount = item.UnitPrice * takeQty * (discountValue / 100m);
                }
                else if (discountType == DiscountType.FixedAmount)
                {
                    discount = Math.Min(item.UnitPrice * takeQty, discountValue * takeQty);
                }
                else
                {
                    discount = item.UnitPrice * takeQty;
                }

                if (discount > 0)
                    discounts[item.Item.Id] = discount;

                remaining -= takeQty;
            }

            return new RewardDiscountResult(discounts.Sum(x => x.Value), discounts);
        }

        public RewardDiscountQuantizedResult CalculateRewardDiscountsQuantized(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId,
            DiscountType discountType,
            decimal discountValue)
        {
            int overriddenQuantity;
            var baseRewardDiscountResult = CalculateRewardDiscounts(rewardItems, rewardQuantity, availableQuantitiesByLineId);
            var baseDiscountAmount = baseRewardDiscountResult.DiscountAmount;
            var baseLineDiscounts = baseRewardDiscountResult.LineDiscounts;
            if (baseDiscountAmount <= 0 || !baseLineDiscounts.Any())
                return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

            // CRITICAL FIX: Track quantities separately from discount amounts
            var discountedQuantities = new Dictionary<int, int>();

            // DEBUG: Log quantity tracking
            DiscountManagerPlusLog.Information(_logger, $"QUANTITY_DEBUG: Calculating rewards for {rewardItems.Count} items, reward quantity: {rewardQuantity}");

            // Reconstruct discounted quantities from base calculation
            var normalizedQuantities = NormalizeAvailableRewardQuantities(rewardItems, availableQuantitiesByLineId);
            var itemsWithPrice = new List<PricedCartLine>();
            foreach (var item in rewardItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                itemsWithPrice.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var remaining = rewardQuantity;
            foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
            {
                if (remaining <= 0)
                    break;

                var availableQuantity = normalizedQuantities.TryGetValue(item.Item.Id, out overriddenQuantity)
                    ? overriddenQuantity
                    : item.Item.Quantity;
                if (availableQuantity <= 0)
                    continue;

                var takeQty = Math.Min(availableQuantity, remaining);
                if (takeQty <= 0)
                    continue;

                // CRITICAL: Track which specific units got discounted
                discountedQuantities[item.Item.Id] = takeQty;

                // DEBUG: Log quantity allocation
                DiscountManagerPlusLog.Information(_logger, $"QUANTITY_DEBUG: Allocated {takeQty} unit(s) of Product {item.Item.ProductId} (Line {item.Item.Id}) at ${item.UnitPrice} each = ${takeQty * item.UnitPrice} discount");

                remaining -= takeQty;
            }

            DiscountManagerPlusLog.Information(_logger, $"QUANTITY_DEBUG: Total discounted quantities: {string.Join(", ", discountedQuantities.Select(x => $"Line {x.Key}: {x.Value} unit(s)"))}");

            if (discountType == DiscountType.FreeItem)
                return new RewardDiscountQuantizedResult(baseDiscountAmount, baseLineDiscounts, discountedQuantities);

            if (discountType == DiscountType.Percentage)
            {
                if (discountValue <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var scale = discountValue / 100m;
                if (scale <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var lineDiscounts = baseLineDiscounts.ToDictionary(x => x.Key, x => x.Value * scale);
                return new RewardDiscountQuantizedResult(lineDiscounts.Sum(x => x.Value), lineDiscounts, discountedQuantities);
            }

            if (discountType == DiscountType.FixedAmount)
            {
                if (discountValue <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var applied = Math.Min(discountValue, baseDiscountAmount);
                var lineDiscounts = new Dictionary<int, decimal>();
                var remainingDiscount = applied;
                foreach (var line in baseLineDiscounts.OrderByDescending(x => x.Value))
                {
                    if (remainingDiscount <= 0)
                        break;

                    var value = Math.Min(line.Value, remainingDiscount);
                    if (value <= 0)
                        continue;

                    lineDiscounts[line.Key] = value;
                    remainingDiscount -= value;
                }

                return new RewardDiscountQuantizedResult(lineDiscounts.Sum(x => x.Value), lineDiscounts, discountedQuantities);
            }

            return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());
        }

        public decimal MergeLineDiscountsWithCap(
            IList<ShoppingCartItem> cart,
            IDictionary<int, decimal> aggregateLineDiscounts,
            IDictionary<int, decimal> candidateLineDiscounts,
            IDictionary<int, decimal> lineSubtotalCache)
        {
            ShoppingCartItem line;
            decimal lineSubtotal;
            decimal existing;
            if (cart == null || !cart.Any() ||
                aggregateLineDiscounts == null ||
                candidateLineDiscounts == null || !candidateLineDiscounts.Any())
            {
                return 0m;
            }

            var cartByLineId = cart
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());

            var mergedDiscountAmount = 0m;
            foreach (var candidateLineDiscount in candidateLineDiscounts)
            {
                if (candidateLineDiscount.Value <= 0)
                    continue;

                if (!cartByLineId.TryGetValue(candidateLineDiscount.Key, out line))
                    continue;

                if (!lineSubtotalCache.TryGetValue(line.Id, out lineSubtotal))
                {
                    var unitPrice = _priceCalculationService.GetUnitPrice(line, false);
                    lineSubtotal = Math.Max(0m, unitPrice * line.Quantity);
                    lineSubtotalCache[line.Id] = lineSubtotal;
                }

                if (lineSubtotal <= 0)
                    continue;

                var alreadyApplied = aggregateLineDiscounts.TryGetValue(line.Id, out existing) ? existing : 0m;
                var availableRoom = lineSubtotal - alreadyApplied;
                if (availableRoom <= 0)
                    continue;

                var amountToApply = Math.Min(availableRoom, candidateLineDiscount.Value);
                if (amountToApply <= 0)
                    continue;

                aggregateLineDiscounts[line.Id] = alreadyApplied + amountToApply;
                mergedDiscountAmount += amountToApply;
            }

            return mergedDiscountAmount;
        }

        public DiscountMapsResult BuildDiscountMaps(
            PromotionEvaluationContext context,
            Func<PromotionRule, PromotionEvaluationContext, bool> isRuleRuntimeEligible,
            Func<PromotionRule, IList<ShoppingCartItem>, PromotionEvaluationContext, AppliedPromotion> evaluateRule)
        {
            int existingConsumed;
            int existingQuantity;
            decimal existing;
            decimal existingRuleDiscount;
            var lineDiscountMap = new Dictionary<int, decimal>();
            var ruleDiscountMap = new Dictionary<int, decimal>();
            var cart = context?.Cart;
            if (cart == null || !cart.Any())
                return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);

            var activeRules = _promotionRuleService.GetActiveRules(context.StoreId);
            var blockedLineIds = new HashSet<int>();
            var consumedBuyXGetYQuantitiesByLineId = new Dictionary<int, int>();
            var allocatedDiscountUnitsByLineId = new Dictionary<int, Dictionary<int, decimal>>(); // Track unit-level discounts

            foreach (var rule in activeRules)
            {
                if (!isRuleRuntimeEligible(rule, context))
                    continue;

                var evaluationCart = rule.RuleType == PromotionRuleType.BuyXGetY
                    ? BuildCartWithRemainingQuantities(cart, consumedBuyXGetYQuantitiesByLineId)
                    : cart;
                if (!evaluationCart.Any())
                    continue;

                var evaluationContext = ReferenceEquals(evaluationCart, cart)
                    ? context
                    : CloneContextWithCart(context, evaluationCart);

                var appliedPromotion = evaluateRule(rule, evaluationCart, evaluationContext);
                if (appliedPromotion == null)
                    continue;

                var isFreeItemReward = appliedPromotion.DiscountTypeId == (int)DiscountType.FreeItem &&
                                       appliedPromotion.RewardQuantity > 0;
                if (!isFreeItemReward && appliedPromotion.DiscountAmount <= 0)
                    continue;

                Dictionary<int, decimal> ruleLineDiscounts = null;
                if (appliedPromotion.LineDiscounts != null && appliedPromotion.LineDiscounts.Any())
                {
                    ruleLineDiscounts = appliedPromotion.LineDiscounts
                        .Where(x => x.Value > 0 && !blockedLineIds.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                if (ruleLineDiscounts == null && isFreeItemReward)
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    IList<ShoppingCartItem> rewardItems = new ShoppingCartItem[0];

                    var hasExplicitRewardScope = HasExplicitRewardScope(ruleProducts, appliedPromotion);
                    if (hasExplicitRewardScope)
                    {
                        rewardItems = GetExplicitRewardItems(cart, ruleProducts, appliedPromotion);
                    }
                    else if (appliedPromotion.EligibleShoppingCartItemIds != null && appliedPromotion.EligibleShoppingCartItemIds.Any())
                    {
                        var rewardIds = new HashSet<int>(appliedPromotion.EligibleShoppingCartItemIds);
                        rewardItems = cart.Where(x => rewardIds.Contains(x.Id)).ToList();
                    }

                    if (rewardItems.Any())
                    {
                        // CRITICAL FIX: Use new quantized method that tracks which specific units received discounts
                        var rewardDiscountResult = CalculateRewardDiscountsQuantized(
                            rewardItems,
                            appliedPromotion.RewardQuantity,
                            null, // availableQuantitiesByLineId - let method handle it
                            (DiscountType)appliedPromotion.DiscountTypeId,
                            appliedPromotion.DiscountValue);

                        var rewardLineDiscounts = rewardDiscountResult.LineDiscounts;
                        var rewardDiscountedQuantities = rewardDiscountResult.DiscountedQuantities;

                        ruleLineDiscounts = rewardLineDiscounts
                            .Where(x => !blockedLineIds.Contains(x.Key) && x.Value > 0)
                            .ToDictionary(x => x.Key, x => x.Value);

                        // CRITICAL: Track discounted quantities for consumption
                        if (rewardDiscountedQuantities != null && rewardDiscountedQuantities.Any())
                        {
                            foreach (var discountedQty in rewardDiscountedQuantities)
                            {
                                if (consumedBuyXGetYQuantitiesByLineId.TryGetValue(discountedQty.Key, out existingConsumed))
                                    consumedBuyXGetYQuantitiesByLineId[discountedQty.Key] = existingConsumed + discountedQty.Value;
                                else
                                    consumedBuyXGetYQuantitiesByLineId[discountedQty.Key] = discountedQty.Value;
                            }
                        }
                    }
                }
                else if (ruleLineDiscounts == null)
                {
                    var eligibleItems = GetEligibleCartItemsForAppliedPromotion(rule, appliedPromotion, cart);
                    eligibleItems = eligibleItems
                        .Where(x => !blockedLineIds.Contains(x.Id))
                        .ToList();

                    if (eligibleItems.Any())
                    {
                        // Use cheapest-first allocation for better multi-offer support
                        ruleLineDiscounts = AllocateDiscountCheapestFirst(
                            appliedPromotion.DiscountAmount,
                            eligibleItems,
                            appliedPromotion.DiscountedQuantitiesByLineId);
                    }
                }

                if (ruleLineDiscounts == null || !ruleLineDiscounts.Any())
                    continue;

                var ruleDiscountAmount = ruleLineDiscounts.Sum(x => x.Value);
                if (ruleDiscountAmount <= 0)
                    continue;

                if (appliedPromotion.IsExclusive)
                {
                    lineDiscountMap.Clear();
                    ruleDiscountMap.Clear();
                    foreach (var discount in ruleLineDiscounts)
                        lineDiscountMap[discount.Key] = discount.Value;

                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = ruleDiscountAmount;
                    return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);
                }

                foreach (var discount in ruleLineDiscounts)
                {
                    if (lineDiscountMap.TryGetValue(discount.Key, out existing))
                        lineDiscountMap[discount.Key] = existing + discount.Value;
                    else
                        lineDiscountMap[discount.Key] = discount.Value;
                }

                if (ruleDiscountMap.TryGetValue(appliedPromotion.PromotionRuleId, out existingRuleDiscount))
                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = existingRuleDiscount + ruleDiscountAmount;
                else
                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = ruleDiscountAmount;

                if (rule.StopFurtherRulesForMatchedLines)
                {
                    foreach (var lineId in ruleLineDiscounts.Keys)
                        blockedLineIds.Add(lineId);
                }

                // FIXED: Track consumed quantities for multi-buy product scenarios
                if (rule.RuleType == PromotionRuleType.BuyXGetY &&
                    appliedPromotion.DiscountedQuantitiesByLineId != null &&
                    appliedPromotion.DiscountedQuantitiesByLineId.Any())
                {
                    foreach (var quantityEntry in appliedPromotion.DiscountedQuantitiesByLineId)
                    {
                        if (quantityEntry.Value <= 0)
                            continue;

                        if (consumedBuyXGetYQuantitiesByLineId.TryGetValue(quantityEntry.Key, out existingQuantity))
                            consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = existingQuantity + quantityEntry.Value;
                        else
                            consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = quantityEntry.Value;
                    }

                    // Also track discounted line IDs for blocking further discounts on the same items
                    if (rule.StopFurtherRulesForMatchedLines)
                    {
                        foreach (var lineId in appliedPromotion.DiscountedQuantitiesByLineId.Keys)
                        {
                            blockedLineIds.Add(lineId);
                        }
                    }
                }
            }

            return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);
        }

        private static PromotionEvaluationContext CloneContextWithCart(PromotionEvaluationContext context, IList<ShoppingCartItem> cart)
        {
            return new PromotionEvaluationContext
            {
                StoreId = context?.StoreId ?? 0,
                CustomerId = context?.CustomerId ?? 0,
                Cart = cart ?? new List<ShoppingCartItem>(),
                CouponCodes = context?.CouponCodes?.ToList() ?? new List<string>(),
                SelectedPaymentMethodSystemName = context?.SelectedPaymentMethodSystemName ?? string.Empty,
                CountryIso2 = context?.CountryIso2 ?? string.Empty,
                CurrentUtc = context?.CurrentUtc ?? DateTime.UtcNow,
                SocialShareProofToken = context?.SocialShareProofToken ?? string.Empty
            };
        }

        private static IList<ShoppingCartItem> BuildCartWithRemainingQuantities(
            IList<ShoppingCartItem> cart,
            IDictionary<int, int> consumedQuantitiesByLineId)
        {
            int existingConsumedQuantity;
            if (cart == null || !cart.Any())
                return new ShoppingCartItem[0];

            if (consumedQuantitiesByLineId == null || !consumedQuantitiesByLineId.Any())
                return cart;

            var remainingCart = new List<ShoppingCartItem>();
            foreach (var item in cart)
            {
                if (item == null || item.Quantity <= 0)
                    continue;

                var consumedQuantity = consumedQuantitiesByLineId.TryGetValue(item.Id, out existingConsumedQuantity)
                    ? Math.Max(0, existingConsumedQuantity)
                    : 0;
                var remainingQuantity = item.Quantity - consumedQuantity;
                if (remainingQuantity <= 0)
                    continue;

                // CRITICAL FIX: Keep original ID but ensure quantity tracking works correctly
                // The consumed quantities dictionary tracks how many units from each cart line
                // have been "used up" by previous Buy X Get Y offers
                remainingCart.Add(new ShoppingCartItem
                {
                    Id = item.Id, // Must keep original ID for discount mapping
                    StoreId = item.StoreId,
                    ShoppingCartTypeId = item.ShoppingCartTypeId,
                    CustomerId = item.CustomerId,
                    ProductId = item.ProductId,
                    AttributesXml = item.AttributesXml,
                    CustomerEnteredPrice = item.CustomerEnteredPrice,
                    Quantity = remainingQuantity,
                    RentalStartDateUtc = item.RentalStartDateUtc,
                    RentalEndDateUtc = item.RentalEndDateUtc,
                    CreatedOnUtc = item.CreatedOnUtc,
                    UpdatedOnUtc = item.UpdatedOnUtc
                });
            }

            return remainingCart;
        }

        public IList<ShoppingCartItem> GetEligibleCartItemsForAppliedPromotion(
            PromotionRule rule,
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart)
        {
            if (rule.DiscountScope == DiscountScope.WholeCart && !PromotionRuntimeHelper.IsItemLevelRuleType(rule.RuleType))
                return cart.Where(x => x.Quantity > 0).ToList();

            switch (rule.RuleType)
            {
                case PromotionRuleType.ProductBased:
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    if (!ruleProducts.Any())
                        return new ShoppingCartItem[0];

                    if (rule.DiscountType == DiscountType.FreeItem)
                    {
                        var hasExplicitRewardScope = HasExplicitRewardScope(ruleProducts, appliedPromotion);
                        if (hasExplicitRewardScope)
                        {
                            var rewardItems = GetExplicitRewardItems(cart, ruleProducts, appliedPromotion);
                            if (rewardItems.Any())
                                return rewardItems;
                        }
                        else
                        {
                            var eligibleItems = GetEligibleItemsFromAppliedPromotion(appliedPromotion, cart);
                            if (eligibleItems.Any())
                                return eligibleItems;
                        }

                        ruleProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
                    }
                    else
                    {
                        var eligibleItems = GetEligibleItemsFromAppliedPromotion(appliedPromotion, cart);
                        if (eligibleItems.Any())
                            return eligibleItems;
                    }

                    return _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, ruleProducts);
                }

                case PromotionRuleType.ComboPricing:
                {
                    var eligibleItems = GetEligibleItemsFromAppliedPromotion(appliedPromotion, cart);
                    if (eligibleItems.Any())
                        return eligibleItems;

                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    return _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, ruleProducts);
                }

                case PromotionRuleType.BuyXGetY:
                {
                    var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                    var hasExplicitRewardScope = HasExplicitRewardScope(ruleProducts, appliedPromotion);
                    if (hasExplicitRewardScope)
                    {
                        var rewardItems = GetExplicitRewardItems(cart, ruleProducts, appliedPromotion);
                        if (rewardItems.Any())
                            return rewardItems;
                    }
                    else
                    {
                        var eligibleItems = GetEligibleItemsFromAppliedPromotion(appliedPromotion, cart);
                        if (eligibleItems.Any())
                            return eligibleItems;
                    }

                    var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
                    return _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
                }

                case PromotionRuleType.CartCondition:
                case PromotionRuleType.SubtotalBased:
                default:
                    var defaultEligibleItems = GetEligibleItemsFromAppliedPromotion(appliedPromotion, cart);
                    if (defaultEligibleItems.Any())
                        return defaultEligibleItems;

                    return cart.Where(x => x.Quantity > 0).ToList();
            }
        }

        private static IList<ShoppingCartItem> GetEligibleItemsFromAppliedPromotion(
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart)
        {
            if (appliedPromotion?.EligibleShoppingCartItemIds == null || !appliedPromotion.EligibleShoppingCartItemIds.Any())
                return new ShoppingCartItem[0];

            var eligibleIds = new HashSet<int>(appliedPromotion.EligibleShoppingCartItemIds);
            return cart.Where(x => eligibleIds.Contains(x.Id)).ToList();
        }

        private static bool HasExplicitRewardScope(
            IList<PromotionRuleProduct> ruleProducts,
            AppliedPromotion appliedPromotion)
        {
            return (ruleProducts?.Any(x => x.IsRewardProduct) ?? false) ||
                   ((appliedPromotion?.RewardProductId.HasValue ?? false) && appliedPromotion.RewardProductId.Value > 0);
        }

        private IList<ShoppingCartItem> GetExplicitRewardItems(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            AppliedPromotion appliedPromotion)
        {
            var rewardProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            if (rewardProducts.Any())
                return _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, rewardProducts);

            if (appliedPromotion != null && appliedPromotion.RewardProductId.HasValue && appliedPromotion.RewardProductId.Value > 0)
                return cart.Where(x => x.ProductId == appliedPromotion.RewardProductId.Value).ToList();

            return new ShoppingCartItem[0];
        }

        public Dictionary<int, decimal> AllocateDiscountAcrossItems(
            decimal discountAmount,
            IList<ShoppingCartItem> eligibleItems,
            IDictionary<int, int> discountedQuantitiesByLineId = null)
        {
            int overriddenQuantity;
            decimal current;
            var lineDiscountMap = new Dictionary<int, decimal>();
            if (discountAmount <= 0 || eligibleItems == null || !eligibleItems.Any())
                return lineDiscountMap;

            var uniqueEligibleItems = eligibleItems
                .Where(x => x.Quantity > 0)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();
            if (!uniqueEligibleItems.Any())
                return lineDiscountMap;

            var lineSubtotals = new Dictionary<int, decimal>();
            foreach (var eligibleItem in uniqueEligibleItems)
            {
                var quantityToDiscount = eligibleItem.Quantity;
                if (discountedQuantitiesByLineId != null && discountedQuantitiesByLineId.Any())
                {
                    if (!discountedQuantitiesByLineId.TryGetValue(eligibleItem.Id, out overriddenQuantity))
                        quantityToDiscount = 0;
                    else
                        quantityToDiscount = Math.Min(eligibleItem.Quantity, Math.Max(0, overriddenQuantity));
                }

                if (quantityToDiscount <= 0)
                    continue;

                var unitPrice = _priceCalculationService.GetUnitPrice(eligibleItem, false);
                var lineSubtotal = unitPrice * quantityToDiscount;
                if (lineSubtotal <= 0)
                    continue;

                lineSubtotals[eligibleItem.Id] = lineSubtotal;
            }

            if (!lineSubtotals.Any())
                return lineDiscountMap;

            var totalEligibleSubtotal = lineSubtotals.Sum(x => x.Value);
            if (totalEligibleSubtotal <= 0)
                return lineDiscountMap;

            var allocatableDiscount = Math.Min(discountAmount, totalEligibleSubtotal);
            if (allocatableDiscount <= 0)
                return lineDiscountMap;

            var remainingDiscount = allocatableDiscount;
            var lineIds = lineSubtotals.Keys.ToList();
            for (var index = 0; index < lineIds.Count; index++)
            {
                var lineId = lineIds[index];
                var lineSubtotal = lineSubtotals[lineId];

                decimal lineDiscount;
                if (index == lineIds.Count - 1)
                {
                    lineDiscount = remainingDiscount;
                }
                else
                {
                    lineDiscount = allocatableDiscount * (lineSubtotal / totalEligibleSubtotal);
                    remainingDiscount -= lineDiscount;
                }

                if (lineDiscount <= 0)
                    continue;

                if (lineDiscount > lineSubtotal)
                    lineDiscount = lineSubtotal;

                lineDiscountMap[lineId] = lineDiscount;
            }

            var distributedDiscount = lineDiscountMap.Sum(x => x.Value);
            remainingDiscount = allocatableDiscount - distributedDiscount;
            if (remainingDiscount > 0)
            {
                foreach (var lineId in lineIds.OrderByDescending(x => lineSubtotals[x]))
                {
                    var alreadyDistributed = lineDiscountMap.TryGetValue(lineId, out current) ? current : 0m;
                    var room = lineSubtotals[lineId] - alreadyDistributed;
                    if (room <= 0)
                        continue;

                    var extra = Math.Min(room, remainingDiscount);
                    if (extra <= 0)
                        continue;

                    lineDiscountMap[lineId] = alreadyDistributed + extra;
                    remainingDiscount -= extra;
                    if (remainingDiscount <= 0)
                        break;
                }
            }

            return lineDiscountMap;
        }

        private decimal GetItemSubtotal(ShoppingCartItem item)
        {
            var product = _productService.GetProductById(item.ProductId);
            if (product == null)
                return 0m;

            var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
            return unitPrice * item.Quantity;
        }

        /// <summary>
        /// Allocates discount across items using cheapest-first strategy
        /// This ensures that when multiple items are eligible, the cheapest items receive the discount first
        /// </summary>
        public Dictionary<int, decimal> AllocateDiscountCheapestFirst(
            decimal discountAmount,
            IList<ShoppingCartItem> eligibleItems,
            IDictionary<int, int> discountedQuantitiesByLineId = null)
        {
            int overriddenQuantity;
            var lineDiscountMap = new Dictionary<int, decimal>();
            if (discountAmount <= 0 || eligibleItems == null || !eligibleItems.Any())
                return lineDiscountMap;

            // Get unique eligible items with their prices
            var itemPricing = new List<ItemPricingInfo>();
            foreach (var eligibleItem in eligibleItems.Where(x => x.Quantity > 0))
            {
                var quantityToDiscount = eligibleItem.Quantity;
                if (discountedQuantitiesByLineId != null && discountedQuantitiesByLineId.Any())
                {
                    if (!discountedQuantitiesByLineId.TryGetValue(eligibleItem.Id, out overriddenQuantity))
                        quantityToDiscount = 0;
                    else
                        quantityToDiscount = Math.Min(eligibleItem.Quantity, Math.Max(0, overriddenQuantity));
                }

                if (quantityToDiscount <= 0)
                    continue;

                var unitPrice = _priceCalculationService.GetUnitPrice(eligibleItem, false);
                var lineSubtotal = unitPrice * quantityToDiscount;

                if (lineSubtotal > 0)
                {
                    itemPricing.Add(new ItemPricingInfo
                    {
                        LineId = eligibleItem.Id,
                        UnitPrice = unitPrice,
                        Quantity = quantityToDiscount,
                        LineSubtotal = lineSubtotal
                    });
                }
            }

            if (!itemPricing.Any())
                return lineDiscountMap;

            // Sort by unit price (cheapest first) - FIXED for multi-offer scenarios
            var sortedItems = itemPricing.OrderBy(x => x.UnitPrice).ToList();

            var remainingDiscount = discountAmount;
            foreach (var item in sortedItems)
            {
                if (remainingDiscount <= 0)
                    break;

                // Calculate discount for this item using proportionate allocation
                // This ensures cheapest items get priority but prevents over-discounting
                var maxDiscountForItem = Math.Min(item.LineSubtotal, remainingDiscount);

                if (maxDiscountForItem > 0)
                {
                    lineDiscountMap[item.LineId] = maxDiscountForItem;
                    remainingDiscount -= maxDiscountForItem;
                }
            }

            return lineDiscountMap;
        }

        /// <summary>
        /// Generates attention/boost messages when better discounts are available
        /// </summary>
        public IList<PromotionAttentionMessage> GenerateAttentionMessages(
            PromotionEvaluationContext context,
            IDictionary<int, decimal> finalLineDiscountMap,
            IDictionary<int, decimal> finalRuleDiscountMap)
        {
            decimal appliedDiscount;
            var attentionMessages = new List<PromotionAttentionMessage>();

            if (context == null || context.Cart == null || !context.Cart.Any())
                return attentionMessages;

            var cart = context.Cart;
            var activeRules = _promotionRuleService.GetActiveRules(context.StoreId);

            foreach (var rule in activeRules)
            {
                // Check if this rule was not applied
                var wasApplied = finalRuleDiscountMap != null &&
                               finalRuleDiscountMap.TryGetValue(rule.Id, out appliedDiscount) &&
                               appliedDiscount > 0;

                if (wasApplied)
                    continue;

                // Check if this rule could be triggered with more items
                var potentialMessage = CheckRulePotential(rule, cart, context);
                if (potentialMessage != null)
                {
                    attentionMessages.Add(potentialMessage);
                }
            }

            return attentionMessages;
        }

        private PromotionAttentionMessage CheckRulePotential(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            // FIXED: Enhanced attention message generation for dual-offer scenarios
            if (rule.RuleType != PromotionRuleType.BuyXGetY &&
                rule.RuleType != PromotionRuleType.ProductBased)
                return null;

            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();

            if (!buyProducts.Any())
                return null;

            var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            if (!tiers.Any())
                return null;

            // Check the lowest tier
            var lowestTier = tiers.OrderBy(t => t.MinQuantity).FirstOrDefault();
            if (lowestTier == null)
                return null;

            // Check if we're close to triggering the offer
            var matchedItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
            var currentQuantity = matchedItems.Sum(x => x.Quantity);
            var requiredQuantity = lowestTier.MinQuantity;

            if (currentQuantity >= requiredQuantity)
                return null; // Already triggered

            var itemsNeeded = requiredQuantity - currentQuantity;
            if (itemsNeeded <= 0)
                return null;

            // FIXED: Show suggestions when within 3 items (not just 1-2) for better dual-offer awareness
            if (itemsNeeded > 3)
                return null;

            // FIXED: Better discount calculation for attention messages
            var potentialDiscount = 0m;
            if (lowestTier.DiscountType == DiscountType.FreeItem)
            {
                // For free items, estimate based on average item price
                var avgPrice = matchedItems.Any() ? GetAverageItemPrice(matchedItems) : 50m;
                potentialDiscount = avgPrice * lowestTier.RewardQuantity;
            }
            else if (lowestTier.DiscountType == DiscountType.Percentage)
            {
                // For percentage discounts, estimate based on current cart value
                var currentSubtotal = GetItemsSubtotal(matchedItems);
                potentialDiscount = currentSubtotal * (lowestTier.DiscountValue / 100m);
            }
            else
            {
                potentialDiscount = lowestTier.DiscountValue;
            }

            if (potentialDiscount <= 0)
                return null;

            // FIXED: Better reward text generation for different discount types
            var rewardText = lowestTier.DiscountType == DiscountType.FreeItem
                ? $"Get {lowestTier.RewardQuantity} FREE"
                : lowestTier.DiscountType == DiscountType.Percentage
                    ? $"Get {lowestTier.DiscountValue}% off"
                    : $"Save ${lowestTier.DiscountValue:F2}";

            // FIXED: Enhanced message with more context
            var message = itemsNeeded == 1
                ? $"Add just 1 more item to {rewardText}! (Save ${potentialDiscount:F2})"
                : $"Add {itemsNeeded} more items to {rewardText}! (Save ${potentialDiscount:F2})";

            return new PromotionAttentionMessage
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Message = message,
                MessageType = currentQuantity >= requiredQuantity - 1 ? AttentionMessageType.Boost : AttentionMessageType.Opportunity,
                RequiredAdditionalItems = itemsNeeded,
                PotentialAdditionalDiscount = potentialDiscount,
                ProductNames = matchedItems.Take(3).Select(x => x.ProductId.ToString()).ToList()
            };
        }

        private decimal GetAverageItemPrice(IList<ShoppingCartItem> items)
        {
            if (!items.Any())
                return 50m; // Default fallback price

            var total = 0m;
            var count = 0;

            foreach (var item in items)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                total += unitPrice;
                count++;
            }

            return count > 0 ? total / count : 50m;
        }

        private static Dictionary<int, int> NormalizeAvailableRewardQuantities(
            IList<ShoppingCartItem> rewardItems,
            IDictionary<int, int> availableQuantitiesByLineId)
        {
            int overriddenQuantity;
            var normalized = new Dictionary<int, int>();
            if (rewardItems == null || !rewardItems.Any())
                return normalized;

            foreach (var rewardItem in rewardItems.Where(x => x != null))
            {
                var quantity = rewardItem.Quantity;
                if (availableQuantitiesByLineId != null &&
                    availableQuantitiesByLineId.TryGetValue(rewardItem.Id, out overriddenQuantity))
                {
                    quantity = Math.Min(rewardItem.Quantity, Math.Max(0, overriddenQuantity));
                }

                normalized[rewardItem.Id] = quantity;
            }

            return normalized;
        }

        private int? GetLinkedMaximumDiscountedQuantity(PromotionRule rule)
        {
            var parentDiscountId = PromotionRuntimeHelper.GetParentDiscountId(rule);
            if (parentDiscountId <= 0)
                return null;

            var discount = _discountService.GetDiscountById(parentDiscountId);
            if (discount == null || !discount.MaximumDiscountedQuantity.HasValue || discount.MaximumDiscountedQuantity.Value <= 0)
                return null;

            return discount.MaximumDiscountedQuantity.Value;
        }

        private static Dictionary<int, int> BuildDiscountedQuantitiesMap(
            IList<ShoppingCartItem> eligibleItems,
            int maximumDiscountedQuantity)
        {
            var discountedQuantities = new Dictionary<int, int>();
            if (maximumDiscountedQuantity <= 0 || eligibleItems == null || !eligibleItems.Any())
                return discountedQuantities;

            var remainingQuantity = maximumDiscountedQuantity;
            foreach (var item in eligibleItems
                         .Where(x => x != null && x.Quantity > 0)
                         .GroupBy(x => x.Id)
                         .Select(x => x.First()))
            {
                if (remainingQuantity <= 0)
                    break;

                var quantityToDiscount = Math.Min(item.Quantity, remainingQuantity);
                if (quantityToDiscount <= 0)
                    continue;

                discountedQuantities[item.Id] = quantityToDiscount;
                remainingQuantity -= quantityToDiscount;
            }

            return discountedQuantities;
        }

        private static CappedLineDiscountResult CapLineDiscountsByQuantity(
            IList<ShoppingCartItem> cart,
            IDictionary<int, decimal> lineDiscounts,
            int maximumDiscountedQuantity,
            bool preferLowestUnitDiscount)
        {
            var cappedLineDiscounts = new Dictionary<int, decimal>();
            var cappedQuantities = new Dictionary<int, int>();
            if (cart == null || !cart.Any() || lineDiscounts == null || !lineDiscounts.Any() || maximumDiscountedQuantity <= 0)
                return new CappedLineDiscountResult(cappedLineDiscounts, cappedQuantities);

            var cartIndex = cart
                .Where(x => x != null)
                .Select((item, itemIndex) => new { item.Id, Index = itemIndex })
                .ToDictionary(x => x.Id, x => x.Index);

            var discountedLines = lineDiscounts
                .Where(x => x.Value > 0)
                .Select(x => new
                {
                    CartItem = cart.FirstOrDefault(item => item.Id == x.Key),
                    DiscountAmount = x.Value
                })
                .Where(x => x.CartItem != null && x.CartItem.Quantity > 0)
                .Select(x => new
                {
                    x.CartItem,
                    x.DiscountAmount,
                    UnitDiscount = x.DiscountAmount / x.CartItem.Quantity
                });

            discountedLines = preferLowestUnitDiscount
                ? discountedLines.OrderBy(x => x.UnitDiscount).ThenBy(x => GetCartLineIndex(cartIndex, x.CartItem.Id))
                : discountedLines.OrderBy(x => GetCartLineIndex(cartIndex, x.CartItem.Id));

            var remainingQuantity = maximumDiscountedQuantity;
            foreach (var discountedLine in discountedLines)
            {
                if (remainingQuantity <= 0)
                    break;

                var quantityToDiscount = Math.Min(discountedLine.CartItem.Quantity, remainingQuantity);
                if (quantityToDiscount <= 0)
                    continue;

                var lineDiscount = discountedLine.UnitDiscount * quantityToDiscount;
                if (lineDiscount <= 0)
                    continue;

                cappedQuantities[discountedLine.CartItem.Id] = quantityToDiscount;
                cappedLineDiscounts[discountedLine.CartItem.Id] = Math.Min(discountedLine.DiscountAmount, lineDiscount);
                remainingQuantity -= quantityToDiscount;
            }

            return new CappedLineDiscountResult(cappedLineDiscounts, cappedQuantities);
        }

        private static int GetCartLineIndex(IDictionary<int, int> cartIndex, int cartItemId)
        {
            int lineIndex;
            if (cartIndex != null && cartIndex.TryGetValue(cartItemId, out lineIndex))
                return lineIndex;

            return int.MaxValue;
        }
    }
}
