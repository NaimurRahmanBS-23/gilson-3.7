# 🖱️ **Click-by-Click Setup Guide - Dual Offer Scenario**

## 🎯 **Scenario: Apply Two Offers Simultaneously with Cheapest Item Logic**

**Objective**: Configure two offers that work together:
- **Offer 1**: Buy 1 Get 50% Off
- **Offer 2**: Buy 2 Get 1 Free

---

## 📋 **Pre-Setup Checklist**

Before starting, ensure you have:
- [ ] Access to nopCommerce Admin panel
- [ ] DiscountManagerPlus plugin installed and active
- [ ] At least 3-5 test products created with different prices
- [ ] Products should have prices like: $50, $100, $245, $1500 (for testing)

---

## 🚀 **Step 1: Create Offer 2 (Buy 2 Get 1 Free)**

### **Why start with Offer 2?**
- It has **Priority 1** (higher priority) → processes first
- It gives the **best discount** (100% free) → should be processed first
- This ensures cheapest items get the best offer

### **Step 1.1: Navigate to Promotion Rules**

1. Login to nopCommerce Admin panel
2. From the left menu, click: **NopStation** → **Promotion Rules**
3. You should see the Promotion Rules list page

### **Step 1.2: Add New Promotion Rule**

1. Click the **"Add New"** button (top-right corner)
2. A new promotion rule form will open

---

## ⚠️ **CRITICAL: Buy X Get Y Hidden Fields Warning**

**⚠️ IMPORTANT**: When you select **"Buy X Get Y"** as Rule Type, **some fields will be HIDDEN**!

### **Visible Fields for Buy X Get Y:**
- ✅ **Name** (always visible)
- ✅ **Rule Type** (dropdown - this is what you select)
- ✅ **Active** (checkbox)
- ✅ **Priority** (number input)
- ✅ **Discount Type** (dropdown)
- ✅ **Discount Value** (number input)
- ✅ **Discount Scope** (dropdown)
- ✅ **Limited to Customer Roles** (multi-select)
- ✅ **Stores** (multi-select)
- ✅ **Coupon Code** (text input)

### **HIDDEN Fields for Buy X Get Y:**
- ❌ **Discount Limitation** (hidden - not used for Buy X Get Y)
- ❌ **Maximum Quantity** (hidden - use Tiers instead)
- ❌ **Apply To Customer Role** (hidden - use Limited to Customer Roles above)
- ❌ **Cumulative With Default Discount** (hidden - not applicable)
- ❌ **Is Cumulative** (hidden - not applicable)

---

## 📝 **Step 1.3: Configure Offer 2 - Basic Info**

### **Info Tab Fields:**

1. **Name**: Enter `Buy 2 Get 1 Free - Best Offer`
   - This name will appear in cart savings breakdown

2. **Rule Type**: Select `Buy X Get Y` ⚠️
   - ⚠️ **WARNING**: Selecting this will **HIDE** some fields!
   - After selecting, the page will refresh and some fields will disappear

3. **Active**: ✅ Check this box
   - The rule must be active to work

4. **Priority**: Enter `1` 
   - ⚠️ **IMPORTANT**: This is the **HIGHEST priority**
   - Lower number = higher priority
   - This offer should process FIRST

5. **Discount Type**: Select `Free Item`
   - This gives 100% discount on the reward item

6. **Discount Value**: Enter `100`
   - For Free Item, this represents 100% discount

7. **Discount Scope**: Select `Per Product`
   - Apply discount to individual products

8. **Is Exclusive**: Leave **UNCHECKED** ☐
   - We want both offers to work together

9. **Stop Further Rules**: Leave **UNCHECKED** ☐
   - We want the second offer to also apply

10. **Enable Auto Upgrade**: Leave **UNCHECKED** ☐
    - Not needed for this scenario

11. **Customer Role Limitations**: Leave empty for all customers
    - Or select specific roles if needed

12. **Stores**: Select your store(s)
    - Usually just select your main store

13. **Coupon Code**: Leave empty
    - Not using coupon for this example

14. **Date Range**: Set your promotional period
    - **Available From Start Date**: Select start date
    - **Available From End Date**: Select end date (or leave empty for no limit)

---

## 📦 **Step 1.4: Configure Products Tab**

1. Click the **"Products"** tab (top navigation)
2. You will see two sections: **"Buy Products"** and **"Reward Products"**

### **Buy Products Section:**

3. Click **"Add Buy Product"** button
4. A popup will appear with product search

5. **Select Products**:
   - Search and select: **Product A** ($1500)
   - Search and select: **Product B** ($100)  
   - Search and select: **Product C** ($245)
   - Click **"Add"** for each product

6. **Configure Quantities** (in the main Products tab after adding):
   - For **Product A**: Set **Min Quantity** = `2`, Max = `0`
   - For **Product B**: Set **Min Quantity** = `2`, Max = `0`
   - For **Product C**: Set **Min Quantity** = `2`, Max = `0`
   - ⚠️ **NOTE**: Max Quantity = `0` means unlimited

### **Reward Products Section:**

7. Click **"Add Reward Product"** button
8. Select the same products: Product A, Product B, Product C
9. For Buy 2 Get 1 Free, the reward can be any of the buy products
10. Click **"Add"** for each product

---

## 🎯 **Step 1.5: Configure Tiers Tab**

1. Click the **"Tiers"** tab (top navigation)
2. You will see a list of tiers (empty for new rule)

3. Click **"Add New Tier"** button
4. A popup will appear

### **Tier Configuration:**

5. **Name**: Enter `Tier 1 - Buy 2 Get 1 Free`
6. **Buy Quantity**: Enter `2`
   - Customer needs to buy 2 items
7. **Reward Quantity**: Enter `1` 
   - Customer gets 1 item free
8. **Discount Type**: Select `Free Item`
9. **Discount Value**: Enter `100` (100% off)
10. **Auto Add Reward**: Leave **UNCHECKED** ☐
    - Let customer choose which product to get free
11. Click **"Save"**

---

## ✅ **Step 1.6: Save Offer 2**

1. Click the **"Save"** button (top-right)
2. You should see a success message: "The new promotion rule has been added successfully."
3. Click **"Continue"** to return to the list

---

## 🔄 **Step 2: Create Offer 1 (Buy 1 Get 50% Off)**

### **Step 2.1: Add New Promotion Rule**

1. From Promotion Rules list, click **"Add New"** again
2. A new promotion rule form will open

---

## 📝 **Step 2.2: Configure Offer 1 - Basic Info**

### **Info Tab Fields:**

1. **Name**: Enter `Buy 1 Get 50% Off Any Item`
   - This name will appear in cart savings breakdown

2. **Rule Type**: Select `Buy X Get Y` ⚠️
   - ⚠️ **WARNING**: Same hidden fields as before!

3. **Active**: ✅ Check this box

4. **Priority**: Enter `2` ⚠️
   - ⚠️ **IMPORTANT**: This is the **SECOND priority**
   - Higher number than Offer 2
   - This offer processes SECOND

5. **Discount Type**: Select `Percentage`
   - This gives percentage discount

6. **Discount Value**: Enter `50`
   - 50% discount on reward item

7. **Discount Scope**: Select `Per Product`

8. **Is Exclusive**: Leave **UNCHECKED** ☐
   - Both offers should work together

9. **Stop Further Rules**: Leave **UNCHECKED** ☐
   - Not stopping further rules

10. **Enable Auto Upgrade**: Leave **UNCHECKED** ☐

11. **Customer Role Limitations**: Leave empty
12. **Stores**: Select your store(s)
13. **Coupon Code**: Leave empty
14. **Date Range**: Set same promotional period as Offer 2

---

## 📦 **Step 2.3: Configure Products Tab**

1. Click the **"Products"** tab
2. **Buy Products Section**:
   - Add the same products: Product A, Product B, Product C
   - Set **Min Quantity** = `1` for all products
   - Set **Max Quantity** = `0` (unlimited) for all products

3. **Reward Products Section**:
   - Add the same products: Product A, Product B, Product C
   - For 50% off, reward can be any of the buy products

---

## 🎯 **Step 2.4: Configure Tiers Tab**

1. Click the **"Tiers"** tab
2. Click **"Add New Tier"** button
3. A popup will appear

### **Tier Configuration:**

4. **Name**: Enter `Tier 1 - Buy 1 Get 50% Off`
5. **Buy Quantity**: Enter `1`
   - Customer needs to buy 1 item
6. **Reward Quantity**: Enter `1`
   - Customer gets 1 item at 50% off
7. **Discount Type**: Select `Percentage`
8. **Discount Value**: Enter `50` (50% off)
9. **Auto Add Reward**: Leave **UNCHECKED** ☐
10. Click **"Save"**

---

## ✅ **Step 2.5: Save Offer 1**

1. Click the **"Save"** button
2. You should see a success message
3. Click **"Continue"** to return to the list

---

## 🔍 **Step 3: Verify Both Offers Are Created**

1. Check the Promotion Rules list
2. You should see both offers:
   ```
   Priority 1: Buy 2 Get 1 Free - Best Offer (Active: ✅)
   Priority 2: Buy 1 Get 50% Off Any Item (Active: ✅)
   ```

3. ⚠️ **Verify Priority Order**:
   - Offer 2 should have **Priority 1** (lower number)
   - Offer 1 should have **Priority 2** (higher number)
   - If wrong, edit and swap priorities

---

## 🧪 **Step 4: Test the Configuration**

### **Step 4.1: Clear All Caches**

1. Go to: **System** → **Configuration** → **Settings**
2. Click **"Clear Cache"** button
3. Wait for confirmation
4. **Clear browser cache**: Ctrl + Shift + Delete

---

### **Step 4.2: Add Test Products to Cart**

1. Navigate to your store's frontend
2. Add these products to shopping cart:
   - **Product A** ($1500): Quantity = `2`
   - **Product B** ($100): Quantity = `2`
   - **Product C** ($245): Quantity = `1`

3. Go to **Shopping Cart** page

---

### **Step 4.3: Verify Cart Display**

**Expected Cart Display:**

```
Shopping Cart:
┌───────────────────────────────────────┐
│ Product A × 2           $3,000.00     │
│ Product B × 2           $100.00        │ ← Should show discount breakdown
│ Product C × 1           $245.00       │
├───────────────────────────────────────┤
│ Subtotal                $3,345.00     │
│ Cart Savings            $150.00       │ ← NEW!
│ Total                   $3,195.00     │
└───────────────────────────────────────┘

✨ 2 simultaneous discounts applied!

Cart Savings Breakdown:
├─ Buy 2 Get 1 Free - Best Offer: $100.00
│  └─ (Applied to Product B - Free Item)
├─ Buy 1 Get 50% Off Any Item: $50.00  
│  └─ (Applied to Product B - 50% Off)
└─ Total Savings: $150.00

💰 Cheapest items automatically selected!
```

---

## 🐛 **Troubleshooting Common Issues**

### **Issue 1: "Some fields are missing"**

**Solution**: This is NORMAL for Buy X Get Y rules!
- Discount Limitation field is **intentionally hidden**
- Maximum Quantity field is **intentionally hidden**  
- Use the **Tiers tab** instead to set quantities

---

### **Issue 2: "Both discounts show on same item"**

**Solution**: Check Priority settings!
1. Edit "Buy 2 Get 1 Free" → Set Priority = `1`
2. Edit "Buy 1 Get 50% Off" → Set Priority = `2`
3. Lower number = Higher priority = Processes first

---

### **Issue 3: "No cart savings shown"**

**Solution**: Enable Cart Savings Breakdown!
1. Go to: **NopStation** → **DiscountManagerPlus** → **Configure**
2. Check **"Enable Cart Savings Breakdown"**
3. Click **"Save"**
4. Clear cache and test again

---

### **Issue 4: "Wrong discount amounts"**

**Solution**: Verify Tiers configuration!
1. Check "Buy 2 Get 1 Free" Tier:
   - Buy Quantity = `2`
   - Reward Quantity = `1`
   - Discount Type = `Free Item`
   - Discount Value = `100`

2. Check "Buy 1 Get 50% Off" Tier:
   - Buy Quantity = `1`
   - Reward Quantity = `1`
   - Discount Type = `Percentage`
   - Discount Value = `50`

---

### **Issue 5: "Both products get 100% discount"**

**Solution**: This is actually CORRECT behavior!
- Product B, Unit 1: $0 (FREE from Offer 2)
- Product B, Unit 2: $50 (50% off from Offer 1)
- **Total for Product B**: $100 → **$50 saved!**

---

## 📋 **Quick Reference Card**

### **Offer 2 Configuration Summary:**
```
Name: Buy 2 Get 1 Free - Best Offer
Rule Type: Buy X Get Y ⚠️
Active: ✅
Priority: 1 ⚠️ (LOWEST number = HIGHEST priority)
Discount Type: Free Item
Discount Value: 100
Discount Scope: Per Product
Is Exclusive: ☐ (unchecked)
Stop Further Rules: ☐ (unchecked)

Buy Products:
- Product A: Min Qty = 2, Max = 0
- Product B: Min Qty = 2, Max = 0  
- Product C: Min Qty = 2, Max = 0

Reward Products: Same as buy products

Tiers:
- Buy Qty: 2, Reward Qty: 1, Type: Free Item, Value: 100
```

### **Offer 1 Configuration Summary:**
```
Name: Buy 1 Get 50% Off Any Item
Rule Type: Buy X Get Y ⚠️
Active: ✅
Priority: 2 ⚠️ (HIGHER number = LOWER priority)
Discount Type: Percentage
Discount Value: 50
Discount Scope: Per Product
Is Exclusive: ☐ (unchecked)
Stop Further Rules: ☐ (unchecked)

Buy Products:
- Product A: Min Qty = 1, Max = 0
- Product B: Min Qty = 1, Max = 0
- Product C: Min Qty = 1, Max = 0

Reward Products: Same as buy products

Tiers:
- Buy Qty: 1, Reward Qty: 1, Type: Percentage, Value: 50
```

---

## 🎉 **Success Confirmation**

When setup is correct, you should see:

✅ **Admin Side**:
- Both offers appear in Promotion Rules list
- Correct priorities (1 and 2)
- Both show "Active: Yes"

✅ **Store Front**:
- Cart shows correct line totals
- Cart savings section appears
- Both discounts are listed separately
- Total savings amount is correct

✅ **Mathematical Verification**:
- Product B (2 units): Original $200 → Final $100 (saved $100)
- 1 unit: FREE ($0 saved)  
- 1 unit: 50% off ($50 saved)
- Total savings: $150 ✅

---

## 🚀 **Next Steps**

After successful setup:

1. **Test with different products**:
   - Try products with same price
   - Try products with wide price range
   - Test edge cases (minimum quantities)

2. **Monitor performance**:
   - Check discount application speed
   - Verify cart display accuracy
   - Test with large product quantities

3. **Deploy to production**:
   - Test thoroughly in staging first
   - Backup your database
   - Deploy during low-traffic period
   - Monitor for 24-48 hours

---

**🎯 Your dual-offer setup is now complete! The system will automatically select the cheapest items for maximum customer savings!**
