# Quick Field Reference Card

## 🚀 **Essential Fields Quick Guide**

### **Rule Type Selection**
```
┌─────────────────────────────────────────┐
│ Rule Type        → Best For              │
├─────────────────────────────────────────┤
│ Product Based    → Targeted products    │
│ Combo Pricing    → Bundle deals         │
│ Buy X Get Y      → Conditional rewards  │
│ Cart Condition   → Cart requirements    │
│ Subtotal Based   → Spending triggers    │
└─────────────────────────────────────────┘
```

### **Discount Type Selection**
```
┌─────────────────────────────────────────┐
│ Discount Type    → Formula              │
├─────────────────────────────────────────┤
│ Percentage       → Price × (Value/100)  │
│ Fixed Amount     → Value (up to price)  │
│ Fixed Bundle     → Set total price      │
│ Free Item        → 100% off reward      │
└─────────────────────────────────────────┘
```

### **Critical Fields Checklist**

#### **For Buy X Get Y Rules:**
```
✅ Priority: Set lower for better deals
✅ Discount Type: Free Item or Percentage
✅ Is Reward Product: 
   - Buy products: NO
   - Reward products: YES
✅ Stop Further Rules: YES (recommended)
✅ Source Type: Product or Category
```

#### **For Bundle Deals:**
```
✅ Rule Type: Combo Pricing
✅ Discount Type: Fixed Bundle Price
✅ Discount Value: Bundle total price
✅ Is Exclusive: YES (recommended)
✅ Multiple Products: Required
```

#### **For Category Sales:**
```
✅ Rule Type: Product Based
✅ Source Type: Category
✅ Discount Type: Percentage
✅ Discount Scope: Matched Items Only
✅ Min Quantity: 1
```

---

## 🎯 **Is Reward Product Explained**

### **Quick Decision Tree:**
```
Does this product RECEIVE the discount?
│
├─ YES → Check "Is Reward Product = YES"
│        This is the "Get Y" in "Buy X Get Y"
│
└─ NO  → Check "Is Reward Product = NO"
         This is the "Buy X" in "Buy X Get Y"
```

### **Examples:**

**Scenario 1: Same Product**
```
Buy 2 Widgets, Get 1 Widget Free

Widget Configuration 1:
  Is Reward Product: NO → Triggers the offer
  Quantity: 2 units

Widget Configuration 2:
  Is Reward Product: YES → Receives discount
  Quantity: 1 unit free
```

**Scenario 2: Different Products**
```
Buy Phone, Get Case Free

Phone Configuration:
  Is Reward Product: NO → Triggers the offer
  
Case Configuration:
  Is Reward Product: YES → Receives discount (free)
```

---

## 📋 **Source Type Quick Reference**

```
┌──────────────────┬──────────────────────┐
│ Source Type      → Use Case             │
├──────────────────┼──────────────────────┤
│ All Products     → Storewide sales      │
│ Product          → Specific items       │
│ Category         → Department discounts│
│ Manufacturer     → Brand promotions     │
│ Vendor          → Supplier deals       │
└──────────────────┴──────────────────────┘
```

---

## 🎨 **Attribute Selection**

```
┌─────────────────────────────────────────┐
│ Selection Type   → When to Use          │
├─────────────────────────────────────────┤
│ Any              → Most scenarios       │
│ Specific Values  → Variant restrictions │
└─────────────────────────────────────────┘
```

---

## 🔢 **Quantity Fields**

```
Min Quantity:
├─ 1: Default for most promotions
├─ 2+: For "Buy 2 or more" offers
└─ Example: Buy 2 Get 1 Free (Min = 2)

Max Quantity:
├─ 0: Unlimited (recommended)
├─ Set number: Cap discount quantity
└─ Example: Limit to 5 items max
```

---

## 📊 **Priority System**

```
Priority 1: Best deals (100% off)
Priority 2: Good deals (50% off)
Priority 3: Fair deals (20% off)

⚠️ Lower number = Higher priority
⚠️ Processed in priority order
⚠️ Set best deals first
```

---

## 🛡️ **Rule Behavior**

```
Is Exclusive = YES:
├─ Only this rule applies
├─ Other rules ignored
└─ Use for special promotions

Stop Further Rules = YES:
├─ Items won't be used by other rules
├─ Prevents double-discounting
└─ Use for multi-offer scenarios
```

---

## 🎁 **Reward Mode Quick Setup**

### **Same Product Reward:**
```
1. Add product → Is Reward Product: NO
   → This triggers the offer

2. Add same product → Is Reward Product: YES
   → This receives discount

3. Set tier: Buy X, Get Y
```

### **Different Product Reward:**
```
1. Add buy product → Is Reward Product: NO
   → This triggers the offer

2. Add reward product → Is Reward Product: YES
   → This receives discount

3. Set tier: Buy X, Get Y
```

---

## ⚡ **Quick Config Templates**

### **Buy 2 Get 1 Free:**
```
Rule Type: Buy X Get Y
Discount Type: Free Item
Priority: 1
Stop Further Rules: Yes
Products: Category/Product
Buy Products: Is Reward Product = NO
Reward Products: Is Reward Product = YES
Tier: Buy 2, Get 1
```

### **Buy 1 Get 1 Half Price:**
```
Rule Type: Buy X Get Y
Discount Type: Percentage (50%)
Priority: 2
Stop Further Rules: Yes
Products: Category/Product
Buy Products: Is Reward Product = NO
Reward Products: Is Reward Product = YES
Tier: Buy 1, Get 1
```

### **Category Sale (20% Off):**
```
Rule Type: Product Based
Discount Type: Percentage (20%)
Discount Scope: Matched Items Only
Products: Category selection
Is Reward Product: NO
```

### **Bundle Deal:**
```
Rule Type: Combo Pricing
Discount Type: Fixed Bundle Price
Discount Value: Bundle total
Is Exclusive: Yes
Stop Further Rules: Yes
Products: Multiple products required
```

---

## 🔍 **Troubleshooting Quick Guide**

### **Discount Not Applying:**
```
❓ Check: Is rule active?
❓ Check: Products in eligible list?
❓ Check: Min quantity met?
❓ Check: Conditions satisfied?
❓ Check: Priority blocking?
```

### **Wrong Items Discounted:**
```
❓ Check: Priority order?
❓ Check: Is Reward Product setting?
❓ Check: Stop Further Rules setting?
❓ Check: Source type selection?
```

### **Multiple Discounts Issue:**
```
❓ Check: Is Exclusive setting?
❓ Check: Stop Further Rules setting?
❓ Check: Priority conflicts?
❓ Check: Quantity tracking?
```

---

## 📱 **Mobile-Friendly Quick View**

### **Essential Setup Steps:**
```
1️⃣ Choose Rule Type
2️⃣ Set Discount Type & Value
3️⃣ Configure Products (buy + reward)
4️⃣ Set Is Reward Product correctly
5️⃣ Add Tiers (for quantity rules)
6️⃣ Set Priority
7️⃣ Configure behavior flags
8️⃣ Test thoroughly
9️⃣ Activate when ready
```

---

## 💡 **Pro Tips**

### **Best Practices:**
```
✅ Use descriptive names
✅ Set logical priorities
✅ Test before activating
✅ Monitor analytics
✅ Use Stop Further Rules
✅ Set Is Reward Product correctly
✅ Clear cache after changes
✅ Document complex rules
```

### **Common Mistakes:**
```
❌ Forgetting to set Is Reward Product
❌ Wrong priority order
❌ Not testing edge cases
❌ Ignoring profit margins
❌ Too many overlapping rules
❌ Wrong discount scope
❌ Forgetting tier configuration
```

---

## 🆘 **Emergency Quick Fixes**

### **Rule Not Working:**
```
1. Check rule is active
2. Verify products are selected
3. Check min/max quantities
4. Verify tier configuration
5. Clear cache
6. Test with fresh cart
```

### **Discount Calculation Wrong:**
```
1. Verify discount type
2. Check discount value
3. Verify discount scope
4. Check priority order
5. Verify Is Reward Product
6. Test with simple cart
```

---

**Save this for quick reference!** 📌
