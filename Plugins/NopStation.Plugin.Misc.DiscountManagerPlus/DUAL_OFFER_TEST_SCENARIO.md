# Dual-Offer Discount Coordination Test Scenario

## Scenario 2: Apply Two Offers at the Same Time with Cheapest Item Discount Logic

### Test Setup

**Customer Cart Contents:**
- 5 eligible products with different prices:
  - Product A: $10.00 (Cheapest)
  - Product B: $20.00 
  - Product C: $30.00
  - Product D: $40.00
  - Product E: $50.00 (Most expensive)

**Active Discount Rules:**
1. **Offer 1:** Buy 1 get 1 item with 50% discount (BuyXGetY rule)
2. **Offer 2:** Buy 2 get 1 item free 100% discount (BuyXGetY rule)

### Expected Behavior

#### Discount Application:
1. **100% discount** applied to Product A ($10.00) - cheapest item
2. **50% discount** applied to Product B ($20.00) - next cheapest item  
3. Products C, D, E charged at normal price

#### Calculations:
- Product A: $10.00 × 0% = $0.00 (FREE)
- Product B: $20.00 × 50% = $10.00 (HALF PRICE)
- Product C: $30.00 × 100% = $30.00 (NORMAL PRICE)
- Product D: $40.00 × 100% = $40.00 (NORMAL PRICE)
- Product E: $50.00 × 100% = $50.00 (NORMAL PRICE)

**Total Discount: $20.00 ($10.00 free + $10.00 half-price)**  
**Total Cart Value: $130.00 (before discounts) → $110.00 (after discounts)**

### Attention Messages Display

The cart should display the following attention messages to the user:

1. **Multiple Discount Notice:**
   ```
   ✨ 2 offers applied to your cart!
   ```

2. **Savings Breakdown:**
   ```
   🎁 Buy 2 Get 1 Free: Applied to Product A - Save $10.00
   💰 Buy 1 Get 50% Off: Applied to Product B - Save $10.00
   ```

3. **Coordinated Discount Notice:**
   ```
   ℹ️ We've applied the best available discounts to your cart. 
   Free items and percentage discounts are automatically applied to the cheapest eligible products.
   ```

### Key Validation Points

#### ✅ DiscountCoordinationService:
1. [ ] Detects 2+ BuyXGetY promotions correctly
2. [ ] Sorts promotions by discount value (100% before 50%)
3. [ ] Allocates correct quantity per promotion (1 item each)
4. [ ] Prevents double-discounting of same product unit
5. [ ] Selects cheapest items first

#### ✅ Line Discount Maps:
1. [ ] LineDiscountMap contains correct discounts per line item
2. [ ] Product A line: $10.00 discount (100%)
3. [ ] Product B line: $10.00 discount (50% of $20.00)
4. [ ] Products C, D, E lines: $0.00 discount

#### ✅ Attention Messages:
1. [ ] MultipleDiscountNotices contains coordination message
2. [ ] CheapestItemSelectionDetails shows which products got discounts
3. [ ] Messages clearly indicate no double-discounting occurred
4. [ ] Users understand which items received which discounts

#### ✅ Cart Display:
1. [ ] CartSavingsViewComponent displays both offers
2. [ ] Each offer shows correct target product
3. [ ] Discount amounts are accurate
4. [ ] Total savings calculation is correct

### Test Execution Steps

1. **Create Test Data:**
   - Add 5 products with prices $10, $20, $30, $40, $50
   - Create "Buy 2 Get 1 Free" rule (100% discount)
   - Create "Buy 1 Get 50% Off" rule (50% discount)
   - Add all 5 products to cart

2. **Execute Discount Evaluation:**
   - Call `EvaluateCartAsync()` with cart items
   - Verify coordination service is activated
   - Check discount allocation logic

3. **Verify Line Discount Maps:**
   - Call `BuildLineDiscountMapAsync()` 
   - Verify correct line-by-line discounts
   - Ensure no double-discounting

4. **Check Attention Messages:**
   - Call `BuildMultipleDiscountNoticesAsync()`
   - Call `BuildCheapestItemSelectionDetailsAsync()`
   - Verify message content and clarity

5. **Display Verification:**
   - Render CartSavingsViewComponent
   - Check UI shows both offers correctly
   - Verify user experience is clear

### Common Issues to Check

#### 🔍 Issue: Same Product Receives Both Discounts
**Symptom:** Product A gets both 100% and 50% discount
**Fix:** Verify `allocatedLineIds` HashSet in coordination service prevents reallocation

#### 🔍 Issue: Wrong Products Discounted
**Symptom:** More expensive products get discounts instead of cheapest
**Fix:** Verify `GetLowestPricedItemsExcludingAllocatedAsync()` sorts by unit price ascending

#### 🔍 Issue: Too Many Items Discounted
**Symptom:** 3 items discounted instead of 2 (1 per promotion)
**Fix:** Verify `requiredQuantity` and `itemsToAllocate` calculations respect RewardQuantity

#### 🔍 Issue: Attention Messages Confusing
**Symptom:** Users don't understand which products got which discounts
**Fix:** Verify `BuildCheapestItemSelectionDetailsAsync()` generates clear, specific messages

### Success Criteria

✅ **All validation points pass**  
✅ **No double-discounting occurs**  
✅ **Cheapest items receive discounts first**  
✅ **Attention messages are clear and helpful**  
✅ **Total discount calculation is accurate**  
✅ **User experience is intuitive**

---

## Additional Test Scenarios

### Scenario 2a: Same Price Products
- 5 products all priced at $20.00
- Expected: Any 2 different products get discounts (selection doesn't matter since same price)

### Scenario 2b: Minimum Quantity Not Met
- Only 2 products in cart (need 3 for both offers)
- Expected: Only applicable offer applies, user sees message about adding more items

### Scenario 2c: Excluded Products
- 5 products, but 2 are excluded from one offer
- Expected: Coordination respects exclusion rules, discount allocation adapts

---

*This test scenario should be executed after each code change to ensure dual-offer coordination works correctly.*