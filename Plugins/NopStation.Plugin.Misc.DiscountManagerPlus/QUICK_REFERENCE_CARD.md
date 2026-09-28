# 🎯 Quick Reference Card - Multiple Offers Setup

## 📋 **Critical Settings Checklist**

### **Offer 1: Buy 1 Get 50% Off**
```
┌─ INFO TAB ─────────────────────────────────────┐
│ Name: Buy 1 Get 50% Off Any Item              │
│ Rule Type: Buy X Get Y                         │
│ Priority: 2                                    │
│ Discount Type: Percentage                     │
│ Discount Value: 50                            │
│ Discount Scope: Per Product                   │
│ Is Exclusive: ❌ NO                            │
│ Stop Further Rules: ❌ NO                      │
└────────────────────────────────────────────────┘

┌─ PRODUCTS TAB ────────────────────────────────┐
│ BUY PRODUCTS:                                   │
│ • Product A: Min Qty 1, Max Qty 0              │
│ • Product B: Min Qty 1, Max Qty 0              │
│                                                 │
│ REWARD PRODUCTS: EMPTY ← IMPORTANT!            │
└────────────────────────────────────────────────┘

┌─ TIERS TAB ───────────────────────────────────┐
│ Tier 1:                                         │
│ • Buy Quantity: 1                              │
│ • Reward Quantity: 1                           │
│ • Discount Type: Percentage                    │
│ • Discount Value: 50                          │
│ • Reward Product: EMPTY ← IMPORTANT!          │
└────────────────────────────────────────────────┘
```

### **Offer 2: Buy 2 Get 1 Free**
```
┌─ INFO TAB ─────────────────────────────────────┐
│ Name: Buy 2 Get 1 Free - Best Offer            │
│ Rule Type: Buy X Get Y                         │
│ Priority: 1 ← HIGHEST!                          │
│ Discount Type: Free Item                       │
│ Discount Value: 100                            │
│ Discount Scope: Per Product                    │
│ Is Exclusive: ❌ NO                            │
│ Stop Further Rules: ❌ NO                      │
└────────────────────────────────────────────────┘

┌─ PRODUCTS TAB ────────────────────────────────┐
│ BUY PRODUCTS:                                   │
│ • Product A: Min Qty 2, Max Qty 0              │
│ • Product B: Min Qty 2, Max Qty 0              │
│                                                 │
│ REWARD PRODUCTS: EMPTY ← IMPORTANT!            │
└────────────────────────────────────────────────┘

┌─ TIERS TAB ───────────────────────────────────┐
│ Tier 1:                                         │
│ • Buy Quantity: 2                              │
│ • Reward Quantity: 1                           │
│ • Discount Type: Free Item                     │
│ • Discount Value: 100                          │
│ • Reward Product: EMPTY ← IMPORTANT!          │
└────────────────────────────────────────────────┘
```

---

## 🚨 **CRITICAL: These Must Be Set Correctly!**

### ✅ **DO THIS:**
- **Priority**: Offer 2 = 1, Offer 1 = 2
- **Is Exclusive**: ❌ **UNCHECKED** (both offers)
- **Stop Further Rules**: ❌ **UNCHECKED** (both offers)
- **Discount Scope**: **Per Product** (both offers)
- **Reward Products**: **EMPTY** (both offers)
- **Reward Product (in Tiers)**: **EMPTY** (both offers)

### ❌ **DON'T DO THIS:**
- Don't set **Is Exclusive** to YES
- Don't set **Stop Further Rules** to YES
- Don't set **Discount Scope** to "Whole Cart"
- Don't add products to **Reward Products** section
- Don't set **Reward Product** in tiers dropdown

---

## 🧪 **Test Scenario**

### **Add to Cart:**
```
Product A ($10) × 2
Product B ($20) × 2
Product C ($30) × 1
```

### **Expected Cart Display:**
```
✨ Cart Savings

Buy 2 Get 1 Free - Best Offer: $10.00
  └─ (Applied to Product A)
  
Buy 1 Get 50% Off Any Item: $10.00
  └─ (Applied to Product B)

Total Savings: $20.00
```

### **Expected Discounts:**
```
Product A (1st): FREE ($0) ← Cheapest item
Product A (2nd): $10
Product B (1st): $10 (50% off) ← Next cheapest
Product B (2nd): $20
Product C: $30

Total: $70 (Saved $20)
```

---

## 🔧 **Troubleshooting Quick Fixes**

### **Problem: No discounts showing**
```
Solution:
1. Check both rules are Active (green status)
2. Clear cache: System → Configuration → Clear Cache
3. Verify products are in Buy Products section
4. Check Min Quantity is set correctly
```

### **Problem: Wrong items discounted**
```
Solution:
1. Verify Reward Products sections are EMPTY
2. Check Reward Product dropdown in tiers is EMPTY
3. Verify Discount Scope is "Per Product"
4. Check Priority values (Offer 2 = 1, Offer 1 = 2)
```

### **Problem: Same item multiple discounts**
```
Solution:
1. Verify Stop Further Rules is UNCHECKED
2. Verify Is Exclusive is UNCHECKED
3. Check quantity tracking is working
```

---

## 🎯 **Setup Order**

1. **Navigate**: Admin → NopStation → Discount Manager Plus → Promotion Rules
2. **Create Offer 1**: Click "Add New" → Configure all tabs → Save
3. **Create Offer 2**: Click "Add New" → Configure all tabs → Save
4. **Verify**: Check both rules appear in list with correct priorities
5. **Test**: Add products to cart → Check Shopping Cart page
6. **Clear Cache**: If discounts don't show immediately

---

## 📞 **Quick Navigation Path**

```
Admin Panel
└─ NopStation (left menu)
   └─ Discount Manager Plus
      └─ Promotion Rules
         └─ Add New (top right button)
            └─ Create Promotion Rule Page
               ├─ Info Tab (settings)
               ├─ Products Tab (buy/reward products)
               ├─ Conditions Tab (optional)
               └─ Tiers Tab (quantity requirements)
```

---

## ✅ **Final Verification Checklist**

Before testing:

- [ ] Both rules show **Active** status (green)
- [ ] **Offer 2** has **Priority 1** (higher number = higher priority)
- [ ] **Offer 1** has **Priority 2**
- [ ] Same products in both rules' **Buy Products** sections
- [ ] **Reward Products** sections are **EMPTY** in both
- [ ] **Reward Product** dropdowns in tiers are **EMPTY**
- [ ] **Is Exclusive** is **UNCHECKED** in both
- [ ] **Stop Further Rules** is **UNCHECKED** in both
- [ ] **Discount Scope** is **"Per Product"** in both
- [ ] Both rules have **Tier 1** configured
- [ ] Cache has been cleared
- [ ] Products are added to cart with correct quantities

---

## 🎉 **Success Indicators**

When setup is correct, you'll see:

```
✨ 2 simultaneous discounts applied!

Cart Savings Section:
• Discount breakdown by offer
• Which items received discounts
• Total savings amount
• Attention messages about cheapest items
```

---

**Print this card and keep it handy while setting up your multiple offers!** 📋