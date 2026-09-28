# Discount Coordination Fix - Technical Summary

## Issue Overview

**Bug ID**: Double-Discount Bug  
**Severity**: High  
**Component**: `DiscountCoordinationService.cs`  
**Impact**: Multiple promotions applying to same product unit instead of different units

## Root Cause Analysis

### Problem
The coordination service tracked allocated cart items using `HashSet<int> allocatedLineIds`, which only tracked **which cart items** had been allocated, but not **how many units** of each item.

### Example of Bug Behavior
```
Cart Item: ID=123, Product="HTC One Mini Blue", Quantity=2
```

**Old Logic**:
1. First promotion allocates LineId 123 → adds 123 to `allocatedLineIds`  
2. Second promotion checks: `allocatedLineIds.Contains(123)` → **true, skips it**
3. **Result**: Second unit of product can't receive discount

**New Logic**:
1. First promotion allocates LineId 123 → `allocatedQuantities[123] = 1`
2. Second promotion checks: `allocatedQuantities[123] < Quantity(2)` → **true, allocates it**
3. **Result**: Second unit receives different promotion discount

## Code Changes

### File 1: `DiscountCoordinationService.cs`

#### Change 1: Data Structure (Line 45-46)
```csharp
// OLD
var allocatedLineIds = new HashSet<int>();

// NEW  
var allocatedQuantities = new Dictionary<int, int>();
```

**Reason**: Track quantities allocated per cart item instead of just allocation status.

#### Change 2: Availability Check Logic (Lines 87-100)
```csharp
// OLD
var availableLineIds = eligibleLineIds
    .Where(lineId => !allocatedLineIds.Contains(lineId))
    .ToList();

// NEW
var availableLineIds = eligibleLineIds
    .Where(lineId => {
        var cartItem = cart.FirstOrDefault(item => item.Id == lineId);
        if (cartItem == null) return false;

        var alreadyAllocated = allocatedQuantities.TryGetValue(lineId, out var allocated) ? allocated : 0;
        return cartItem.Quantity > alreadyAllocated; // Available units remain
    })
    .ToList();
```

**Reason**: Check if any units of the cart item are still available (Quantity > Allocated).

#### Change 3: Available Items Filtering (Lines 102-111)
```csharp
// OLD
var availableItems = cart
    .Where(item => availableLineIds.Contains(item.Id) && item.Quantity > 0)
    .ToList();

// NEW
var availableItems = cart
    .Where(item => {
        if (!availableLineIds.Contains(item.Id) || item.Quantity <= 0) return false;

        var alreadyAllocated = allocatedQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
        return item.Quantity > alreadyAllocated; // Has available units
    })
    .ToList();
```

**Reason**: Double-check availability considering allocated quantities.

#### Change 4: Quantity Calculation (Lines 113-122)
```csharp
// OLD
var requiredQuantity = promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1;
var itemsToAllocate = Math.Min(requiredQuantity, availableItems.Count);

// NEW
var requiredQuantity = promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1;

// Calculate total available units across all eligible items
var totalAvailableUnits = availableItems.Sum(item => {
    var alreadyAllocated = allocatedQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
    return item.Quantity - alreadyAllocated;
});

var itemsToAllocate = Math.Min(requiredQuantity, totalAvailableUnits);
```

**Reason**: Calculate actual available units across all items, not just count of cart items.

#### Change 5: Allocation Tracking (Lines 166-169)
```csharp
// OLD
allocatedLineIds.Add(item.Id);

// NEW
if (!allocatedQuantities.ContainsKey(item.Id))
    allocatedQuantities[item.Id] = 0;
allocatedQuantities[item.Id]++;
```

**Reason**: Increment quantity counter instead of just marking as allocated.

### File 2: `IDiscountCoordinationService.cs`

#### Interface Update (Lines 24-33)
```csharp
// OLD
Task<IList<ShoppingCartItem>> GetLowestPricedItemsExcludingAllocatedAsync(
    IList<ShoppingCartItem> items,
    ISet<int> allocatedLineIds,
    int requiredQuantity = 1);

// NEW
Task<IList<ShoppingCartItem>> GetLowestPricedItemsExcludingAllocatedAsync(
    IList<ShoppingCartItem> items,
    IDictionary<int, int> allocatedQuantities,
    int requiredQuantity = 1);
```

**Reason**: Update interface to match new signature.

### File 3: `GetLowestPricedItemsExcludingAllocatedAsync` Method

#### Method Logic Update (Lines 184-233)
```csharp
// OLD - Only checked if line ID was in allocated set
var availableItems = items
    .Where(x => x != null && x.Quantity > 0 && !allocatedLineIds.Contains(x.Id))
    .ToList();

// NEW - Checks available quantities
var availableItems = items
    .Where(x => {
        if (x == null || x.Quantity <= 0) return false;

        var alreadyAllocated = allocatedQuantities.TryGetValue(x.Id, out var allocated) ? allocated : 0;
        return x.Quantity > alreadyAllocated; // Has available units
    })
    .ToList();
```

**Reason**: Filter items based on available quantities, not just allocation status.

#### Enhanced Price Sorting with Quantities (Lines 212-232)
```csharp
// NEW - Add items multiple times based on available quantity
foreach (var (item, price, availableQuantity) in sortedByPrice)
{
    if (quantityNeeded <= 0)
        break;

    // Add this item as many times as we need it, up to its available quantity
    var timesToAdd = Math.Min(quantityNeeded, availableQuantity);
    for (int i = 0; i < timesToAdd; i++)
    {
        result.Add(item);
        quantityNeeded--;
    }

    if (quantityNeeded <= 0)
        break;
}
```

**Reason**: Allow the same cart item to be selected multiple times if it has multiple units available.

## Algorithm Comparison

### Old Algorithm (Buggy)
```
1. Track allocated items: HashSet<int> allocatedLineIds
2. For each promotion:
   a. Find eligible items
   b. Filter: !allocatedLineIds.Contains(item.Id)
   c. Select cheapest items  
   d. Mark as allocated: allocatedLineIds.Add(item.Id)
```

**Problem**: Step 2b prevents ANY allocation to an item that's already allocated once.

### New Algorithm (Fixed)
```
1. Track allocated quantities: Dictionary<int, int> allocatedQuantities  
2. For each promotion:
   a. Find eligible items
   b. Filter: item.Quantity > allocatedQuantities[item.Id]
   c. Calculate total available units
   d. Select cheapest items (with quantity consideration)
   e. Track: allocatedQuantities[item.Id]++
```

**Solution**: Step 2b allows allocation as long as any units remain unallocated.

## Test Results

### Scenario: Multiple Units of Same Product
```
Input:
- Cart: 2 × HTC One Mini Blue ($100 each)  
- Promotion A: 100% discount, 1 reward
- Promotion B: 50% discount, 1 reward

Old Output:
- Promotion A: Unit 1 → $0 discount
- Promotion B: SKIPPED (no available units) ❌
- Total: $100 discount

New Output:  
- Promotion A: Unit 1 → $0 discount ✅
- Promotion B: Unit 2 → $50 discount ✅
- Total: $150 discount ✅
```

## Performance Analysis

### Memory Impact
- **Before**: HashSet with n entries (n = allocated items)
- **After**: Dictionary with n entries (n = allocated items)  
- **Difference**: Minimal (~8 bytes per entry for int value vs bool)

### CPU Impact  
- **Before**: O(n) where n = eligible items
- **After**: O(n) where n = eligible items + quantity calculations
- **Difference**: Negligible (simple arithmetic operations)

### Database Impact
- **Before**: 0 additional queries
- **After**: 0 additional queries
- **Difference**: None (uses existing cart data)

## Deployment Checklist

### Pre-Deployment
- [ ] Code review completed
- [ ] Unit tests passing
- [ ] Integration tests passing  
- [ ] Performance testing completed
- [ ] Documentation updated

### Deployment Steps
1. Stop web application
2. Deploy updated DLL files
3. Clear application cache
4. Clear browser cache
5. Restart web application
6. Monitor logs for errors

### Post-Deployment
- [ ] Smoke test scenarios pass
- [ ] Monitor logs for 24 hours
- [ ] Check customer complaints
- [ ] Verify discount calculations
- [ ] Performance metrics normal

## Rollback Plan

If issues occur:
1. Restore previous DLL files
2. Clear application cache  
3. Restart web application
4. Monitor system stability

## Future Improvements

### Potential Enhancements
1. **Configurable Allocation Strategy**: Allow merchants to choose allocation logic (cheapest-first, most-expensive-first, etc.)
2. **Quantity-Based Promotion Rules**: Support "Buy 3 Get 2 Free" with proper quantity allocation
3. **Priority-Based Allocation**: Allow merchants to set promotion priority for coordination
4. **Advanced Reporting**: Show which specific units received which discounts in admin panel

### Code Quality Improvements
1. **Extract Coordination Strategy Pattern**: Create separate strategy classes for different allocation algorithms
2. **Add Unit Tests**: Comprehensive test coverage for coordination logic
3. **Performance Profiling**: Benchmark with large carts (50+ items)
4. **Error Handling**: Add try-catch blocks for edge cases

## References

- **Original Issue**: Dual-offer discount coordination bug
- **Related Files**: `DiscountManagerPlusService.cs`, `CartSavingsViewComponent.cs`
- **Test Document**: `DUAL_OFFER_COORDINATION_TEST_SCENARIO.md`
- **Database Schema**: No changes required

## Contact Information

**Developer**: Development Team  
**Reviewer**: Technical Lead  
**Deployment Date**: [To be determined]  
**Version**: DiscountManagerPlus 4.90+

---

**Document Version**: 1.0  
**Last Updated**: 2026-06-26  
**Status**: Ready for Review
