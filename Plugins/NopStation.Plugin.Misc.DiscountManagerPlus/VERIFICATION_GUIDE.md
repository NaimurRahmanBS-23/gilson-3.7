# Buy 5 Get 1 + Buy 5 Get 1 with 50% Off - Verification Guide

## 🎯 Scenario Configuration Summary

### Offer 1: Buy 5 Get 1 Free (100% discount)
```
Name: "Buy 5 Get 1 Free"
Rule Type: Buy X Get Y
Discount Type: Free Item
Buy Quantity: 5
Reward Quantity: 1
Reward Discount Type: Free Item
Discount Target Type: Cheapest In Cart
Priority: 10 (higher)
Stop Further Rules: true
```

### Offer 2: Buy 5 Get 1 with 50% Off
```
Name: "Buy 5 Get 1 with 50% Off"
Rule Type: Buy X Get Y
Discount Type: Percentage
Discount Value: 50
Buy Quantity: 5
Reward Quantity: 1
Reward Discount Type: Percentage
Reward Discount Value: 50
Discount Target Type: Cheapest In Cart
Priority: 5 (lower)
Stop Further Rules: false
```

## ✅ Expected Behavior

### Test Case: Cart with 6 Products (Different Prices)
```
Shopping Cart:
- Product A: $10.00 (cheapest)
- Product B: $15.00
- Product C: $20.00
- Product D: $25.00
- Product E: $30.00
- Product F: $35.00

Expected Results:
✅ Product A ($10.00) → FREE (100% discount via Buy 5 Get 1 Free)
✅ Product B ($15.00) → $7.50 savings (50% discount via Buy 5 Get 1 with 50% Off)
✅ Products C-F → Normal price ($20.00, $25.00, $30.00, $35.00)

Total Cart Value: $135.00
Total Savings: $17.50 ($10.00 + $7.50)
Final Total: $117.50
```

## 🔍 Verification Steps

### 1. Admin Panel Setup
- [ ] Navigate to Promotions > Promotion Rules
- [ ] Create "Buy 5 Get 1 Free" with settings above
- [ ] Create "Buy 5 Get 1 with 50% Off" with settings above
- [ ] Ensure both are Active
- [ ] Add same products to both rules (or set "All Products")

### 2. Frontend Testing
- [ ] Add 6 products to cart with different prices
- [ ] Proceed to cart page
- [ ] Check for discount messages
- [ ] Verify cheapest item shows as "FREE"
- [ ] Verify next cheapest shows "50% OFF"
- [ ] Check total savings amount

### 3. Backend Verification
- [ ] Check discount calculation logs
- [ ] Verify `DiscountCoordinationService` is called
- [ ] Confirm proper allocation of items to discounts
- [ ] Ensure no double-discounting occurs

## 📊 Expected Cart Display

### Cart Savings Component Should Show:
```
🎉 You're receiving multiple discounts!

✅ Buy 5 Get 1 Free
   Applied to: Product A ($10.00)
   Savings: $10.00 (FREE)

✅ Buy 5 Get 1 with 50% Off
   Applied to: Product B ($15.00)
   Savings: $7.50 (50% OFF)

Total Savings: $17.50
```

## 🚨 Troubleshooting

### Issue 1: Only One Discount Applied
**Solution**: Check that both rules are Active and have proper priority values

### Issue 2: Same Product Gets Both Discounts
**Solution**: Verify `DiscountCoordinationService` is registered in PluginNopStartup.cs

### Issue 3: Cheapest Item Not Selected
**Solution**: Ensure both rules have `Discount Target Type: Cheapest In Cart`

### Issue 4: No Discounts Applied
**Solution**: Verify both rules have the same products and cart meets minimum quantity (5 items)

## 🎯 Success Criteria

✅ Both offers activate simultaneously
✅ Cheapest item gets 100% discount (FREE)
✅ Next cheapest item gets 50% discount
✅ Same product never receives both discounts
✅ Cart displays clear breakdown of savings
✅ Total savings calculation is correct

## 📝 Test Results Template

```
Date: _______________
Tester: ______________

Rule Configuration:
- Buy 5 Get 1 Free created: ☐ Yes ☐ No
- Buy 5 Get 1 with 50% Off created: ☐ Yes ☐ No
- Both rules active: ☐ Yes ☐ No

Cart Test:
- Added 6 products to cart: ☐ Yes ☐ No
- Cart shows discount messages: ☐ Yes ☐ No

Verification:
- Cheapest item is FREE: ☐ Yes ☐ No
- Next cheapest has 50% off: ☐ Yes ☐ No
- No double-discounting: ☐ Yes ☐ No
- Total savings correct: ☐ Yes ☐ No

Issues Found: ___________________________________

Overall Status: ☐ PASS ☐ FAIL
```