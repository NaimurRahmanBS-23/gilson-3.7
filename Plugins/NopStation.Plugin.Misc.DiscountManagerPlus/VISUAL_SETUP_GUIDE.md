# 🎯 Visual Setup Guide - Multiple Offers Flow

## 📊 **How Multiple Offers Work - Visual Flow**

```
CUSTOMER ADDS 5 PRODUCTS TO CART
┌─────────────────────────────────────────┐
│ Cart:                                    │
│ • Product A ($10) × 2                    │
│ • Product B ($20) × 2                    │
│ • Product C ($30) × 1                    │
│ Total: 5 items, $90 value                │
└─────────────────────────────────────────┘
              ↓
PLUGIN PROCESSES RULES BY PRIORITY
┌─────────────────────────────────────────┐
│ Rule Processing Order:                   │
│ 1. Buy 2 Get 1 Free (Priority 1)        │
│ 2. Buy 1 Get 50% Off (Priority 2)       │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ STEP 1: Process "Buy 2 Get 1 Free"      │
├─────────────────────────────────────────┤
│ Needs: 2 items to qualify                │
│ Has: 5 items in cart ✓                   │
│                                          │
│ Calculates: 1 free item (100% discount)  │
│                                          │
│ Selection Logic:                         │
│ Sort items by price (cheapest first):    │
│ 1. Product A ($10) ← SELECTED FOR FREE  │
│ 2. Product A ($10)                       │
│ 3. Product B ($20)                       │
│ 4. Product B ($20)                       │
│ 5. Product C ($30)                       │
│                                          │
│ Result:                                  │
│ • Product A (1st unit): FREE ($0)       │
│ • Tracks: 1 unit consumed from line A    │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ STEP 2: Process "Buy 1 Get 50% Off"    │
├─────────────────────────────────────────┤
│ Needs: 1 item to qualify                 │
│ Has: 4 remaining items (1 was consumed)  │
│                                          │
│ Calculates: 1 item at 50% discount       │
│                                          │
│ Remaining items after Step 1:           │
│ 1. Product A ($10) ← 1 unit remaining    │
│ 2. Product B ($20)                       │
│ 3. Product B ($20)                       │
│ 4. Product C ($30)                       │
│                                          │
│ Selection Logic:                         │
│ Sort remaining by price (cheapest first):│
│ 1. Product A ($10) ← SELECTED FOR 50%    │
│ 2. Product B ($20)                       │
│ 3. Product B ($20)                       │
│ 4. Product C ($30)                       │
│                                          │
│ Result:                                  │
│ • Product A (2nd unit): $5 (50% off)    │
│ • Tracks: 1 more unit consumed from A    │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ FINAL CART CALCULATION                   │
├─────────────────────────────────────────┤
│ Product A (1st unit): FREE ($0)          │
│ Product A (2nd unit): $5 (50% off)       │
│ Product B (1st unit): $20 (full price)   │
│ Product B (2nd unit): $20 (full price)   │
│ Product C: $30 (full price)              │
│                                          │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│ Total: $75 (was $90, saved $15)         │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ ATTENTION MESSAGES SHOWN TO CUSTOMER    │
├─────────────────────────────────────────┤
│ ✨ 2 simultaneous discounts applied!    │
│                                          │
│ 💰 Cart Savings:                        │
│ • Buy 2 Get 1 Free: $10.00              │
│   └─ (Applied to Product A - Free)     │
│ • Buy 1 Get 50% Off: $5.00             │
│   └─ (Applied to Product A - 50% off)   │
│                                          │
│ • Total Savings: $15.00                │
│ • Cheapest items automatically selected │
└─────────────────────────────────────────┘
```

---

## 🎯 **Admin Panel Setup Screenshots Guide**

### **Screen 1: Promotion Rules List**
```
┌─ ADMIN PANEL ──────────────────────────────────────┐
│                                                      │
│  Promotion Rules / List                              │
│  ─────────────────────────────────────────────────  │
│                                                      │
│  [Add New]  [Export]  [Import]                      │
│                                                      │
│  ┌─ Promotion Rules ──────────────────────────────┐ │
│  │                                                 │ │
│  │  Buy 2 Get 1 Free - Best Offer    [Active] ⬈1  │ │
│  │  Buy 1 Get 50% Off Any Item    [Active] ⬈2     │ │
│  │                                                 │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ⬈ = Priority number (lower = processes first)     │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### **Screen 2: Create/Edit Rule - Info Tab**
```
┌─ Create Promotion Rule ─────────────────────────────┐
│                                                      │
│  [Info] [Products] [Conditions] [Tiers]             │
│  ────────────────────────────────────────────────   │
│                                                      │
│  Rule Information                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │ Name:                    [Buy 1 Get 50% Off ]  │  │
│  │ Description:             [Buy 1, get 50% off]  │  │
│  │ Rule Type:        [Buy X Get Y       ▼]       │  │
│  │ Priority:                  [2]               │  │
│  │ Is Active:          [✓] (Check this!)     │  │
│  └──────────────────────────────────────────────┘  │
│                                                      │
│  Discount Settings                                   │
│  ┌──────────────────────────────────────────────┐  │
│  │ Discount Type:    [Percentage        ▼]       │  │
│  │ Discount Value:               [50]           │  │
│  │ Discount Scope:    [Per Product       ▼]       │  │
│  │ Is Exclusive:       [✗] (UNCHECKED!)     │  │
│  │ Stop Further Rules:[✗] (UNCHECKED!)     │  │
│  └──────────────────────────────────────────────┘  │
│                                                      │
│  [Save] [Save and Continue] [Cancel]                 │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### **Screen 3: Products Tab**
```
┌─ Create Promotion Rule - Products ─────────────────┐
│                                                      │
│  [Info] [Products] [Conditions] [Tiers]             │
│  ────────────────────────────────────────────────   │
│                                                      │
│  Buy Products Section                                │
│  ┌──────────────────────────────────────────────┐  │
│  │ [Add New]                                       │  │
│  │                                                 │  │
│  │ ┌─ Product ─────────────────────────────────┐ │  │
│  │ │ Product: [Product A            ▼]          │ │  │
│  │ │ Min Qty:              [1]                 │ │  │
│  │ │ Max Qty:              [0] (unlimited)     │ │  │
│  │ │ Is Reward:  [✗] (This is BUY product)  │ │  │
│  │ │ [Edit] [Delete]                            │ │  │
│  │ └────────────────────────────────────────────┘ │  │
│  │                                                 │  │
│  │ ┌─ Product ─────────────────────────────────┐ │  │
│  │ │ Product: [Product B            ▼]          │ │  │
│  │ │ Min Qty:              [1]                 │ │  │
│  │ │ Max Qty:              [0]                 │ │  │
│  │ │ Is Reward:  [✗]                           │ │  │
│  │ │ [Edit] [Delete]                            │ │  │
│  │ └────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────┘  │
│                                                      │
│  Reward Products Section                            │
│  ┌──────────────────────────────────────────────┐  │
│  │ [Add New]                                       │  │
│  │                                                 │  │
│  │ ← LEAVE THIS EMPTY!                            │  │
│  │ (System auto-selects cheapest item)            │  │
│  │                                                 │  │
│  └──────────────────────────────────────────────┘  │
│                                                      │
│  [Save] [Save and Continue] [Cancel]                 │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### **Screen 4: Tiers Tab**
```
┌─ Create Promotion Rule - Tiers ──────────────────────┐
│                                                       │
│  [Info] [Products] [Conditions] [Tiers]              │
│  ────────────────────────────────────────────────    │
│                                                       │
│  Tiers Configuration                                  │
│  ┌───────────────────────────────────────────────┐  │
│  │ [Add New]                                        │  │
│  │                                                  │  │
│  │ ┌─ Tier 1 ───────────────────────────────────┐ │  │
│  │ │ Min Quantity:              [1]             │ │  │
│  │ │ Max Quantity:              [0] (unlimited) │ │  │
│  │ │ Buy Quantity:              [1]             │ │  │
│  │ │ Reward Quantity:           [1]             │ │  │
│  │ │                                                  │ │  │
│  │ │ Discount Type:   [Percentage        ▼]     │ │  │
│  │ │ Discount Value:              [50]          │ │  │
│  │ │                                                  │ │  │
│  │ │ Reward Product: [          ▼]               │ │  │
│  │ │                  ↑ LEAVE EMPTY!            │ │  │
│  │ │                                                  │ │  │
│  │ │ Auto Add Reward:    [✗] (UNCHECKED)      │ │  │
│  │ │ Display Order:              [1]           │ │  │
│  │ │ [Edit] [Delete]                                │ │  │
│  │ └──────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────┘  │
│                                                       │
│  [Save] [Save and Continue] [Cancel]                  │
│                                                       │
└───────────────────────────────────────────────────────┘
```

---

## 🔍 **Common Mistakes to Avoid**

### ❌ **Mistake 1: Wrong Priority Order**
```
WRONG:
Offer 1 (Buy 1 Get 50% Off): Priority 1 ← Processes first
Offer 2 (Buy 2 Get 1 Free): Priority 2

RIGHT:
Offer 2 (Buy 2 Get 1 Free): Priority 1 ← Processes first
Offer 1 (Buy 1 Get 50% Off): Priority 2
```

### ❌ **Mistake 2: Setting Reward Products**
```
WRONG:
Reward Products Section:
• Product A ← Don't add products here!

RIGHT:
Reward Products Section:
← Leave EMPTY!
```

### ❌ **Mistake 3: Wrong Min Quantity**
```
WRONG:
Offer 1 (Buy 1 Get 50% Off): Min Qty 2
Offer 2 (Buy 2 Get 1 Free): Min Qty 1

RIGHT:
Offer 1 (Buy 1 Get 50% Off): Min Qty 1
Offer 2 (Buy 2 Get 1 Free): Min Qty 2
```

### ❌ **Mistake 4: Setting Is Exclusive to YES**
```
WRONG:
Is Exclusive: ✓ ← Only ONE offer will apply!

RIGHT:
Is Exclusive: ✗ ← Multiple offers can apply
```

---

## 🧪 **Test Scenarios Matrix**

### **Scenario 1: Minimum Qualifying Cart**
```
Add to Cart:
• Product A ($10) × 3

Expected Result:
• Offer 2 (Buy 2 Get 1 Free): 1 item free ($10 saved)
• Offer 1 (Buy 1 Get 50% Off): 1 item at 50% ($5 saved)
• Total: $15 (was $30, saved $15)
```

### **Scenario 2: Different Price Products**
```
Add to Cart:
• Product A ($10) × 1
• Product B ($20) × 1
• Product C ($30) × 1
• Product D ($40) × 1
• Product E ($50) × 1

Expected Result:
• Offer 2: Product A FREE ($10 saved)
• Offer 1: Product B 50% off ($10 saved)
• Total: $190 (was $210, saved $20)
```

### **Scenario 3: Multiple Units Same Product**
```
Add to Cart:
• Product X ($25) × 5

Expected Result:
• Offer 2: 1 unit FREE ($25 saved)
• Offer 1: 1 unit at 50% ($12.50 saved)
• Total: $87.50 (was $125, saved $37.50)
```

---

## 🎯 **Success Checklist**

### **Before Testing:**
```
□ Both rules appear in Promotion Rules list
□ Both rules show "Active" status (green)
□ Priority 1: Buy 2 Get 1 Free
□ Priority 2: Buy 1 Get 50% Off
□ Same products in both rules' Buy Products
□ Reward Products sections EMPTY
□ Reward Product dropdowns in tiers EMPTY
□ Is Exclusive UNCHECKED
□ Stop Further Rules UNCHECKED
□ Discount Scope: Per Product
□ Both rules have Tier 1 configured
□ Cache cleared after setup
```

### **Expected Cart Display:**
```
✨ Cart Savings Section Visible
✅ Discount breakdown shown by offer
✅ Which items received discounts displayed
✅ Total savings amount calculated correctly
✅ Attention messages about cheapest items shown
✅ Cheapest items automatically selected
✅ Same item doesn't get multiple discounts
```

---

## 🚀 **Quick Start Commands**

### **Navigate to Setup:**
```
1. Login to Admin: http://yourstore.com/admin
2. Go to: NopStation → Discount Manager Plus → Promotion Rules
3. Click "Add New"
```

### **Quick Setup Order:**
```
1. Create Offer 2 first (Buy 2 Get 1 Free, Priority 1)
2. Create Offer 1 second (Buy 1 Get 50% Off, Priority 2)
3. Add same products to both rules
4. Leave Reward Products EMPTY
5. Configure Tier 1 for each rule
6. Save both rules
7. Clear cache
8. Test with products in cart
```

### **Verification Steps:**
```
1. Check Promotion Rules list shows both rules
2. Verify both are Active (green status)
3. Add qualifying products to cart
4. Check Shopping Cart page
5. Look for Cart Savings section
6. Verify discount calculations
7. Check attention messages
```

---

## 📞 **Need Help?**

### **Check These First:**
```
1. Both rules Active?
2. Priority order correct?
3. Same products in both rules?
4. Reward Products sections empty?
5. Cache cleared?
6. Products added with correct quantities?
```

### **Reset Procedure:**
```
1. Deactivate both rules
2. Clear cache
3. Reactivate both rules
4. Clear cache again
5. Test with simple cart (3 products)
```

---

**🎉 You're all set! Follow this visual guide and your multiple offers will work perfectly!**