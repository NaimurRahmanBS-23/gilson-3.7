# Quick Setup Guide: Multiple Offers Configuration

## 🎯 **Scenario 2 Setup: Two Simultaneous Offers**

This guide shows you exactly how to configure the plugin to support multiple offers with cheapest-item discount logic.

---

## 📋 **Prerequisites**

- ✅ DiscountManagerPlus plugin installed and activated
- ✅ Products created in your catalog
- ✅ Admin access to Promotion Rules

---

## 🚀 **Step-by-Step Setup**

### **Step 1: Create Offer 1 (Buy X Get 50% Off)**

1. Navigate to **Promotion Rules** → **Add New**
2. **Basic Settings:**
   ```
   Name: "Buy 1 Get 1 Half Price"
   System Name: "buy-1-get-1-half-price"
   Rule Type: Buy X Get Y
   Discount Type: Percentage
   Discount Value: 50%
   Discount Scope: Per Product
   Priority: 2
   Is Active: Yes
   Is Exclusive: No
   Stop Further Rules: No
   ```

3. **Products Tab:**
   - Click "Add New" in "Buy Products" section
   - Select your products (or leave blank for all products)
   - Set Min Quantity: 1
   - Set Max Quantity: 0 (for unlimited)

4. **Tiers Tab:**
   - Click "Add New Tier"
   - Set Buy Quantity: 1
   - Set Reward Quantity: 1
   - Click "Save"

5. Click **Save** button

---

### **Step 2: Create Offer 2 (Buy 2 Get 1 Free)**

1. Navigate to **Promotion Rules** → **Add New**
2. **Basic Settings:**
   ```
   Name: "Buy 2 Get 1 Free"
   System Name: "buy-2-get-1-free"
   Rule Type: Buy X Get Y
   Discount Type: Free Item
   Discount Value: 0%
   Discount Scope: Per Product
   Priority: 1 (Higher priority = processed first)
   Is Active: Yes
   Is Exclusive: No
   Stop Further Rules: No
   ```

3. **Products Tab:**
   - Click "Add New" in "Buy Products" section
   - Select your products (or leave blank for all products)
   - Set Min Quantity: 2
   - Set Max Quantity: 0 (for unlimited)

4. **Tiers Tab:**
   - Click "Add New Tier"
   - Set Buy Quantity: 2
   - Set Reward Quantity: 1
   - Click "Save"

5. Click **Save** button

---

## 🧪 **Test the Setup**

### **Test Cart:**

Add 5 products to cart with varying prices:

| Product | Price | Quantity |
|---------|-------|----------|
| Economy Widget | $10 | 2 |
| Standard Gadget | $20 | 2 |
| Premium Tool | $30 | 1 |

**Total: 5 items, $90 value**

### **Expected Result:**

1. **Offer 2 (Priority 1)** processes first:
   - Gives 1 FREE item
   - Selects cheapest: Economy Widget ($10)
   - **Savings: $10**

2. **Offer 1 (Priority 2)** processes second:
   - Gives 1 item at 50% off
   - Selects next cheapest: Standard Gadget ($20)
   - **Savings: $10**

**Final Cart:**
- Economy Widget: 1 FREE + 1 paid ($10)
- Standard Gadget: 1 at 50% off ($10) + 1 paid ($20)
- Premium Tool: 1 paid ($30)
- **Total: $70** (Total savings: $20)

---

## 🔧 **Priority Configuration**

### **Why Priority Matters:**

Rules process in priority order (lower number = higher priority):

```
Priority 1: Buy 2 Get 1 Free (100% off) - Best deal
Priority 2: Buy 1 Get 1 Half Price (50% off) - Good deal
Priority 3: Buy 3 Get 20% Off (20% off) - Lower value
```

**Strategy:** Always prioritize higher discount percentages!

---

## ⚙️ **Advanced Configuration**

### **Option 1: Same Products, Different Tiers**

You can put both offers in the same rule with different tiers:

**Single Rule Setup:**
```
Name: "Multi-Tier Discount"
Rule Type: Buy X Get Y
Discount Type: Percentage

Tier 1:
- Buy: 2, Get: 1 Free (100% off)

Tier 2:
- Buy: 1, Get: 1 Half Price (50% off)
```

### **Option 2: Product-Specific Rules**

Create separate rules for different product categories:

```
Rule 1: Electronics Bundle
- Products: All electronics
- Buy 2 Get 1 Free

Rule 2: Clothing Deal
- Products: All clothing
- Buy 1 Get 1 Half Price
```

### **Option 3: Minimum Purchase Requirements**

Add conditions to restrict when offers apply:

```
Conditions:
- Cart subtotal >= $50
- Customer group = VIP
- Store = Main Store
```

---

## 🎨 **Customer Experience**

### **Attention Messages:**

The plugin automatically shows helpful messages:

**Before Triggering:**
> "Add 1 more item to Buy 2 Get 1 Free!"

**After Triggering:**
> "🎉 You've unlocked Buy 2 Get 1 Free!"

**Cart Display:**
- Shows which items are discounted
- Displays savings amount
- Lists applied offers

---

## 📊 **Reporting & Analytics**

### **Track Performance:**

Navigate to **Promotion Rules** → **Analytics**

**Metrics Available:**
- Total usage count
- Total discount amount
- Average discount per order
- Conversion rate
- Revenue impact

**Popular Reports:**
- Most redeemed offers
- Best performing products
- Customer savings trends
- ROI by offer type

---

## ⚠️ **Common Issues & Solutions**

### **Issue 1: Discounts Not Applying**

**Possible Causes:**
- Rule not active
- Wrong priority
- Products not in eligible list
- Conditions not met

**Solution:**
1. Check rule is active
2. Verify products are in "Buy Products" list
3. Test with products from eligible list
4. Check conditions are met

### **Issue 2: Same Item Gets Multiple Discounts**

**Possible Causes:**
- `StopFurtherRulesForMatchedLines` = No
- Priority not set correctly

**Solution:**
1. Set `StopFurtherRulesForMatchedLines` = Yes
2. Adjust priority order
3. Enable quantity tracking

### **Issue 3: Wrong Items Discounted**

**Possible Causes:**
- Old discount allocation logic
- Cache issues

**Solution:**
1. Clear cache
2. Update plugin to latest version
3. Verify "Cheapest First" allocation is enabled

---

## 🎯 **Best Practices**

### **1. Strategic Priority Setting**
```
Priority 1: Best offers (100% off)
Priority 2: Good offers (50% off)
Priority 3: Average offers (20% off)
```

### **2. Clear Rule Names**
```
✅ Good: "Buy 2 Get 1 Free - Electronics"
❌ Bad: "Rule 1"
```

### **3. Test Thoroughly**
```
✅ Test with:
- Single product types
- Mixed product types
- Different quantities
- Edge cases (0, 1, max items)
```

### **4. Monitor Performance**
```
✅ Check weekly:
- Which offers are most popular
- Total discount amount
- Customer satisfaction
- Revenue impact
```

---

## 🚀 **Ready to Use!**

Your multiple offer setup is now complete! The plugin will automatically:

✅ Process offers in priority order
✅ Select cheapest items for discounts
✅ Track quantities to prevent conflicts
✅ Show helpful attention messages
✅ Calculate accurate discounts
✅ Provide detailed analytics

**Test it now by adding products to your cart!** 🎉

---

## 📞 **Need Help?**

For additional support:
- Check the documentation: `MULTIPLE_OFFER_SCENARIOS.md`
- Review the implementation: `PromotionDiscountAllocator.cs`
- Contact NopStation support

**Happy Selling!** 💰
