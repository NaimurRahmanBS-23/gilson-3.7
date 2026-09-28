# Dual-Offer Discount Coordination - Step-by-Step Setup Guide

## Prerequisites

- nopCommerce 4.90+ installed
- DiscountManagerPlus plugin already installed and activated
- Admin access to configure promotion rules
- Basic understanding of nopCommerce discount system

## Step 1: Deploy Code Changes

### 1.1 Update DiscountCoordinationService.cs

**File Location:** `src/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Services/DiscountCoordinationService.cs`

**Replace the entire `CoordinateCheapestItemDiscountsAsync` method with this updated version:**

```csharp
public async Task<Dictionary<int, List<DiscountAllocation>>> CoordinateCheapestItemDiscountsAsync(
    IList<AppliedPromotion> appliedPromotions,
    IList<ShoppingCartItem> cart)
{
    var result = new Dictionary<int, List<DiscountAllocation>>();
    if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
        return result;

    // FIXED: Include all BuyXGetY promotions for coordination
    var cheapestItemPromotions = appliedPromotions
        .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
        .ToList();

    if (!cheapestItemPromotions.Any())
        return result;

    // Sort by effective discount value (highest first) to prioritize 100% discounts over 50%
    var sortedPromotions = cheapestItemPromotions
        .OrderByDescending(x => CalculateEffectiveDiscountValue(x))
        .ThenBy(x => x.RewardQuantity) // Then by reward quantity (smaller quantities first for efficiency)
        .ToList();

    // Track which cart items have already received discounts
    var allocatedLineIds = new HashSet<int>();

    foreach (var promotion in sortedPromotions)
    {
        var allocations = new List<DiscountAllocation>();

        // FIXED: Handle both cases - with and without pre-calculated LineDiscounts
        IList<int> eligibleLineIds;

        if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
        {
            // Use existing line discounts if available, but limit to RewardQuantity
            var sortedLineDiscounts = promotion.LineDiscounts
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Take(promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1)
                .ToList();

            eligibleLineIds = sortedLineDiscounts
                .Select(x => x.Key)
                .ToList();
        }
        else
        {
            // CRITICAL FIX: Only use cart items that are in the EligibleShoppingCartItemIds
            if (promotion.EligibleShoppingCartItemIds == null || !promotion.EligibleShoppingCartItemIds.Any())
            {
                continue; // If no eligible items specified, we can't apply this promotion
            }

            eligibleLineIds = promotion.EligibleShoppingCartItemIds
                .Where(lineId => cart.Any(item => item.Id == lineId && item.Quantity > 0))
                .ToList();
        }

        if (!eligibleLineIds.Any())
            continue;

        // Filter out already-allocated items
        var availableLineIds = eligibleLineIds
            .Where(lineId => !allocatedLineIds.Contains(lineId))
            .ToList();

        if (!availableLineIds.Any())
            continue;

        // Get the actual cart items
        var availableItems = cart
            .Where(item => availableLineIds.Contains(item.Id) && item.Quantity > 0)
            .ToList();

        if (!availableItems.Any())
            continue;

        // CRITICAL FIX: Limit the number of items to allocate based on RewardQuantity
        var requiredQuantity = promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1;
        var itemsToAllocate = Math.Min(requiredQuantity, availableItems.Count);

        // Select the cheapest available items (limited by RewardQuantity)
        var cheapestItems = await GetLowestPricedItemsExcludingAllocatedAsync(
            availableItems,
            allocatedLineIds,
            itemsToAllocate);

        // Create allocations for the cheapest items (limited by RewardQuantity)
        foreach (var item in cheapestItems)
        {
            if (item == null || item.Quantity <= 0)
                continue;

            var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(item, false);

            // CRITICAL FIX: Calculate discount amount properly based on discount type
            var discountAmount = 0m;
            if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any() &&
                promotion.LineDiscounts.TryGetValue(item.Id, out var existingDiscount))
            {
                discountAmount = existingDiscount;
            }
            else
            {
                // Calculate discount based on discount type
                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                {
                    discountAmount = unitPrice; // 100% discount
                }
                else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                {
                    var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;
                    discountAmount = unitPrice * (percentage / 100m);
                }
                else if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                {
                    discountAmount = Math.Min(promotion.DiscountValue, unitPrice);
                }
            }

            if (discountAmount <= 0)
                continue;

            // CRITICAL FIX: Only allocate 1 quantity per item to prevent double-discounting
            allocations.Add(new DiscountAllocation
            {
                LineId = item.Id,
                DiscountAmount = discountAmount,
                PromotionRuleId = promotion.PromotionRuleId,
                UnitPrice = unitPrice,
                Quantity = 1, // Always allocate 1 unit per line item
                ProductId = item.ProductId,
                IsFreeItem = promotion.DiscountTypeId == (int)DiscountType.FreeItem ||
                            discountAmount >= unitPrice
            });

            // Mark this item as allocated
            allocatedLineIds.Add(item.Id);

            // CRITICAL FIX: Stop allocating once we reach the RewardQuantity limit
            if (allocations.Count >= requiredQuantity)
                break;
        }

        if (allocations.Any())
        {
            result[promotion.PromotionRuleId] = allocations;
        }
    }

    return result;
}
```

### 1.2 Update CartSavingsViewComponent.cs

**File Location:** `src/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Components/CartSavingsViewComponent.cs`

**Replace the `BuildMultipleDiscountNoticesAsync` method with this enhanced version:**

```csharp
protected virtual async Task<IList<string>> BuildMultipleDiscountNoticesAsync(IList<AppliedPromotion> appliedPromotions)
{
    var notices = new List<string>();
    if (appliedPromotions == null || !appliedPromotions.Any())
        return notices;

    var appliedBuyXGetYRules = appliedPromotions
        .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && x.DiscountAmount > 0)
        .ToList();

    if (appliedBuyXGetYRules.Count >= 2)
    {
        // ENHANCED: Provide more detailed information about the coordinated discounts
        var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleDiscounts");
        var message = string.Format(template, appliedBuyXGetYRules.Count);
        notices.Add(message);

        // NEW: Add explanation about cheapest-item selection
        var coordinationTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CoordinatedDiscounts");
        var coordinationMessage = string.Format(coordinationTemplate, appliedBuyXGetYRules.Count);
        notices.Add(coordinationMessage);

        // NEW: Add specific details about which discounts are being applied
        foreach (var promotion in appliedBuyXGetYRules.OrderByDescending(x => x.DiscountAmount))
        {
            var discountTypeText = promotion.DiscountTypeId == (int)DiscountType.FreeItem
                ? await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo")
                : promotion.DiscountTypeId == (int)DiscountType.Percentage
                    ? $"{promotion.DiscountValue.ToString("0.##")}% {await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount")}"
                    : await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");

            var targetProductText = !string.IsNullOrEmpty(promotion.TargetProductName)
                ? $"{await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedTo")} {promotion.TargetProductName}"
                : await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedToCheapest");

            var promotionDetail = $"{promotion.RuleName}: {discountTypeText} - {targetProductText}";
            notices.Add(promotionDetail);
        }
    }

    return notices;
}
```

**Replace the `BuildCheapestItemSelectionDetailsAsync` method with this enhanced version:**

```csharp
protected virtual async Task<IList<string>> BuildCheapestItemSelectionDetailsAsync(
    IList<ShoppingCartItem> cart,
    IList<AppliedPromotion> appliedPromotions)
{
    var details = new List<string>();
    if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
        return details;

    try
    {
        var cheapestItemPromotions = appliedPromotions
            .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY &&
                       x.LineDiscounts != null &&
                       x.LineDiscounts.Any(d => d.Value > 0))
            .ToList();

        var isDualOfferScenario = cheapestItemPromotions.Count >= 2;

        foreach (var promotion in cheapestItemPromotions)
        {
            if (promotion.DiscountAmount <= 0)
                continue;

            if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
            {
                foreach (var lineDiscount in promotion.LineDiscounts.Where(x => x.Value > 0))
                {
                    var discountedItem = cart.FirstOrDefault(x => x.Id == lineDiscount.Key);
                    if (discountedItem != null)
                    {
                        var product = await _productService.GetProductByIdAsync(discountedItem.ProductId);
                        if (product != null)
                        {
                            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                            {
                                var freeItemTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied");
                                var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                var message = string.Format(freeItemTemplate, product.Name, discountAmount);
                                details.Add(message);
                            }
                            else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                            {
                                var percentageTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied");
                                var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
                                var message = string.Format(percentageTemplate, product.Name, percentage, discountAmount);
                                details.Add(message);
                            }
                            else
                            {
                                var discountType = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
                                var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemDetail");
                                var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                var message = string.Format(template, discountType, product.Name, discountAmount);
                                details.Add(message);
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(promotion.TargetProductName))
            {
                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                {
                    var freeItemTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied");
                    var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                    var message = string.Format(freeItemTemplate, promotion.TargetProductName, discountAmount);
                    details.Add(message);
                }
                else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                {
                    var percentageTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied");
                    var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                    var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
                    var message = string.Format(percentageTemplate, promotion.TargetProductName, percentage, discountAmount);
                    details.Add(message);
                }
                else
                {
                    var discountType = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
                    var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemDetail");
                    var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                    var message = string.Format(template, discountType, promotion.TargetProductName, discountAmount);
                    details.Add(message);
                }
            }
        }

        // NEW: Add a summary message for dual-offer scenarios
        if (isDualOfferScenario && details.Any())
        {
            var coordinationSummary = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.DualOfferSummary");
            details.Insert(0, coordinationSummary);
        }
    }
    catch (Exception ex)
    {
        await _logger.ErrorAsync("Error building cheapest item selection details", ex);
    }

    return details;
}
```

## Step 2: Add Localization Resources

### 2.1 Create Localization XML

**File Location:** Create or edit: `Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Localization/Resources/DiscountManagerPlus.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Language Name="English">
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleDiscounts">
    <Value>✨ {0} offers applied to your cart!</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.CoordinatedDiscounts">
    <Value>💡 We've applied the best available discounts to your cart automatically. Free items and percentage discounts are applied to the cheapest eligible products.</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedTo">
    <Value>Applied to</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedToCheapest">
    <Value>Applied to cheapest eligible product</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied">
    <Value>🎁 {0}: FREE (Save {1})</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied">
    <Value>💰 {0}: {1}% OFF (Save {2})</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.DualOfferSummary">
    <Value>📊 Dual Offer Summary: Multiple promotions optimally applied to different products.</Value>
  </Resource>
  
  <Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.SavingsBreakdown">
    <Value>💰 Savings Breakdown</Value>
  </Resource>
</Language>
```

## Step 3: Configure Promotion Rules

### 3.1 Create "Buy 2 Get 1 Free" Rule

1. **Navigate to Admin Panel:**
   - Go to **Nop-Station → Plugins → Discount Manager Plus → Promotion Rules**

2. **Create New Rule:**
   - Click **Add New**
   - **Rule Name:** "Buy 2 Get 1 Free"
   - **Rule Type:** Select "Buy X Get Y"
   - **Active:** Check the box
   - **Priority:** Set to 10 (higher priority)

3. **Configure Discount:**
   - **Discount Type:** Select "Free Item (100% discount)"
   - **Discount Value:** 100 (for 100% discount)

4. **Configure Tiers:**
   - Click **Add New Tier**
   - **Buy Quantity:** 2 (customer buys 2 items)
   - **Reward Quantity:** 1 (gets 1 free)
   - **Min Quantity:** 2 (minimum to qualify)
   - **Save** the tier

5. **Configure Products:**
   - **Buy Products:** Add products that qualify for the promotion
     - Choose "All Products" or select specific products/categories
   - **Reward Products:** Same as buy products (or select specific reward products)

6. **Save Rule**

### 3.2 Create "Buy 1 Get 50% Off" Rule

1. **Create Another New Rule:**
   - **Rule Name:** "Buy 1 Get 50% Off"
   - **Rule Type:** Select "Buy X Get Y"
   - **Active:** Check the box
   - **Priority:** Set to 5 (lower priority than 100% discount)

2. **Configure Discount:**
   - **Discount Type:** Select "Percentage"
   - **Discount Value:** 50 (for 50% discount)

3. **Configure Tiers:**
   - **Buy Quantity:** 1 (customer buys 1 item)
   - **Reward Quantity:** 1 (gets 50% off 1 item)
   - **Min Quantity:** 1 (minimum to qualify)
   - **Save** the tier

4. **Configure Products:**
   - **Buy Products:** Same products as first rule (or compatible products)
   - **Reward Products:** Same as buy products

5. **Save Rule**

## Step 4: Test the Configuration

### 4.1 Create Test Products

1. **Navigate to:** **Catalog → Products**
2. **Create 5 test products:**
   - Product A: Price $10.00
   - Product B: Price $20.00
   - Product C: Price $30.00
   - Product D: Price $40.00
   - Product E: Price $50.00

### 4.2 Test Scenario

1. **Add Products to Cart:**
   - Add all 5 products to shopping cart (1 quantity each)

2. **Expected Results:**
   - **Product A ($10):** Should show as **FREE** (100% discount)
   - **Product B ($20):** Should show as **$10** (50% discount)  
   - **Products C, D, E:** Should show normal price
   - **Total Discount:** $20.00
   - **Cart Total:** $110.00 (instead of $130.00)

3. **Verify Cart Savings Display:**
   - Scroll to cart savings section
   - Should see: "✨ 2 offers applied to your cart!"
   - Should see coordination explanation
   - Should see detailed breakdown per product

### 4.3 Verify Discount Application

1. **Check Cart Totals:**
   - Subtotal: $110.00
   - Total Savings: $20.00
   - Applied Promotions: 2

2. **Check Attention Messages:**
   - Multiple discount notice visible
   - Cheapest item details shown
   - Clear indication of which products got which discounts

## Step 5: Verify and Troubleshoot

### 5.1 Common Issues

**Issue 1: Both discounts applied to same product**
- **Check:** DiscountCoordinationService code is updated
- **Solution:** Verify `allocatedLineIds.Add(item.Id)` is working

**Issue 2: Wrong products get discounts**
- **Check:** Promotion rules product configuration
- **Solution:** Ensure both rules have compatible product settings

**Issue 3: Attention messages not showing**
- **Check:** Localization resources are added
- **Solution:** Clear cache and restart application

**Issue 4: No discounts applied**
- **Check:** Both promotion rules are Active
- **Solution:** Verify priority settings and product eligibility

### 5.2 Debug Mode

Enable detailed logging in `appsettings.json`:

```json
{
  "NopSettings": {
    "DiscountManagerPlus": {
      "EnableDebugLogging": true,
      "LogDiscountCalculations": true
    }
  }
}
```

## Step 6: Deploy to Production

### 6.1 Pre-Deployment Checklist

- [ ] Code changes tested in development environment
- [ ] Localization resources added and tested
- [ ] Promotion rules configured correctly
- [ ] Test scenarios executed successfully
- [ ] Performance impact assessed (should be minimal)
- [ ] User acceptance testing completed

### 6.2 Deployment Steps

1. **Backup current plugin files**
2. **Deploy updated files:**
   - `DiscountCoordinationService.cs`
   - `CartSavingsViewComponent.cs`
3. **Add/update localization resources**
4. **Clear application cache**
5. **Restart application**
6. **Verify promotion rules are still active**
7. **Test with sample cart**

### 6.3 Post-Deployment Monitoring

- Monitor cart performance (should be minimal impact)
- Check user feedback on attention messages
- Verify discount calculations are accurate
- Monitor for any error logs

## Step 7: Advanced Configuration (Optional)

### 7.1 Create 3+ Promotion Rules

You can extend to support more than 2 promotions:

1. **Buy 3 Get 2 Free (100% + 50% + 25%)**
2. **Tiered discounts based on cart total**
3. **Category-specific coordination**

### 7.2 Custom Localization

Add custom messages for specific scenarios:

```xml
<Resource Name="Plugins.NopStation.DiscountManagerPlus.CartSavings.CustomMessage">
  <Value>Your custom message here</Value>
</Resource>
```

## Quick Setup Checklist

- [ ] Code files updated and compiled
- [ ] Localization resources added
- [ ] Promotion rules created (2+ rules)
- [ ] Test products created
- [ ] Test scenario executed successfully
- [ ] Attention messages verified
- [ ] Cart totals verified
- [ ] Production deployment completed
- [ ] Monitoring setup in place

---

## Support and Troubleshooting

**If you encounter issues:**

1. **Check Logs:** Look for DiscountManagerPlus log entries
2. **Verify Configuration:** Ensure promotion rules are active and compatible
3. **Test Individually:** Test each promotion rule separately first
4. **Clear Cache:** Clear application and browser cache
5. **Check Product Eligibility:** Ensure products are properly configured

**Expected Timeline:**
- Setup: 30-45 minutes
- Testing: 15-30 minutes
- Total: 1-2 hours

---

*This setup guide covers all steps needed to implement dual-offer discount coordination. For additional support, refer to the documentation files created with this implementation.*