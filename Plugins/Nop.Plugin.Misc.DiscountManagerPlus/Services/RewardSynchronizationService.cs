using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class RewardSynchronizationService : IRewardSynchronizationService
    {
        private const string AutoRewardDescriptorAttribute = "DiscountManagerPlus.AutoAddedRewardDescriptor";
        private const string AutoRewardManagedQuantityAttribute = "DiscountManagerPlus.AutoAddedRewardManagedQuantity";
        private const string ManualRewardDescriptorAttribute = "DiscountManagerPlus.ManualRewardDescriptor";
        private const string ManualRewardManagedQuantityAttribute = "DiscountManagerPlus.ManualRewardManagedQuantity";
        private const string ManualRewardRuleIdAttribute = "DiscountManagerPlus.ManualRewardRuleId";

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILogger _logger;

        public RewardSynchronizationService(
            IGenericAttributeService genericAttributeService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IPromotionRuleService promotionRuleService,
            IShoppingCartService shoppingCartService,
            ILogger logger)
        {
            _genericAttributeService = genericAttributeService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _promotionRuleService = promotionRuleService;
            _shoppingCartService = shoppingCartService;
            _logger = logger;
        }

        public string BuildRewardAttributesXml(PromotionRuleProduct rewardProduct)
        {
            if (rewardProduct == null ||
                rewardProduct.RewardAttributeSelectionType != RewardAttributeSelectionType.SpecificValues ||
                string.IsNullOrWhiteSpace(rewardProduct.RewardAttributeValueIds))
            {
                return string.Empty;
            }

            var valueIds = ParseIdList(rewardProduct.RewardAttributeValueIds);
            if (!valueIds.Any())
                return string.Empty;

            var attributesXml = string.Empty;
            foreach (var valueId in valueIds)
            {
                var value = _productAttributeService.GetProductAttributeValueById(valueId);
                if (value == null)
                    continue;

                var mapping = _productAttributeService.GetProductAttributeMappingById(value.ProductAttributeMappingId);
                if (mapping == null)
                    continue;

                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, mapping, value.Id.ToString());
            }

            return attributesXml;
        }

        public bool RequiresRewardSelection(PromotionRuleProduct rewardRuleProduct, Product rewardProduct)
        {
            if (rewardProduct == null)
                return false;

            if (rewardProduct.CustomerEntersPrice || rewardProduct.IsGiftCard || rewardProduct.IsRental)
                return true;

            var requiredMappings = _productAttributeService.GetProductAttributeMappingsByProductId(rewardProduct.Id)
                .Where(x => x.IsRequired)
                .ToList();
            if (!requiredMappings.Any())
                return false;

            if (rewardRuleProduct != null &&
                rewardRuleProduct.RewardAttributeSelectionType == RewardAttributeSelectionType.SpecificValues &&
                !string.IsNullOrWhiteSpace(rewardRuleProduct.RewardAttributeValueIds))
            {
                var requiredMappingIds = new HashSet<int>(requiredMappings.Select(x => x.Id));
                var valueIds = ParseIdList(rewardRuleProduct.RewardAttributeValueIds);
                if (valueIds.Any())
                {
                    var mappingIds = new HashSet<int>();
                    foreach (var valueId in valueIds)
                    {
                        var value = _productAttributeService.GetProductAttributeValueById(valueId);
                        if (value != null)
                            mappingIds.Add(value.ProductAttributeMappingId);
                    }

                    if (requiredMappingIds.All(mappingIds.Contains))
                        return false;
                }
            }

            return true;
        }

        public IList<ShoppingCartItem> FilterRewardItemsByAttributes(IList<ShoppingCartItem> items, string attributesXml)
        {
            var matched = new List<ShoppingCartItem>();
            if (items == null || !items.Any())
                return matched;

            foreach (var item in items)
            {
                if (_productAttributeParser.AreProductAttributesEqual(item.AttributesXml, attributesXml, true))
                    matched.Add(item);
            }

            return matched;
        }

        public void TrackManualReward(
            Customer customer,
            int storeId,
            int promotionRuleId,
            int productId,
            string attributesXml,
            int quantityAdded)
        {
            if (customer == null || customer.Id <= 0 || promotionRuleId <= 0 || productId <= 0 || quantityAdded <= 0)
                return;

            var cart = GetCustomerCart(customer, storeId);
            if (!cart.Any())
                return;

            var descriptor = BuildRewardDescriptor(productId, attributesXml ?? string.Empty);
            var matchingItems = cart.Where(x => x.ProductId == productId).ToList();
            if (!string.IsNullOrWhiteSpace(attributesXml))
                matchingItems = FilterRewardItemsByAttributes(matchingItems, attributesXml).ToList();
            else
                matchingItems = matchingItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

            var item = matchingItems.FirstOrDefault();
            if (item == null)
                return;

            var currentManagedQuantity = item.GetAttribute<int>(ManualRewardManagedQuantityAttribute, _genericAttributeService);
            _genericAttributeService.SaveAttribute(item, ManualRewardDescriptorAttribute, descriptor);
            _genericAttributeService.SaveAttribute(item, ManualRewardManagedQuantityAttribute, currentManagedQuantity + quantityAdded);
            _genericAttributeService.SaveAttribute(item, ManualRewardRuleIdAttribute, promotionRuleId);
        }

        public void SynchronizeAutoAddedRewards(
            Customer customer,
            IList<ShoppingCartItem> cart,
            IList<AppliedPromotion> appliedPromotions,
            int storeId = 0)
        {
            if (customer == null || customer.Id <= 0)
                return;

            if (cart == null)
                cart = new ShoppingCartItem[0];
            if (appliedPromotions == null)
                appliedPromotions = new AppliedPromotion[0];

            var requiredRewards = BuildRequiredRewards(appliedPromotions);
            ReconcileManagedRewards(customer, cart, requiredRewards, storeId);
            SynchronizeManualRewards(customer, cart, appliedPromotions, storeId);
        }

        private void SynchronizeManualRewards(
            Customer customer,
            IList<ShoppingCartItem> cart,
            IList<AppliedPromotion> appliedPromotions,
            int storeId)
        {
            if (cart == null)
                cart = new ShoppingCartItem[0];
            if (appliedPromotions == null)
                appliedPromotions = new AppliedPromotion[0];

            var requiredQuantitiesByRuleId = appliedPromotions
                .Where(x => x.RequiresRewardSelection)
                .GroupBy(x => x.PromotionRuleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(x => x.RewardQuantity > 0 ? x.RewardQuantity : 1));

            var managedGroups = BuildManualGroups(cart);
            foreach (var managedGroup in managedGroups)
            {
                var quantity = 0;
                var requiredQuantity = requiredQuantitiesByRuleId.TryGetValue(managedGroup.Key, out quantity)
                    ? quantity
                    : 0;

                var currentManagedQuantity = managedGroup.Value.Sum(x => x.ManagedQuantity);
                if (currentManagedQuantity <= requiredQuantity)
                    continue;

                ReduceManualQuantity(customer, managedGroup.Value, currentManagedQuantity - requiredQuantity);
            }
        }

        private Dictionary<int, List<ManagedRewardLine>> BuildManualGroups(IList<ShoppingCartItem> cart)
        {
            var result = new Dictionary<int, List<ManagedRewardLine>>();
            foreach (var item in cart.Where(x => x != null && x.Quantity > 0))
            {
                var ruleId = item.GetAttribute<int>(ManualRewardRuleIdAttribute, _genericAttributeService);
                var managedQuantity = item.GetAttribute<int>(ManualRewardManagedQuantityAttribute, _genericAttributeService);
                if (ruleId <= 0 || managedQuantity <= 0)
                    continue;

                List<ManagedRewardLine> lines;
                if (!result.TryGetValue(ruleId, out lines))
                {
                    lines = new List<ManagedRewardLine>();
                    result[ruleId] = lines;
                }

                lines.Add(new ManagedRewardLine
                {
                    Item = item,
                    ManagedQuantity = Math.Min(item.Quantity, managedQuantity)
                });
            }

            return result;
        }

        private void ReduceManualQuantity(Customer customer, IList<ManagedRewardLine> managedLines, int quantityToRemove)
        {
            foreach (var managedLine in managedLines.OrderByDescending(x => x.ManagedQuantity))
            {
                if (quantityToRemove <= 0)
                    break;

                var removeFromLine = Math.Min(managedLine.ManagedQuantity, quantityToRemove);
                if (removeFromLine <= 0)
                    continue;

                var newManagedQuantity = managedLine.ManagedQuantity - removeFromLine;
                var newTotalQuantity = managedLine.Item.Quantity - removeFromLine;
                if (newTotalQuantity <= 0)
                {
                    _shoppingCartService.DeleteShoppingCartItem(managedLine.Item, false, false);
                }
                else
                {
                    var warnings = _shoppingCartService.UpdateShoppingCartItem(
                        customer,
                        managedLine.Item.Id,
                        managedLine.Item.AttributesXml,
                        managedLine.Item.CustomerEnteredPrice,
                        managedLine.Item.RentalStartDateUtc,
                        managedLine.Item.RentalEndDateUtc,
                        newTotalQuantity,
                        false);

                    if (warnings.Any())
                    {
                        DiscountManagerPlusLog.Information(_logger,
                            string.Format("DiscountManagerPlusService: failed to reduce selected reward quantity. Product id = {0}. Warnings: {1}",
                                managedLine.Item.ProductId, string.Join(" | ", warnings)));
                        continue;
                    }

                    _genericAttributeService.SaveAttribute(managedLine.Item, ManualRewardManagedQuantityAttribute, Math.Max(0, newManagedQuantity));
                    var descriptor = newManagedQuantity > 0
                        ? managedLine.Item.GetAttribute<string>(ManualRewardDescriptorAttribute, _genericAttributeService)
                        : string.Empty;
                    _genericAttributeService.SaveAttribute(managedLine.Item, ManualRewardDescriptorAttribute, descriptor ?? string.Empty);
                    if (newManagedQuantity <= 0)
                        _genericAttributeService.SaveAttribute(managedLine.Item, ManualRewardRuleIdAttribute, 0);
                }

                quantityToRemove -= removeFromLine;
            }
        }

        private Dictionary<string, RewardRequirement> BuildRequiredRewards(IList<AppliedPromotion> appliedPromotions)
        {
            var requiredRewards = new Dictionary<string, RewardRequirement>(StringComparer.Ordinal);
            foreach (var applied in appliedPromotions.Where(x => x.AutoAddReward && x.RewardProductId.HasValue && x.RewardQuantity > 0))
            {
                var rewardProductId = applied.RewardProductId.Value;
                var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(applied.PromotionRuleId);
                var rewardRuleProduct = ruleProducts.FirstOrDefault(x => x.IsRewardProduct && x.ProductId == rewardProductId);
                var rewardProduct = _productService.GetProductById(rewardProductId);
                if (RequiresRewardSelection(rewardRuleProduct, rewardProduct))
                    continue;

                var attributesXml = rewardRuleProduct != null
                    ? BuildRewardAttributesXml(rewardRuleProduct)
                    : string.Empty;
                var descriptor = BuildRewardDescriptor(rewardProductId, attributesXml);
                RewardRequirement requirement;
                if (!requiredRewards.TryGetValue(descriptor, out requirement))
                {
                    requirement = new RewardRequirement
                    {
                        ProductId = rewardProductId,
                        AttributesXml = attributesXml ?? string.Empty,
                        RequiredQuantity = 0
                    };
                    requiredRewards[descriptor] = requirement;
                }

                requirement.RequiredQuantity += applied.RewardQuantity;
            }

            return requiredRewards;
        }

        private void ReconcileManagedRewards(
            Customer customer,
            IList<ShoppingCartItem> cart,
            IDictionary<string, RewardRequirement> requiredRewards,
            int storeId)
        {
            var managedGroups = BuildManagedGroups(cart);

            foreach (var managedGroup in managedGroups)
            {
                RewardRequirement requirement;
                var requiredQuantity = requiredRewards.TryGetValue(managedGroup.Key, out requirement)
                    ? requirement.RequiredQuantity
                    : 0;

                var currentManagedQuantity = managedGroup.Value.Sum(x => x.ManagedQuantity);
                if (currentManagedQuantity <= requiredQuantity)
                    continue;

                ReduceManagedQuantity(customer, managedGroup.Value, currentManagedQuantity - requiredQuantity);
            }

            var latestCart = GetCustomerCart(customer, storeId);
            managedGroups = BuildManagedGroups(latestCart);

            foreach (var requiredReward in requiredRewards)
            {
                List<ManagedRewardLine> managedLines;
                var currentManagedQuantity = managedGroups.TryGetValue(requiredReward.Key, out managedLines)
                    ? managedLines.Sum(x => x.ManagedQuantity)
                    : 0;
                if (currentManagedQuantity >= requiredReward.Value.RequiredQuantity)
                    continue;

                AddManagedQuantity(
                    customer,
                    latestCart,
                    requiredReward.Key,
                    requiredReward.Value,
                    requiredReward.Value.RequiredQuantity - currentManagedQuantity,
                    storeId);

                latestCart = GetCustomerCart(customer, storeId);
                managedGroups = BuildManagedGroups(latestCart);
            }
        }

        private Dictionary<string, List<ManagedRewardLine>> BuildManagedGroups(IList<ShoppingCartItem> cart)
        {
            var result = new Dictionary<string, List<ManagedRewardLine>>(StringComparer.Ordinal);
            foreach (var item in cart.Where(x => x != null && x.Quantity > 0))
            {
                var descriptor = item.GetAttribute<string>(AutoRewardDescriptorAttribute, _genericAttributeService);
                var managedQuantity = item.GetAttribute<int>(AutoRewardManagedQuantityAttribute, _genericAttributeService);
                if (string.IsNullOrWhiteSpace(descriptor) || managedQuantity <= 0)
                    continue;

                List<ManagedRewardLine> lines;
                if (!result.TryGetValue(descriptor, out lines))
                {
                    lines = new List<ManagedRewardLine>();
                    result[descriptor] = lines;
                }

                lines.Add(new ManagedRewardLine
                {
                    Item = item,
                    ManagedQuantity = Math.Min(item.Quantity, managedQuantity)
                });
            }

            return result;
        }

        private void ReduceManagedQuantity(Customer customer, IList<ManagedRewardLine> managedLines, int quantityToRemove)
        {
            foreach (var managedLine in managedLines.OrderByDescending(x => x.ManagedQuantity))
            {
                if (quantityToRemove <= 0)
                    break;

                var removeFromLine = Math.Min(managedLine.ManagedQuantity, quantityToRemove);
                if (removeFromLine <= 0)
                    continue;

                var newManagedQuantity = managedLine.ManagedQuantity - removeFromLine;
                var newTotalQuantity = managedLine.Item.Quantity - removeFromLine;
                if (newTotalQuantity <= 0)
                {
                    _shoppingCartService.DeleteShoppingCartItem(managedLine.Item, false, false);
                }
                else
                {
                    var warnings = _shoppingCartService.UpdateShoppingCartItem(
                        customer,
                        managedLine.Item.Id,
                        managedLine.Item.AttributesXml,
                        managedLine.Item.CustomerEnteredPrice,
                        managedLine.Item.RentalStartDateUtc,
                        managedLine.Item.RentalEndDateUtc,
                        newTotalQuantity,
                        false);

                    if (warnings.Any())
                    {
                        DiscountManagerPlusLog.Information(_logger,
                            string.Format("DiscountManagerPlusService: failed to reduce auto-added reward quantity. Product id = {0}. Warnings: {1}",
                                managedLine.Item.ProductId, string.Join(" | ", warnings)));
                        continue;
                    }

                    _genericAttributeService.SaveAttribute(managedLine.Item, AutoRewardManagedQuantityAttribute, Math.Max(0, newManagedQuantity));
                    var descriptor = newManagedQuantity > 0
                        ? managedLine.Item.GetAttribute<string>(AutoRewardDescriptorAttribute, _genericAttributeService)
                        : string.Empty;
                    _genericAttributeService.SaveAttribute(managedLine.Item, AutoRewardDescriptorAttribute, descriptor ?? string.Empty);
                }

                quantityToRemove -= removeFromLine;
            }
        }

        private void AddManagedQuantity(
            Customer customer,
            IList<ShoppingCartItem> cart,
            string descriptor,
            RewardRequirement requirement,
            int quantityToAdd,
            int storeId)
        {
            if (quantityToAdd <= 0)
                return;

            var matchingItems = cart.Where(x => x.ProductId == requirement.ProductId).ToList();
            if (!string.IsNullOrWhiteSpace(requirement.AttributesXml))
                matchingItems = FilterRewardItemsByAttributes(matchingItems, requirement.AttributesXml).ToList();
            else
                matchingItems = matchingItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

            if (matchingItems.Any())
            {
                var targetItem = matchingItems.First();
                var warnings = _shoppingCartService.UpdateShoppingCartItem(
                    customer,
                    targetItem.Id,
                    targetItem.AttributesXml,
                    targetItem.CustomerEnteredPrice,
                    targetItem.RentalStartDateUtc,
                    targetItem.RentalEndDateUtc,
                    targetItem.Quantity + quantityToAdd,
                    false);

                if (warnings.Any())
                {
                    DiscountManagerPlusLog.Information(_logger,
                        string.Format("DiscountManagerPlusService: failed to increase auto-added reward quantity. Product id = {0}. Warnings: {1}",
                            requirement.ProductId, string.Join(" | ", warnings)));
                    return;
                }
            }
            else
            {
                var rewardProduct = _productService.GetProductById(requirement.ProductId);
                if (rewardProduct == null || rewardProduct.Deleted)
                    return;

                var warnings = _shoppingCartService.AddToCart(
                    customer,
                    rewardProduct,
                    ShoppingCartType.ShoppingCart,
                    storeId,
                    requirement.AttributesXml,
                    decimal.Zero,
                    null,
                    null,
                    quantityToAdd,
                    false);

                if (warnings.Any())
                {
                    DiscountManagerPlusLog.Information(_logger,
                        string.Format("DiscountManagerPlusService: failed to auto-add reward product. Product id = {0}. Warnings: {1}",
                            requirement.ProductId, string.Join(" | ", warnings)));
                    return;
                }
            }

            var refreshedCart = GetCustomerCart(customer, storeId);
            var updatedItems = refreshedCart.Where(x => x.ProductId == requirement.ProductId).ToList();
            if (!string.IsNullOrWhiteSpace(requirement.AttributesXml))
                updatedItems = FilterRewardItemsByAttributes(updatedItems, requirement.AttributesXml).ToList();
            else
                updatedItems = updatedItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

            var updatedItem = updatedItems.FirstOrDefault();
            if (updatedItem == null)
                return;

            var currentManagedQuantity = updatedItem.GetAttribute<int>(AutoRewardManagedQuantityAttribute, _genericAttributeService);
            _genericAttributeService.SaveAttribute(updatedItem, AutoRewardDescriptorAttribute, descriptor);
            _genericAttributeService.SaveAttribute(updatedItem, AutoRewardManagedQuantityAttribute, currentManagedQuantity + quantityToAdd);
        }

        private static IList<ShoppingCartItem> GetCustomerCart(Customer customer, int storeId)
        {
            if (customer == null || customer.ShoppingCartItems == null)
                return new List<ShoppingCartItem>();

            return customer.ShoppingCartItems
                .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(storeId)
                .ToList();
        }

        private static string BuildRewardDescriptor(int productId, string attributesXml)
        {
            var normalizedAttributes = attributesXml ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedAttributes))
                return string.Format("{0}:none", productId);

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalizedAttributes));
                return string.Format("{0}:{1}", productId, BitConverter.ToString(bytes).Replace("-", string.Empty));
            }
        }

        private static IList<int> ParseIdList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new int[0];

            return raw
                .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Select(x =>
                {
                    var value = 0;
                    return int.TryParse(x, out value) ? value : 0;
                })
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        private class RewardRequirement
        {
            public int ProductId { get; set; }
            public string AttributesXml { get; set; }
            public int RequiredQuantity { get; set; }
        }

        private class ManagedRewardLine
        {
            public ShoppingCartItem Item { get; set; }
            public int ManagedQuantity { get; set; }
        }
    }
}
