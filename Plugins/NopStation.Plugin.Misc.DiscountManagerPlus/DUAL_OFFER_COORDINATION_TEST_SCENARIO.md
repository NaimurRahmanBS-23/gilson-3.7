# Dual-Offer Coordination Test Scenario

## Problem Statement

**Original Bug**: Multiple promotions were applying to the same product unit instead of different units.

**Example Issue**:
- Cart contains: 2 × HTC One Mini Blue ($100 each)
- Promotion 1: "Buy X Get 100% Off" applies to HTC One Mini Blue  
- Promotion 2: "Buy 1 Get 50% Off" also applies to HTC One Mini Blue
- **Bug**: Both discounts apply to the same unit instead of different units

## Expected Behavior After Fix

1. **100% discount** applies to the cheapest eligible item (or one unit of it)
2. **50% discount** applies to another cheapest eligible item (or a different unit)  
3. The same product **unit** must not receive both discounts
4. System automatically chooses best eligible items based on price

## Test Scenarios

### Scenario 1: Multiple Units of Same Product (CRITICAL FIX)

**Setup**:
- Product: HTC One Mini Blue ($100 each)
- Cart: 2 × HTC One Mini Blue  
- Promotion A: "Buy X Get 1 Free Item" (100% discount)
- Promotion B: "Buy 1 Get 50% Off Any Item"

**Expected Behavior**:
1. Promotion A (100% discount) applies to **Unit 1** of HTC One Mini Blue → $0
2. Promotion B (50% discount) applies to **Unit 2** of HTC One Mini Blue → $50
3. Total discount: $150 ($100 + $50)
4. No unit receives both discounts

**How to Test**:
1. Add 2 × HTC One Mini Blue to cart
2. Configure both promotions to be active
3. View cart savings display
4. Verify: 
   - First promotion shows "Free: HTC One Mini Blue"
   - Second promotion shows "50% off: HTC One Mini Blue"  
   - Cart shows correct discount amounts
   - Total savings = $150

### Scenario 2: Different Products

**Setup**:
- Product A: HTC One Mini Blue ($100)
- Product B: Samsung Galaxy Mini ($80) 
- Cart: 1 × Product A, 1 × Product B
- Promotion A: "Buy X Get 1 Free Item" (100% discount)
- Promotion B: "Buy 1 Get 50% Off Any Item"

**Expected Behavior**:
1. Promotion A (100% discount) applies to **Product B** (cheapest) → $0
2. Promotion B (50% discount) applies to **Product A** (next cheapest) → $50
3. Total discount: $130 ($80 + $50)

**How to Test**:
1. Add both products to cart
2. Configure both promotions
3. Verify cheapest-item selection logic works correctly

### Scenario 3: Insufficient Units

**Setup**:
- Cart: 1 × HTC One Mini Blue ($100)
- Promotion A: "Buy X Get 1 Free Item" (100% discount)  
- Promotion B: "Buy 1 Get 50% Off Any Item"

**Expected Behavior**:
1. Promotion A (100% discount) applies to the single unit → $0
2. Promotion B (50% discount) has **no available units** → not applied
3. Total discount: $100

### Scenario 4: Multiple Quantities > Promotions

**Setup**:
- Cart: 4 × HTC One Mini Blue ($100 each = $400 total)
- Promotion A: "Buy 2 Get 1 Free" (100% discount, 1 reward)
- Promotion B: "Buy 1 Get 50% Off" (50% discount, 1 reward)

**Expected Behavior**:
1. Promotion A applies to **Unit 1** → $0 (100% off)
2. Promotion B applies to **Unit 2** → $50 (50% off)  
3. Units 3-4: No discount (exceeds reward quantities)
4. Total discount: $150
5. Total cart value: $400 - $150 = $250

## Technical Implementation Details

### Key Changes Made

**File**: `DiscountCoordinationService.cs`

**Before** (Buggy):
```csharp
// Track which cart items have already received discounts
var allocatedLineIds = new HashSet<int>();

// Filter out already-allocated items
var availableLineIds = eligibleLineIds
    .Where(lineId => !allocatedLineIds.Contains(lineId))
    .ToList();
```

**After** (Fixed):
```csharp
// Track allocated QUANTITIES per cart item, not just allocated line IDs
var allocatedQuantities = new Dictionary<int, int>();

// Filter out items where ALL units are already allocated
var availableLineIds = eligibleLineIds
    .Where(lineId => {
        var cartItem = cart.FirstOrDefault(item => item.Id == lineId);
        if (cartItem == null) return false;

        var alreadyAllocated = allocatedQuantities.TryGetValue(lineId, out var allocated) ? allocated : 0;
        return cartItem.Quantity > alreadyAllocated; // Available units remain
    })
    .ToList();
```

**Allocation Logic**:
```csharp
// Track allocated quantity for this cart item
if (!allocatedQuantities.ContainsKey(item.Id))
    allocatedQuantities[item.Id] = 0;
allocatedQuantities[item.Id]++;
```

## Verification Steps

### 1. Code Review Checklist
- ✅ Changed from `HashSet<int> allocatedLineIds` to `Dictionary<int, int> allocatedQuantities`
- ✅ Updated availability check to compare allocated vs total quantity  
- ✅ Modified allocation logic to increment quantity counter
- ✅ Updated `GetLowestPricedItemsExcludingAllocatedAsync` to accept quantity dictionary
- ✅ Interface updated to match new signature

### 2. Integration Testing
1. Stop the running web application
2. Clean solution in Visual Studio
3. Rebuild the entire solution  
4. Clear browser cache
5. Test each scenario above
6. Check log files for "DUAL_OFFER_DEBUG" messages

### 3. Log Monitoring

Watch for these debug messages in logs:
```
DUAL_OFFER_DEBUG: Found X active rules
DUAL_OFFER_DEBUG: Multiple BuyXGetY detected: true
DUAL_OFFER_DEBUG: ACTIVATING COORDINATION SERVICE
```

## Debugging Commands

### Check Applied Promotions
```sql
-- Check which promotions were applied
SELECT * FROM AppliedPromotion 
WHERE RuleTypeId = (int)PromotionRuleType.BuyXGetY
ORDER BY DiscountAmount DESC
```

### Check Discount Allocations  
```sql
-- Verify line item discounts
SELECT LineId, SUM(DiscountAmount) as TotalDiscount
FROM LineDiscountMap
GROUP BY LineId
```

## Expected Cart Savings Display

When both promotions apply correctly, the cart savings should show:

```
🎁 Your Savings: $150.00

✅ Buy X Get 100% Off (#1039)
   Discount: $100.00
   Applied to: HTC One Mini Blue (Free item)

✅ Buy 1 Get 50% Off Any Item  
   Discount: $50.00
   Applied to: HTC One Mini Blue (50% off)

Multiple discounts applied: System automatically selected the best
items for each promotion to maximize your savings.
```

## Performance Impact

The new coordination logic has minimal performance impact:
- **Time Complexity**: O(n) where n = number of promotions (typically 2-5)
- **Space Complexity**: O(m) where m = number of cart items (typically 5-20)
- **Database Calls**: No additional database queries required
- **Memory**: Small dictionary tracking quantities per cart item

## Common Issues & Solutions

### Issue 1: Both promotions apply to same unit
**Cause**: Old code still deployed or cached
**Solution**: Clear application cache, restart web server

### Issue 2: Second promotion not applying  
**Cause**: Insufficient units available
**Solution**: Add more units to cart or verify promotion eligibility

### Issue 3: Wrong discount amounts
**Cause**: Promotion configuration error
**Solution**: Verify RewardQuantity and DiscountValue settings in admin

## Success Criteria

✅ **Critical Fix**: Scenario 1 works correctly (multiple units of same product)
✅ No single product unit receives multiple discounts
✅ Cheapest-item selection works across all scenarios  
✅ Cart savings display shows correct breakdown
✅ Total discount amount matches expected calculation
✅ No performance degradation
✅ No database errors or exceptions in logs

## Maintenance Notes

- Always test dual-offer scenarios when modifying coordination logic
- Monitor logs for "DUAL_OFFER_DEBUG" messages during testing
- Keep this document updated with new test scenarios
- Review discount coordination logic quarterly for optimization opportunities
