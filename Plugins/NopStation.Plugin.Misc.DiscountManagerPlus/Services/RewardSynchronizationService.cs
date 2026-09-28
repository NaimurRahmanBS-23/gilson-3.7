using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

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

    public async Task<string> BuildRewardAttributesXmlAsync(PromotionRuleProduct rewardProduct)
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
            var value = await _productAttributeService.GetProductAttributeValueByIdAsync(valueId);
            if (value == null)
                continue;

            var mapping = await _productAttributeService.GetProductAttributeMappingByIdAsync(value.ProductAttributeMappingId);
            if (mapping == null)
                continue;

            attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, mapping, value.Id.ToString());
        }

        return attributesXml;
    }

    public async Task<bool> RequiresRewardSelectionAsync(PromotionRuleProduct rewardRuleProduct, Product rewardProduct)
    {
        if (rewardProduct == null)
            return false;

        if (rewardProduct.CustomerEntersPrice || rewardProduct.IsGiftCard || rewardProduct.IsRental)
            return true;

        var requiredMappings = (await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(rewardProduct.Id))
            .Where(x => x.IsRequired)
            .ToList();
        if (!requiredMappings.Any())
            return false;

        if (rewardRuleProduct != null &&
            rewardRuleProduct.RewardAttributeSelectionType == RewardAttributeSelectionType.SpecificValues &&
            !string.IsNullOrWhiteSpace(rewardRuleProduct.RewardAttributeValueIds))
        {
            var requiredMappingIds = requiredMappings.Select(x => x.Id).ToHashSet();
            var valueIds = ParseIdList(rewardRuleProduct.RewardAttributeValueIds);
            if (valueIds.Any())
            {
                var mappingIds = new HashSet<int>();
                foreach (var valueId in valueIds)
                {
                    var value = await _productAttributeService.GetProductAttributeValueByIdAsync(valueId);
                    if (value != null)
                        mappingIds.Add(value.ProductAttributeMappingId);
                }

                if (requiredMappingIds.All(mappingIds.Contains))
                    return false;
            }
        }

        return true;
    }

    public async Task<IList<ShoppingCartItem>> FilterRewardItemsByAttributesAsync(IList<ShoppingCartItem> items, string attributesXml)
    {
        var matched = new List<ShoppingCartItem>();
        if (items == null || !items.Any())
            return matched;

        foreach (var item in items)
        {
            if (await _productAttributeParser.AreProductAttributesEqualAsync(item.AttributesXml, attributesXml, true, true))
                matched.Add(item);
        }

        return matched;
    }

    public async Task TrackManualRewardAsync(
        Customer customer,
        int storeId,
        int promotionRuleId,
        int productId,
        string attributesXml,
        int quantityAdded)
    {
        if (customer == null || customer.Id <= 0 || promotionRuleId <= 0 || productId <= 0 || quantityAdded <= 0)
            return;

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        if (!cart.Any())
            return;

        var descriptor = BuildRewardDescriptor(productId, attributesXml ?? string.Empty);
        var matchingItems = cart.Where(x => x.ProductId == productId).ToList();
        if (!string.IsNullOrWhiteSpace(attributesXml))
            matchingItems = (await FilterRewardItemsByAttributesAsync(matchingItems, attributesXml)).ToList();
        else
            matchingItems = matchingItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

        var item = matchingItems.FirstOrDefault();
        if (item == null)
            return;

        var currentManagedQuantity = await _genericAttributeService.GetAttributeAsync<int>(item, ManualRewardManagedQuantityAttribute);
        await _genericAttributeService.SaveAttributeAsync(item, ManualRewardDescriptorAttribute, descriptor);
        await _genericAttributeService.SaveAttributeAsync(item, ManualRewardManagedQuantityAttribute, currentManagedQuantity + quantityAdded);
        await _genericAttributeService.SaveAttributeAsync(item, ManualRewardRuleIdAttribute, promotionRuleId);
    }

    public async Task SynchronizeAutoAddedRewardsAsync(
        Customer customer,
        IList<ShoppingCartItem> cart,
        IList<AppliedPromotion> appliedPromotions,
        int storeId = 0)
    {
        if (customer == null || customer.Id <= 0)
            return;

        cart ??= Array.Empty<ShoppingCartItem>();
        appliedPromotions ??= Array.Empty<AppliedPromotion>();

        var requiredRewards = await BuildRequiredRewardsAsync(appliedPromotions);
        await ReconcileManagedRewardsAsync(customer, cart, requiredRewards, storeId);
        await SynchronizeManualRewardsAsync(customer, cart, appliedPromotions, storeId);
    }

    private async Task SynchronizeManualRewardsAsync(
        Customer customer,
        IList<ShoppingCartItem> cart,
        IList<AppliedPromotion> appliedPromotions,
        int storeId)
    {
        cart ??= Array.Empty<ShoppingCartItem>();
        appliedPromotions ??= Array.Empty<AppliedPromotion>();

        var requiredQuantitiesByRuleId = appliedPromotions
            .Where(x => x.RequiresRewardSelection)
            .GroupBy(x => x.PromotionRuleId)
            .ToDictionary(
                g => g.Key,
                g => g.Max(x => x.RewardQuantity > 0 ? x.RewardQuantity : 1));

        var managedGroups = await BuildManualGroupsAsync(cart);
        foreach (var managedGroup in managedGroups)
        {
            var requiredQuantity = requiredQuantitiesByRuleId.TryGetValue(managedGroup.Key, out var quantity)
                ? quantity
                : 0;

            var currentManagedQuantity = managedGroup.Value.Sum(x => x.ManagedQuantity);
            if (currentManagedQuantity <= requiredQuantity)
                continue;

            await ReduceManualQuantityAsync(customer, managedGroup.Value, currentManagedQuantity - requiredQuantity);
        }
    }

    private async Task<Dictionary<int, List<ManagedRewardLine>>> BuildManualGroupsAsync(IList<ShoppingCartItem> cart)
    {
        var result = new Dictionary<int, List<ManagedRewardLine>>();
        foreach (var item in cart.Where(x => x != null && x.Quantity > 0))
        {
            var ruleId = await _genericAttributeService.GetAttributeAsync<int>(item, ManualRewardRuleIdAttribute);
            var managedQuantity = await _genericAttributeService.GetAttributeAsync<int>(item, ManualRewardManagedQuantityAttribute);
            if (ruleId <= 0 || managedQuantity <= 0)
                continue;

            if (!result.TryGetValue(ruleId, out var lines))
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

    private async Task ReduceManualQuantityAsync(Customer customer, IList<ManagedRewardLine> managedLines, int quantityToRemove)
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
                await _shoppingCartService.DeleteShoppingCartItemAsync(managedLine.Item, false, false);
            }
            else
            {
                var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(
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
                    await _logger.InformationAsync(
                        $"DiscountManagerPlusService: failed to reduce selected reward quantity. Product id = {managedLine.Item.ProductId}. Warnings: {string.Join(" | ", warnings)}");
                    continue;
                }

                await _genericAttributeService.SaveAttributeAsync(managedLine.Item, ManualRewardManagedQuantityAttribute, Math.Max(0, newManagedQuantity));
                var descriptor = newManagedQuantity > 0
                    ? await _genericAttributeService.GetAttributeAsync<string>(managedLine.Item, ManualRewardDescriptorAttribute)
                    : string.Empty;
                await _genericAttributeService.SaveAttributeAsync(managedLine.Item, ManualRewardDescriptorAttribute, descriptor ?? string.Empty);
                if (newManagedQuantity <= 0)
                    await _genericAttributeService.SaveAttributeAsync(managedLine.Item, ManualRewardRuleIdAttribute, 0);
            }

            quantityToRemove -= removeFromLine;
        }
    }

    private async Task<Dictionary<string, RewardRequirement>> BuildRequiredRewardsAsync(IList<AppliedPromotion> appliedPromotions)
    {
        var requiredRewards = new Dictionary<string, RewardRequirement>(StringComparer.Ordinal);
        foreach (var applied in appliedPromotions.Where(x => x.AutoAddReward && x.RewardProductId.HasValue && x.RewardQuantity > 0))
        {
            var rewardProductId = applied.RewardProductId.Value;
            var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(applied.PromotionRuleId);
            var rewardRuleProduct = ruleProducts.FirstOrDefault(x => x.IsRewardProduct && x.ProductId == rewardProductId);
            var rewardProduct = await _productService.GetProductByIdAsync(rewardProductId);
            if (await RequiresRewardSelectionAsync(rewardRuleProduct, rewardProduct))
                continue;

            var attributesXml = rewardRuleProduct != null
                ? await BuildRewardAttributesXmlAsync(rewardRuleProduct)
                : string.Empty;
            var descriptor = BuildRewardDescriptor(rewardProductId, attributesXml);
            if (!requiredRewards.TryGetValue(descriptor, out var requirement))
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

    private async Task ReconcileManagedRewardsAsync(
        Customer customer,
        IList<ShoppingCartItem> cart,
        IDictionary<string, RewardRequirement> requiredRewards,
        int storeId)
    {
        var managedGroups = await BuildManagedGroupsAsync(cart);

        foreach (var managedGroup in managedGroups)
        {
            var requiredQuantity = requiredRewards.TryGetValue(managedGroup.Key, out var requirement)
                ? requirement.RequiredQuantity
                : 0;

            var currentManagedQuantity = managedGroup.Value.Sum(x => x.ManagedQuantity);
            if (currentManagedQuantity <= requiredQuantity)
                continue;

            await ReduceManagedQuantityAsync(customer, managedGroup.Value, currentManagedQuantity - requiredQuantity);
        }

        var latestCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        managedGroups = await BuildManagedGroupsAsync(latestCart);

        foreach (var requiredReward in requiredRewards)
        {
            var currentManagedQuantity = managedGroups.TryGetValue(requiredReward.Key, out var managedLines)
                ? managedLines.Sum(x => x.ManagedQuantity)
                : 0;
            if (currentManagedQuantity >= requiredReward.Value.RequiredQuantity)
                continue;

            await AddManagedQuantityAsync(
                customer,
                latestCart,
                requiredReward.Key,
                requiredReward.Value,
                requiredReward.Value.RequiredQuantity - currentManagedQuantity,
                storeId);

            latestCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            managedGroups = await BuildManagedGroupsAsync(latestCart);
        }
    }

    private async Task<Dictionary<string, List<ManagedRewardLine>>> BuildManagedGroupsAsync(IList<ShoppingCartItem> cart)
    {
        var result = new Dictionary<string, List<ManagedRewardLine>>(StringComparer.Ordinal);
        foreach (var item in cart.Where(x => x != null && x.Quantity > 0))
        {
            var descriptor = await _genericAttributeService.GetAttributeAsync<string>(item, AutoRewardDescriptorAttribute);
            var managedQuantity = await _genericAttributeService.GetAttributeAsync<int>(item, AutoRewardManagedQuantityAttribute);
            if (string.IsNullOrWhiteSpace(descriptor) || managedQuantity <= 0)
                continue;

            if (!result.TryGetValue(descriptor, out var lines))
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

    private async Task ReduceManagedQuantityAsync(Customer customer, IList<ManagedRewardLine> managedLines, int quantityToRemove)
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
                await _shoppingCartService.DeleteShoppingCartItemAsync(managedLine.Item, false, false);
            }
            else
            {
                var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(
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
                    await _logger.InformationAsync(
                        $"DiscountManagerPlusService: failed to reduce auto-added reward quantity. Product id = {managedLine.Item.ProductId}. Warnings: {string.Join(" | ", warnings)}");
                    continue;
                }

                await _genericAttributeService.SaveAttributeAsync(managedLine.Item, AutoRewardManagedQuantityAttribute, Math.Max(0, newManagedQuantity));
                var descriptor = newManagedQuantity > 0
                    ? await _genericAttributeService.GetAttributeAsync<string>(managedLine.Item, AutoRewardDescriptorAttribute)
                    : string.Empty;
                await _genericAttributeService.SaveAttributeAsync(managedLine.Item, AutoRewardDescriptorAttribute, descriptor ?? string.Empty);
            }

            quantityToRemove -= removeFromLine;
        }
    }

    private async Task AddManagedQuantityAsync(
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
            matchingItems = (await FilterRewardItemsByAttributesAsync(matchingItems, requirement.AttributesXml)).ToList();
        else
            matchingItems = matchingItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

        if (matchingItems.Any())
        {
            var targetItem = matchingItems.First();
            var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(
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
                await _logger.InformationAsync(
                    $"DiscountManagerPlusService: failed to increase auto-added reward quantity. Product id = {requirement.ProductId}. Warnings: {string.Join(" | ", warnings)}");
                return;
            }
        }
        else
        {
            var rewardProduct = await _productService.GetProductByIdAsync(requirement.ProductId);
            if (rewardProduct == null || rewardProduct.Deleted)
                return;

            var warnings = await _shoppingCartService.AddToCartAsync(
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
                await _logger.InformationAsync(
                    $"DiscountManagerPlusService: failed to auto-add reward product. Product id = {requirement.ProductId}. Warnings: {string.Join(" | ", warnings)}");
                return;
            }
        }

        var refreshedCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        var updatedItems = refreshedCart.Where(x => x.ProductId == requirement.ProductId).ToList();
        if (!string.IsNullOrWhiteSpace(requirement.AttributesXml))
            updatedItems = (await FilterRewardItemsByAttributesAsync(updatedItems, requirement.AttributesXml)).ToList();
        else
            updatedItems = updatedItems.Where(x => string.IsNullOrWhiteSpace(x.AttributesXml)).ToList();

        var updatedItem = updatedItems.FirstOrDefault();
        if (updatedItem == null)
            return;

        var currentManagedQuantity = await _genericAttributeService.GetAttributeAsync<int>(updatedItem, AutoRewardManagedQuantityAttribute);
        await _genericAttributeService.SaveAttributeAsync(updatedItem, AutoRewardDescriptorAttribute, descriptor);
        await _genericAttributeService.SaveAttributeAsync(updatedItem, AutoRewardManagedQuantityAttribute, currentManagedQuantity + quantityToAdd);
    }

    private static string BuildRewardDescriptor(int productId, string attributesXml)
    {
        var normalizedAttributes = attributesXml ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedAttributes))
            return $"{productId}:none";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedAttributes));
        return $"{productId}:{Convert.ToHexString(bytes)}";
    }

    private static IList<int> ParseIdList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<int>();

        return raw
            .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var value) ? value : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
    }

    private sealed class RewardRequirement
    {
        public int ProductId { get; set; }
        public string AttributesXml { get; set; } = string.Empty;
        public int RequiredQuantity { get; set; }
    }

    private sealed class ManagedRewardLine
    {
        public ShoppingCartItem Item { get; set; } = default!;
        public int ManagedQuantity { get; set; }
    }
}
