# 🚨 **CRITICAL FIX: Quantity-Based Discount Allocation**

## 🎯 **The Core Problem:**

**Current Issue:** Both discounts apply to the **same line item** instead of **different units** of the same product.

**Example:**
```
Cart: Product B, Quantity = 2, Price = $100 per unit

Current (WRONG):
LineDiscount[ProductB] = $150 (both discounts merged)
→ Both units get some share of both discounts ❌

Required (CORRECT):
Unit 1: $100 discount (100% off) = $0
Unit 2: $50 discount (50% off) = $50  
→ Line total: $50 (not $150) ✅
```

---

## 🔧 **ROOT CAUSE:**

The discount allocation system is using **line-level discount aggregation** instead of **unit-level quantity allocation**.

**Problem Flow:**
1. Offer 1 (Buy 2 Get 1 Free) calculates: $100 discount for Product B
2. Offer 2 (Buy 1 Get 50% Off) calculates: $50 discount for Product B  
3. System **MERGES** both: LineDiscount[ProductB] = $150
4. Cart displays: Product B gets $150 off (both discounts merged) ❌

**Required Flow:**
1. Offer 1 (Buy 2 Get 1 Free) allocates: **1 unit × $100 = $100 discount**
2. Offer 2 (Buy 1 Get 50% Off) allocates: **1 unit × $50 = $50 discount**  
3. System **TRACKS** which units were discounted
4. Cart displays: **2 units with different discounts** ✅

---

## ✅ **THE FIX:**

### **Enhanced Quantity Tracking Implementation**

The issue is that the system needs to properly track **how many units of each cart line** have been discounted, not just **which cart lines received discounts**.

### **Step 1: Enhance Unit-Level Tracking**

**Location:** `PromotionDiscountAllocator.cs` - `CalculateRewardDiscountsAsync` method

**Current Logic (Lines 293-314):**
```csharp
var remaining = rewardQuantity;
foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
{
    var takeQty = Math.Min(availableQuantity, remaining);
    var discount = item.UnitPrice * takeQty;
    discounts[item.Item.Id] = discount;
    remaining -= takeQty;
}
```

**Problem:** This sets `discounts[item.Item.Id] = discount` which **overwrites** any existing discount for that line item.

### **Step 2: Fixed Logic**

**Enhanced Implementation:**
```csharp
// Track how many units were discounted for each line
var discountedQuantitiesByLineId = new Dictionary<int, int>();

var remaining = rewardQuantity;
foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
{
    var availableQuantity = normalizedQuantities.TryGetValue(item.Item.Id, out var overriddenQuantity)
        ? overriddenQuantity
        : item.Item.Quantity;
    
    var takeQty = Math.Min(availableQuantity, remaining);
    var discount = item.UnitPrice * takeQty;
    
    // CRITICAL: Track quantity separately from discount amount
    if (discounts.ContainsKey(item.Item.Id))
    {
        discounts[item.Item.Id] += discount; // Add to existing discount
        discountedQuantitiesByLineId[item.Item.Id] += takeQty;
    }
    else
    {
        discounts[item.Item.Id] = discount;
        discountedQuantitiesByLineId[item.Item.Id] = takeQty;
    }
    
    remaining -= takeQty;
}
```

### **Step 3: Consume Quantities Properly**

**Location:** `PromotionDiscountAllocator.cs` - Line 570-593

**Enhanced Implementation:**
```csharp
// FIXED: Track consumed quantities for multi-buy product scenarios
if (rule.RuleType == PromotionRuleType.BuyXGetY)
{
    // Use discounted quantities from reward calculation
    var rewardDiscountResult = await CalculateRewardDiscountsAsync(
        rewardItems,
        appliedPromotion.RewardQuantity,
        (DiscountType)appliedPromotion.DiscountTypeId,
        appliedPromotion.DiscountValue);

    var discountedQuantities = rewardDiscountResult.DiscountedQuantitiesByLineId;
    
    if (discountedQuantities != null && discountedQuantities.Any())
    {
        foreach (var quantityEntry in discountedQuantities)
        {
            if (quantityEntry.Value <= 0)
                continue;

            if (consumedBuyXGetYQuantitiesByLineId.TryGetValue(quantityEntry.Key, out var existingQuantity))
                consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = existingQuantity + quantityEntry.Value;
            else
                consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = quantityEntry.Value;
        }
    }
}
```

---

## 📋 **EXACT BEHAVIOR AFTER FIX:**

### **With Product B, Quantity 2, Price $100:**

**Offer 1 (Buy 2 Get 1 Free) Processing:**
```
Input: Product B, Quantity 2, Price $100
Reward Quantity: 1 unit
Calculation: 
  → Cheapest item selected: Product B
  → Take 1 unit from Product B
  → Discount: 1 × $100 = $100
Output: 
  → DiscountedQuantities[ProductB] = 1 unit
  → LineDiscount[ProductB] = $100
  → ConsumedQuantities[ProductB] = 1 unit
```

**Offer 2 (Buy 1 Get 50% Off) Processing:**
```
Input: Product B, Quantity 2 (but 1 unit already consumed)
Reward Quantity: 1 unit
Calculation:
  → Available Quantity: 2 - 1 = 1 unit remaining ✅
  → Cheapest available item: Product B (different unit)
  → Take 1 unit from remaining Product B
  → Discount: 1 × $50 = $50
Output:
  → DiscountedQuantities[ProductB] = 1 unit
  → LineDiscount[ProductB] += $50 (now $150 total)
  → ConsumedQuantities[ProductB] = 2 units (all consumed)
```

**Final Cart Display:**
```
Product B (2 units):
  → Unit 1: $100 - $100 (100% off) = $0
  → Unit 2: $100 - $50 (50% off) = $50
  → Line Total: $50 ✅
```

---

## 🔍 **KEY CHANGES NEEDED:**

### **Change 1: Return Discounted Quantities**

**Method:** `CalculateRewardDiscountsAsync`

**Return Type Enhancement:**
```csharp
// BEFORE:
public async Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts)> 
    CalculateRewardDiscountsAsync(...)

// AFTER:
public async Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts, Dictionary<int, int> DiscountedQuantities)> 
    CalculateRewardDiscountsAsync(...)
```

### **Change 2: Track Quantities Properly**

**Implementation:**
```csharp
public async Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts, Dictionary<int, int> DiscountedQuantities)> 
    CalculateRewardDiscountsAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity,
        IDictionary<int, int> availableQuantitiesByLineId,
        DiscountType discountType,
        decimal discountValue)
{
    var discounts = new Dictionary<int, decimal>();
    var discountedQuantities = new Dictionary<int, int>();
    
    // ... discount calculation logic ...
    
    // CRITICAL: Track quantities separately
    foreach (var item in itemsWithPrice.OrderBy(x => x.UnitPrice))
    {
        var takeQty = Math.Min(availableQuantity, remaining);
        
        if (takeQty > 0)
        {
            var discount = item.UnitPrice * takeQty;
            
            if (discounts.ContainsKey(item.Item.Id))
                discounts[item.Item.Id] += discount;
            else
                discounts[item.Item.Id] = discount;
                
            // Track quantities separately
            if (discountedQuantities.ContainsKey(item.Item.Id))
                discountedQuantities[item.Item.Id] += takeQty;
            else
                discountedQuantities[item.Item.Id] = takeQty;
        }
        
        remaining -= takeQty;
    }
    
    return (discounts.Sum(x => x.Value), discounts, discountedQuantities);
}
```

### **Change 3: Use Discounted Quantities for Consumption**

**Location:** `BuildDiscountMapsAsync` method

```csharp
// BEFORE:
if (rule.RuleType == PromotionRuleType.BuyXGetY &&
    appliedPromotion.DiscountedQuantitiesByLineId != null)
{
    // Use DiscountedQuantitiesByLineId
}

// AFTER:
if (rule.RuleType == PromotionRuleType.BuyXGetY)
{
    var rewardDiscountResult = await CalculateRewardDiscountsAsync(
        rewardItems,
        appliedPromotion.RewardQuantity,
        (DiscountType)appliedPromotion.DiscountTypeId,
        appliedPromotion.DiscountValue);
    
    // Use the discounted quantities from the reward calculation
    var discountedQuantities = rewardDiscountResult.DiscountedQuantities;
    
    if (discountedQuantities != null && discountedQuantities.Any())
    {
        foreach (var quantityEntry in discountedQuantities)
        {
            // Track consumed quantities
            if (consumedBuyXGetYQuantitiesByLineId.TryGetValue(quantityEntry.Key, out var existingQuantity))
                consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = existingQuantity + quantityEntry.Value;
            else
                consumedBuyXGetYQuantitiesByLineId[quantityEntry.Key] = quantityEntry.Value;
        }
    }
}
```

---

## ✅ **FINAL BEHAVIOR AFTER FIX:**

### **Product B (2 units @ $100 each):**

**Before Fix:**
```
LineDiscount[ProductB] = $150 (both merged)
→ Shows: Product B discounted $150 ❌
→ Same unit effectively gets both discounts
```

**After Fix:**
```
Offer 1 Processing:
  → Allocates 1 unit × $100 = $100 discount
  → Consumed: 1 unit
  
Offer 2 Processing:
  → Remaining: 1 unit
  → Allocates 1 unit × $50 = $50 discount  
  → Consumed: 2 units total

Final Display:
  → Unit 1: $0 (100% off)
  → Unit 2: $50 (50% off)
  → Line Total: $50 ✅
  → Different units get different discounts ✅
```

---

## 🎯 **VERIFICATION TEST:**

**Add to cart:**
- Product A ($1500) × 2
- Product B ($100) × 2  
- Product C ($245) × 1

**Expected Cart Display:**
```
Product A × 2:        $3,000.00
Product B × 2:        $50.00    ← CRITICAL TEST!
Product C × 1:        $245.00

Subtotal:              $3,445.00
Cart Savings:          $150.00   ← Exactly $150
Total:                 $3,295.00   ← Exactly $3,295

Breakdown:
✨ 2 simultaneous discounts applied!
Buy 2 Get 1 Free: $100.00 (Product B - Unit 1: FREE)
Buy 1 Get 50% Off: $50.00 (Product B - Unit 2: 50% off)
```

**Key Verification:**
- ✅ Product B shows $50 total (not $150)
- ✅ Two different units received different discounts
- ✅ Same unit didn't receive both discounts
- ✅ Cheapest items automatically selected

---

## 🚀 **IMPLEMENTATION PRIORITY:**

This is the **CRITICAL FIX** that ensures:

1. ✅ **Unit-level discount allocation** (not line-level merging)
2. ✅ **Quantity consumption tracking** (prevents double-discounting)
3. ✅ **Proper remaining quantity calculation** (for second offer)
4. ✅ **Accurate cart display** (shows correct unit breakdown)

**This fix directly addresses your core issue of both discounts applying to the same line item!**
