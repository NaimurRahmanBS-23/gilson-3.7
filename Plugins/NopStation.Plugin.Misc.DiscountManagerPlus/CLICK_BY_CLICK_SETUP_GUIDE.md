# 🎯 Click-By-Click Setup Guide - Multiple Offers Scenario

## 📋 **Complete Setup for Scenario 2: Two Simultaneous Offers**

This guide will walk you through **EVERY CLICK** needed to set up multiple simultaneous offers with cheapest-item discount logic.

---

## 🚀 **PART 1: Navigate to Promotion Rules**

### **Step 1: Log into Admin Panel**
1. Open your browser
2. Go to: `http://yourstore.com/admin`
3. Enter your admin credentials
4. Click **Login**

### **Step 2: Navigate to Promotion Rules**
1. In the left admin menu, look for **"NopStation"** section
2. Click on **"Discount Manager Plus"**
3. Click on **"Promotion Rules"** (you'll see a list of existing rules)

---

## 🎯 **PART 2: Create Offer 1 - Buy 1 Get 50% Off**

### **Step 3: Create New Promotion Rule**
1. Click the **"Add New"** button (top right of the page)
2. You'll see the **"Create Promotion Rule"** page with multiple tabs

### **Step 4: Configure Basic Info Tab**

#### **4.1 Rule Information**
- **Name**: `Buy 1 Get 50% Off Any Item`
- **Description**: `Buy 1 qualifying product, get 50% off another item`
- **Rule Type**: Select **"Buy X Get Y"** from dropdown
- **Is Active**: ✅ Check this box (make rule active)
- **Priority**: Enter `2` (this will process second)
- **Display Order**: Enter `2`
- **Start Date**: (Optional) Set when offer starts
- **End Date**: (Optional) Set when offer ends
- **Stores**: Select your store(s)
- **Customer Roles**: Select which customers can use this offer (or leave empty for all)
- **Limit Per Customer**: Enter `0` for unlimited

#### **4.2 Discount Settings**
- **Discount Type**: Select **"Percentage"** from dropdown
- **Discount Value**: Enter `50` (for 50% off)
- **Discount Scope**: Select **"Per Product"** from dropdown
- **Maximum Discount Amount**: Leave empty or enter `0` for unlimited
- **Is Exclusive**: ❌ **UNCHECKED** (leave empty - allows multiple offers)
- **Stop Further Rules**: ❌ **UNCHECKED** (leave empty - allows stacking)
- **Enable Auto Upgrade**: ❌ Leave unchecked
- **Enable Stacked Cumulative Mode**: ❌ Leave unchecked

#### **4.4 Click "Save and Continue"** button

---

### **Step 5: Configure Products Tab**

#### **5.1 Add Buy Products**
1. Click on the **"Products"** tab at the top
2. You'll see two sections: **"Buy Products"** and **"Reward Products"**

#### **5.2 Add Products to "Buy Products" Section**
1. In the **"Buy Products"** section, click **"Add New"** button
2. A popup window will appear: **"Add Rule Product"**

#### **5.3 Configure First Buy Product**
In the popup window:
- **Product**: Select **"Product A"** (or your first product)
- **Min Quantity**: Enter `1`
- **Max Quantity**: Enter `0` (0 = unlimited)
- **Is Reward Product**: ❌ **UNCHECKED** (this is a BUY product)

3. Click **"Insert"** button

#### **5.4 Add More Buy Products (Optional)**
If you want multiple products to qualify:
1. Click **"Add New"** again in Buy Products section
2. Select **"Product B"** from dropdown
3. **Min Quantity**: Enter `1`
4. **Max Quantity**: Enter `0`
5. **Is Reward Product**: ❌ **UNCHECKED**
6. Click **"Insert"**

Repeat for any other products you want to include.

#### **5.5 Leave "Reward Products" Section EMPTY**
- **IMPORTANT**: For cheapest-item logic, leave Reward Products section EMPTY
- The system will automatically select the cheapest item from your buy products

#### **5.6 Click "Save and Continue"** button

---

### **Step 6: Configure Conditions Tab (Optional)**

#### **6.1 Skip Conditions (Recommended)**
- For basic setup, leave this section **EMPTY**
- Conditions allow you to restrict when offers apply (like customer groups, shipping methods, etc.)

#### **6.2 Click "Save and Continue"** button

---

### **Step 7: Configure Tiers Tab**

#### **7.1 Add Tier Configuration**
1. Click on the **"Tiers"** tab at the top
2. You'll see a **"Tiers"** section with an **"Add New"** button
3. Click **"Add New"** button
4. A popup window will appear: **"Add Rule Tier"**

#### **7.2 Configure Tier Settings**
In the popup window:
- **Min Quantity**: Enter `1` (minimum items to buy)
- **Max Quantity**: Enter `0` (0 = unlimited)
- **Buy Quantity**: Enter `1` (how many they need to buy)
- **Reward Quantity**: Enter `1` (how many items get discount)
- **Discount Type**: Select **"Percentage"** from dropdown
- **Discount Value**: Enter `50` (for 50% off)
- **Reward Product**: Leave this **EMPTY** (important for cheapest-item logic)
- **Auto Add Reward**: ❌ **UNCHECKED**
- **Display Order**: Enter `1`

#### **7.3 Click "Insert"** button

#### **7.4 Click "Save and Continue"** button

---

### **Step 8: Verify and Save Offer 1**
1. Review all settings on each tab
2. Click the big **"Save"** button (top right)
3. You should see a success message: **"The promotion rule has been inserted successfully"**

---

## 🎯 **PART 3: Create Offer 2 - Buy 2 Get 1 Free**

### **Step 9: Create Second Promotion Rule**
1. Click the **"Add New"** button again (top right)
2. You'll see a new **"Create Promotion Rule"** page

### **Step 10: Configure Basic Info Tab**

#### **10.1 Rule Information**
- **Name**: `Buy 2 Get 1 Free - Best Offer`
- **Description**: `Buy 2 qualifying products, get 1 free (100% discount)`
- **Rule Type**: Select **"Buy X Get Y"** from dropdown
- **Is Active**: ✅ **Check this box** (make rule active)
- **Priority**: Enter `1` (this will process FIRST - highest priority)
- **Display Order**: Enter `1`
- **Start Date**: (Optional) Set when offer starts
- **End Date**: (Optional) Set when offer ends
- **Stores**: Select your store(s)
- **Customer Roles**: Select which customers can use this offer (or leave empty for all)
- **Limit Per Customer**: Enter `0` for unlimited

#### **10.2 Discount Settings**
- **Discount Type**: Select **"Free Item"** from dropdown
- **Discount Value**: Enter `0` (not used for Free Item)
- **Discount Scope**: Select **"Per Product"** from dropdown
- **Maximum Discount Amount**: Leave empty or enter `0` for unlimited
- **Is Exclusive**: ❌ **UNCHECKED** (leave empty - allows multiple offers)
- **Stop Further Rules**: ❌ **UNCHECKED** (leave empty - allows stacking)
- **Enable Auto Upgrade**: ❌ Leave unchecked
- **Enable Stacked Cumulative Mode**: ❌ Leave unchecked

#### **10.3 Click "Save and Continue"** button

---

### **Step 11: Configure Products Tab**

#### **11.1 Add Buy Products**
1. Click on the **"Products"** tab
2. In the **"Buy Products"** section, click **"Add New"** button

#### **11.2 Configure First Buy Product**
In the popup window:
- **Product**: Select **"Product A"** (same products as Offer 1)
- **Min Quantity**: Enter `2` (minimum to qualify for this offer)
- **Max Quantity**: Enter `0` (0 = unlimited)
- **Is Reward Product**: ❌ **UNCHECKED** (this is a BUY product)

3. Click **"Insert"** button

#### **11.3 Add More Buy Products (Optional)**
1. Click **"Add New"** again in Buy Products section
2. Select **"Product B"** from dropdown
3. **Min Quantity**: Enter `2`
4. **Max Quantity**: Enter `0`
5. **Is Reward Product**: ❌ **UNCHECKED**
6. Click **"Insert"**

Repeat for other products.

#### **11.4 Leave "Reward Products" Section EMPTY**
- **IMPORTANT**: Leave Reward Products section EMPTY for cheapest-item logic

#### **11.5 Click "Save and Continue"** button

---

### **Step 12: Configure Conditions Tab (Optional)**

- Skip this section for basic setup
- Click **"Save and Continue"** button

---

### **Step 13: Configure Tiers Tab**

#### **13.1 Add Tier Configuration**
1. Click on the **"Tiers"** tab
2. Click **"Add New"** button
3. A popup will appear: **"Add Rule Tier"**

#### **13.2 Configure Tier Settings**
In the popup window:
- **Min Quantity**: Enter `2` (minimum items to buy)
- **Max Quantity**: Enter `0` (0 = unlimited)
- **Buy Quantity**: Enter `2` (how many they need to buy)
- **Reward Quantity**: Enter `1` (how many items get free)
- **Discount Type**: Select **"Free Item"** from dropdown
- **Discount Value**: Enter `100` (100% discount = free)
- **Reward Product**: Leave this **EMPTY** (important for cheapest-item logic)
- **Auto Add Reward**: ❌ **UNCHECKED**
- **Display Order**: Enter `1`

#### **13.3 Click "Insert"** button

#### **13.4 Click "Save and Continue"** button

---

### **Step 14: Verify and Save Offer 2**
1. Review all settings on each tab
2. Click the big **"Save"** button (top right)
3. You should see a success message: **"The promotion rule has been inserted successfully"**

---

## 🎯 **PART 4: Verify Setup**

### **Step 15: Check Promotion Rules List**
1. You should now see **2 promotion rules** in the list
2. Verify the order:
   - **Priority 1**: `Buy 2 Get 1 Free - Best Offer` (processes first)
   - **Priority 2**: `Buy 1 Get 50% Off Any Item` (processes second)

### **Step 16: Verify Both Rules are Active**
- Make sure both rules have ✅ **"Is Active"** checked
- Both should show status as **"Active"** in the list

---

## 🧪 **PART 5: Test the Setup**

### **Step 17: Add Products to Cart**
1. Go to your store front: `http://yourstore.com`
2. Add **Product A** (price: $10) - Quantity: `2`
3. Add **Product B** (price: $20) - Quantity: `2`
4. Add **Product C** (price: $30) - Quantity: `1`
5. **Total Cart**: 5 items, $90 value

### **Step 18: Check Cart Page**
1. Go to **Shopping Cart** page
2. Look for **"Cart Savings"** section (usually near order summary)
3. You should see:

#### **Expected Output:**
```
🎉 Cart Savings
├─ Buy 2 Get 1 Free - Best Offer: $10.00
├─ Buy 1 Get 50% Off Any Item: $10.00
└─ Total Savings: $20.00
```

#### **Discount Breakdown:**
- **Product A (1st unit)**: FREE ($0) - Saved $10
- **Product A (2nd unit)**: $10 (full price)
- **Product B (1st unit)**: $10 (50% off) - Saved $10
- **Product B (2nd unit)**: $20 (full price)
- **Product C**: $30 (full price)
- **Final Total**: $70 (was $90, saved $20)

### **Step 19: Check Attention Messages**
You should also see messages like:
```
✨ 2 simultaneous discounts applied to your cart!
💰 Cheapest items automatically selected for maximum savings
```

---

## 🔧 **PART 6: Troubleshooting**

### **Problem: No discounts showing in cart**

#### **Solution 1: Check Rule Active Status**
1. Go back to Admin → Promotion Rules
2. Make sure both rules have ✅ **"Is Active"** checked
3. Click **"Save"** on any rule that's not active

#### **Solution 2: Check Product Assignment**
1. Edit each rule → **Products** tab
2. Make sure products are added to **"Buy Products"** section
3. **Min Quantity** should be set correctly (1 for Offer 1, 2 for Offer 2)

#### **Solution 3: Check Tier Configuration**
1. Edit each rule → **Tiers** tab
2. Make sure tiers are configured correctly:
   - Offer 1: Buy 1, Reward 1, 50% off
   - Offer 2: Buy 2, Reward 1, 100% off (Free Item)

#### **Solution 4: Check Priority Settings**
1. In Promotion Rules list, verify:
   - Offer 2 (Buy 2 Get 1 Free) has **Priority 1**
   - Offer 1 (Buy 1 Get 50% Off) has **Priority 2**

#### **Solution 5: Clear Cache**
1. Go to **System → Configuration**
2. Find **"Performance"** section
3. Click **"Clear Cache"** button
4. Refresh your shopping cart page

---

### **Problem: Same item getting multiple discounts**

#### **Solution: Check Stop Further Rules Setting**
1. Edit each rule → **Info** tab
2. Make sure **"Stop Further Rules"** is **UNCHECKED**
3. Make sure **"Is Exclusive"** is **UNCHECKED**

---

### **Problem: Wrong items getting discounted**

#### **Solution: Check Discount Scope**
1. Edit each rule → **Info** tab
2. Make sure **"Discount Scope"** is set to **"Per Product"**
3. **NOT** "Whole Cart"

---

## 📋 **PART 7: Complete Configuration Summary**

### **Offer 1: Buy 1 Get 50% Off**
```
Name: Buy 1 Get 50% Off Any Item
Rule Type: Buy X Get Y
Priority: 2
Discount Type: Percentage
Discount Value: 50
Discount Scope: Per Product
Is Exclusive: ❌ No
Stop Further Rules: ❌ No

Products (Buy Products):
- Product A: Min Qty 1, Max Qty 0
- Product B: Min Qty 1, Max Qty 0
- Product C: Min Qty 1, Max Qty 0

Tiers:
- Buy 1, Get 1, 50% Off

Reward Products: EMPTY
```

### **Offer 2: Buy 2 Get 1 Free**
```
Name: Buy 2 Get 1 Free - Best Offer
Rule Type: Buy X Get Y
Priority: 1 (Processes First)
Discount Type: Free Item
Discount Value: 100
Discount Scope: Per Product
Is Exclusive: ❌ No
Stop Further Rules: ❌ No

Products (Buy Products):
- Product A: Min Qty 2, Max Qty 0
- Product B: Min Qty 2, Max Qty 0
- Product C: Min Qty 2, Max Qty 0

Tiers:
- Buy 2, Get 1, 100% Off (Free)

Reward Products: EMPTY
```

---

## 🎯 **PART 8: Advanced Configuration (Optional)**

### **Option A: Different Products for Each Offer**

If you want different products to trigger different offers:

#### **Offer 1 (Buy 1 Get 50% Off)**
- **Buy Products**: Product A, Product B, Product C
- **Min Quantity**: 1 for each

#### **Offer 2 (Buy 2 Get 1 Free)**
- **Buy Products**: Product D, Product E, Product F
- **Min Quantity**: 2 for each

### **Option B: Same Products, Different Quantities**

If you want to use the same products but different trigger points:

#### **Setup:**
- Both offers use the SAME products
- **Offer 1**: Min Quantity 1 (triggers with 1 item)
- **Offer 2**: Min Quantity 2 (triggers with 2+ items)
- **Priority**: Offer 2 processes first

---

## ✅ **PART 9: Final Checklist**

Before testing, verify:

- [ ] Both rules are **Active** (green status)
- [ ] **Offer 2** has **Priority 1** (higher)
- [ ] **Offer 1** has **Priority 2** (lower)
- [ ] Both rules have **same products** in Buy Products section
- [ ] **Reward Products** sections are **EMPTY** in both rules
- [ ] **Is Exclusive** is **UNCHECKED** in both rules
- [ ] **Stop Further Rules** is **UNCHECKED** in both rules
- [ ] **Discount Scope** is **"Per Product"** in both rules
- [ ] **Tiers** are configured correctly in both rules
- [ ] Cache has been **cleared**

---

## 🎉 **PART 10: Success Indicators**

When properly configured, you should see:

### **In Shopping Cart:**
```
✨ 2 simultaneous discounts applied to your cart!

Cart Savings
├─ Buy 2 Get 1 Free - Best Offer: $10.00
│  └─ (Applied to Product A)
├─ Buy 1 Get 50% Off Any Item: $10.00
│  └─ (Applied to Product B)
└─ Total Savings: $20.00
```

### **Expected Behavior:**
- ✅ Cheapest item gets 100% discount (free)
- ✅ Next cheapest item gets 50% discount
- ✅ No item receives both discounts
- ✅ Remaining items pay full price
- ✅ Total discount is maximized for customer

---

## 📞 **PART 11: Support & Help**

If you still have issues after following this guide:

1. **Check Plugin Logs**:
   - Go to `/Logs/` folder in your website root
   - Look for `DiscountManagerPlus.log` file
   - Check for error messages

2. **Verify Plugin Version**:
   - Admin → Configuration → Plugins
   - Find "NopStation.Plugin.Misc.DiscountManagerPlus"
   - Version should be `4.90.1.0` or higher

3. **Test One Rule at a Time**:
   - Deactivate Offer 1, test Offer 2 only
   - Deactivate Offer 2, test Offer 1 only
   - Then activate both and test together

4. **Reset Plugin Settings**:
   - Admin → Configuration → Plugins
   - Find DiscountManagerPlus plugin
   - Click **"Edit"**
   - Click **"Reset to Default"** (if available)

---

## 🎯 **Quick Reference Card**

### **Priority Order (Critical!)**
```
Offer 2 (Buy 2 Get 1 Free): Priority 1 ← Process First
Offer 1 (Buy 1 Get 50% Off): Priority 2 ← Process Second
```

### **Key Settings**
```
Is Exclusive: NO ← Allows multiple offers
Stop Further Rules: NO ← Allows stacking
Discount Scope: Per Product ← Item-level discounts
Reward Products: EMPTY ← Auto-select cheapest
```

### **Cart Test Scenario**
```
Add to Cart:
- Product A ($10) × 2
- Product B ($20) × 2  
- Product C ($30) × 1

Expected Result:
- Product A (1st): FREE ($0) ← Cheapest
- Product A (2nd): $10
- Product B (1st): $10 (50% off) ← Next cheapest
- Product B (2nd): $20
- Product C: $30
- Total: $70 (Saved $20)
```

---

## 🎉 **Congratulations!**

You've successfully configured multiple simultaneous offers with automatic cheapest-item discount logic! Your customers will now enjoy the best possible savings when they add multiple products to their cart.

**Next Steps:**
1. Test thoroughly with different product combinations
2. Monitor cart savings display in store front
3. Adjust priorities if needed
4. Add more products to expand the offer
5. Create additional offers with different discount percentages

**Your plugin is now ready for production use!** 🚀
