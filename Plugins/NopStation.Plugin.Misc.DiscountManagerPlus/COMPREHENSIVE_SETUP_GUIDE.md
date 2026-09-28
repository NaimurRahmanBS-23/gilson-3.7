# Complete Setup Guide - All Fields Explained

## 🎯 **Comprehensive Discount ManagerPlus Configuration**

This guide explains **EVERY FIELD** in the DiscountManagerPlus plugin with detailed explanations, examples, and best practices.

---

## 📋 **Table of Contents**
1. [Basic Rule Settings](#basic-rule-settings)
2. [Rule Types Explained](#rule-types-explained)
3. [Discount Types](#discount-types)
4. [Discount Scopes](#discount-scopes)
5. [Products Configuration](#products-configuration)
6. [Tiers Configuration](#tiers-configuration)
7. [Reward Mode & Is Reward Product](#reward-mode--is-reward-product)
8. [Source Types](#source-types)
9. [Attribute Selection](#attribute-selection)
10. [Complete Examples](#complete-examples)

---

## 🚀 **Basic Rule Settings**

### **Information Tab**

#### **Name**
```
Field Type: Text (required)
Description: Human-readable name for the promotion rule
Example: "Buy 2 Get 1 Free - Electronics"
Best Practice: Use descriptive names that indicate the offer type and scope
```

#### **System Name**
```
Field Type: Text (required, auto-generated from Name)
Description: Unique identifier for the rule (auto-generated from Name)
Example: "buy-2-get-1-free-electronics"
Best Practice: Let the system auto-generate this for consistency
```

#### **Rule Type**
```
Field Type: Dropdown (required)
Options:
  1. Product Based - Discounts on specific products
  2. Combo Pricing - Bundle deals with multiple products
  3. Buy X Get Y - Conditional purchase rewards
  4. Cart Condition - Cart-level requirements
  5. Subtotal Based - Cart total triggers
```

#### **Discount Type**
```
Field Type: Dropdown (depends on Rule Type)
Options:
  1. Percentage - % off (e.g., 50% off)
  2. Fixed Amount - $ off (e.g., $10 off)
  3. Fixed Bundle Price - Set total price for bundle
  4. Free Item - 100% off on reward item
```

#### **Discount Scope**
```
Field Type: Dropdown
Options:
  1. Matched Items Only - Discount only applies to eligible products
  2. Whole Cart - Discount applies to entire cart
Best Practice: Use "Matched Items Only" for targeted promotions
```

#### **Discount Value**
```
Field Type: Decimal number
Description: The discount amount/percentage
Examples:
  - Percentage: 50 (for 50% off)
  - Fixed Amount: 10 (for $10 off)
  - Fixed Bundle Price: 49.99 (for $49.99 total)
Best Practice: Always test the final calculation
```

#### **Priority**
```
Field Type: Integer
Description: Processing order (lower = higher priority)
Example: 1 = highest priority, 10 = lowest priority
Best Practice: Set higher discounts to lower priority numbers
```

#### **Is Active**
```
Field Type: Checkbox
Description: Whether the rule is currently enabled
Best Practice: Test thoroughly before activating
```

#### **Is Exclusive**
```
Field Type: Checkbox
Description: Whether this rule prevents other rules from applying
When YES: Only this rule applies to matched items
When NO: Multiple rules can apply (with quantity tracking)
Best Practice: Use YES for special promotions that shouldn't combine
```

#### **Stop Further Rules For Matched Lines**
```
Field Type: Checkbox
Description: Prevents subsequent rules from using already-discounted items
When YES: Items discounted by this rule won't be used by other rules
When NO: Items can still be considered (with quantity tracking)
Best Practice: Use YES for most multi-offer scenarios
```

---

## 📚 **Rule Types Explained**

### **1. Product Based**
```
Description: Apply discounts to specific products
Use Cases:
  - Category-wide sales (e.g., "All shoes 20% off")
  - Product-specific discounts (e.g., "Premium widgets 15% off")
  - Brand promotions (e.g., "Nike products 10% off")

Compatible Discount Types:
  ✅ Percentage
  ✅ Fixed Amount
  ✅ Free Item (for bundles)

Best For: Targeted product promotions
```

### **2. Combo Pricing**
```
Description: Special pricing when customers buy products together
Use Cases:
  - Bundle deals (e.g., "Phone + Case = $99")
  - Product combinations (e.g., "Shirt + Tie = $80")
  - Multi-product packages

Compatible Discount Types:
  ✅ Percentage
  ✅ Fixed Amount
  ✅ Fixed Bundle Price (recommended)

Best For: Product bundles and packages
```

### **3. Buy X Get Y**
```
Description: Conditional rewards based on purchase quantity
Use Cases:
  - Buy 2 Get 1 Free
  - Buy 1 Get 1 Half Price
  - Buy 3 Get 20% Off Any Item

Compatible Discount Types:
  ✅ Percentage
  ✅ Fixed Amount
  ✅ Free Item (recommended)

Best For: Volume-based promotions
```

### **4. Cart Condition**
```
Description: Apply discounts based on cart characteristics
Use Cases:
  - Free shipping over $50
  - 10% off when cart has 5+ items
  - $5 off when buying from multiple categories

Compatible Discount Types:
  ✅ Percentage
  ✅ Fixed Amount

Best For: Cart-level incentives
```

### **5. Subtotal Based**
```
Description: Trigger discounts based on cart total
Use Cases:
  - Spend $100, get $10 off
  - 15% off orders over $200
  - Free gift with $150 purchase

Compatible Discount Types:
  ✅ Percentage
  ✅ Fixed Amount

Best For: Tiered spending promotions
```

---

## 💰 **Discount Types**

### **1. Percentage**
```
Description: Discount calculated as percentage of price
Formula: Discount = Price × (DiscountValue / 100)

Examples:
  - 50% off on $100 item = $50 discount
  - 25% off on $80 item = $20 discount
  - 10% off on $50 item = $5 discount

Best For:
  ✅ Sales promotions
  ✅ Category discounts
  ✅ Volume discounts

Not For:
  ❌ Fixed-price bundles (use Fixed Bundle Price)
```

### **2. Fixed Amount**
```
Description: Fixed dollar amount discount
Formula: Discount = DiscountValue (up to item price)

Examples:
  - $10 off on $50 item = $10 discount
  - $5 off on $20 item = $5 discount
  - $3 off on $2 item = $2 discount (can't exceed price)

Best For:
  ✅ Cash discounts
  ✅ Coupon codes
  ✅ Fixed savings offers

Not For:
  ❌ Percentage-based sales (use Percentage)
```

### **3. Fixed Bundle Price**
```
Description: Set total price for multiple items together
Formula: Total Bundle Price = DiscountValue

Examples:
  - Phone ($100) + Case ($20) = $80 bundle (save $40)
  - Shirt ($30) + Tie ($25) + Belt ($20) = $60 (save $15)

Best For:
  ✅ Product bundles
  ✅ Package deals
  ✅ Combo pricing

Configuration Tips:
  ⚠️ Only works with Combo Pricing rule type
  ⚠️ Must have multiple products in the bundle
  ⚠️ Customer sees the bundle price as final total
```

### **4. Free Item**
```
Description: 100% discount on reward item
Formula: Discount = RewardItem Price × Reward Quantity

Examples:
  - Buy 2 Get 1 Free: Get 1 item at 100% off
  - Buy 1 Get 1 Half Price: Reward item at 50% off (use percentage)
  - Buy 3 Get Any Item Free: Reward item at 100% off

Best For:
  ✅ BOGO deals
  ✅ Free gift with purchase
  ✅ Loyalty rewards

Configuration:
  ⚠️ Requires setting up reward products
  ⚠️ Works with Buy X Get Y and Product Based rules
```

---

## 🎯 **Discount Scopes**

### **1. Matched Items Only**
```
Description: Discount applies only to eligible products

When to Use:
  ✅ Product-specific promotions
  ✅ Category discounts
  ✅ Brand promotions
  ✅ Most targeted offers

Example:
  Rule: "20% off all Electronics"
  Cart: Laptop ($500) + Shoes ($100)
  Discount: $100 (20% of $500, only Electronics)
  Final: $400 (Laptop) + $100 (Shoes) = $500

Best Practice: Default choice for most promotions
```

### **2. Whole Cart**
```
Description: Discount applies to entire cart subtotal

When to Use:
  ✅ Site-wide sales
  ✅ Shipping discounts
  ✅ Cart-level incentives
  ✅ Holiday promotions

Example:
  Rule: "10% off entire order"
  Cart: Laptop ($500) + Shoes ($100)
  Discount: $60 (10% of $600, entire cart)
  Final: $540 total

Best Practice: Use sparingly for special occasions
```

---

## 🛍️ **Products Configuration**

### **Products Tab Fields**

#### **Source Type**
```
Field Type: Dropdown
Options:
  1. All Products - Apply to all products in store
  2. Product - Specific products only
  3. Category - All products in selected category
  4. Manufacturer - All products from selected manufacturer
  5. Vendor - All products from selected vendor

Usage Examples:
  All Products: Storewide sale
  Product: Specific item promotion
  Category: "All shoes 20% off"
  Manufacturer: "All Nike products 15% off"
  Vendor: "All Acme Corp products 10% off"
```

#### **Product Selection (When Source Type = Product)**
```
Field Type: Multi-select dropdown
Description: Choose specific products for the promotion

Best Practices:
  ✅ Start with a few products to test
  ✅ Use product categories for broader scope
  ✅ Consider inventory levels
  ✅ Group related products together

Example:
  Rule: "Buy 2 Get 1 Free"
  Selected Products:
    - Product A (Widget)
    - Product B (Gadget)
    - Product C (Tool)
```

#### **Category Selection (When Source Type = Category)**
```
Field Type: Dropdown
Description: Choose a category for the promotion

Best Practices:
  ✅ Use main categories for broad promotions
  ✅ Use subcategories for targeted offers
  ✅ Consider category hierarchy

Example:
  Rule: "20% off Electronics"
  Selected Category: Electronics > Computers > Laptops
```

#### **Manufacturer Selection (When Source Type = Manufacturer)**
```
Field Type: Dropdown
Description: Choose a manufacturer for brand-specific promotions

Example:
  Rule: "15% off all Sony products"
  Selected Manufacturer: Sony
```

#### **Vendor Selection (When Source Type = Vendor)**
```
Field Type: Dropdown
Description: Choose a vendor for vendor-specific promotions

Example:
  Rule: "10% off all Acme Corp products"
  Selected Vendor: Acme Corporation
```

#### **Min Quantity**
```
Field Type: Integer
Description: Minimum quantity required to qualify

Examples:
  - 1: Need at least 1 item
  - 2: Need at least 2 items
  - 5: Need at least 5 items

Best Practices:
  ✅ Set to 1 for most promotions
  ✅ Use higher values for bulk discounts
  ✅ Consider average order quantities

Example:
  Rule: "Buy 2 Get 1 Free"
  Min Quantity: 2
```

#### **Max Quantity**
```
Field Type: Integer
Description: Maximum quantity to discount (0 = unlimited)

Examples:
  - 0: No limit
  - 5: Discount up to 5 items only
  - 10: Discount up to 10 items only

Best Practices:
  ✅ Use 0 for most promotions
  ✅ Set limits for loss prevention
  ✅ Consider profit margins

Example:
  Rule: "Buy 2 Get 1 Free (max 3 free items)"
  Max Quantity: 0 (unlimited buy items)
  Tier Reward Quantity: 1 (free items)
```

---

## 🎁 **Reward Mode & Is Reward Product**

### **Understanding Reward Products**

Reward products are the items that **receive the discount** in Buy X Get Y scenarios. They are separate from the "buy" products that trigger the promotion.

### **Is Reward Product Field**

```
Field Type: Checkbox
Description: Marks this product as a reward item (receives discount)

When to Check YES:
  ✅ This product receives the discount
  ✅ This is the "Get Y" in "Buy X Get Y"
  ✅ Different from the triggering products

When to Check NO:
  ✅ This product triggers the promotion
  ✅ This is the "Buy X" in "Buy X Get Y"
  ✅ Regular eligible products

Critical Concept:
  Buy Products (Is Reward Product = NO) → Trigger the offer
  Reward Products (Is Reward Product = YES) → Receive the discount
```

### **Reward Mode Scenarios**

#### **Scenario 1: Same Product Reward**
```
Setup: "Buy 2 Get 1 Free" on same product
Example: Buy 2 widgets, get 1 widget free

Configuration:
  Product: Widget X
  Is Reward Product: NO (buy products)
  Min Quantity: 2
  Max Quantity: 0

  Product: Widget X
  Is Reward Product: YES (reward product)
  Tier: Buy 2, Get 1

Customer Cart:
  Widget X × 3 units

Result:
  - 2 units: Paid at full price (trigger)
  - 1 unit: FREE (reward)
```

#### **Scenario 2: Different Product Reward**
```
Setup: "Buy Phone, Get Case Free"
Example: Buy a phone, get a phone case free

Configuration:
  Product: Smartphone
  Is Reward Product: NO (buy products)
  Min Quantity: 1

  Product: Phone Case
  Is Reward Product: YES (reward product)
  Tier: Buy 1, Get 1

Customer Cart:
  Smartphone × 1 unit
  Phone Case × 1 unit

Result:
  - Smartphone: Paid at full price
  - Phone Case: FREE (reward)
```

#### **Scenario 3: Multiple Reward Options**
```
Setup: "Buy 2 Electronics, Get Any Accessory Half Price"
Example: Buy any 2 electronics, get 50% off any accessory

Configuration:
  Source Type: Category
  Category: Electronics
  Is Reward Product: NO (buy products)
  Min Quantity: 2

  Source Type: Category
  Category: Accessories
  Is Reward Product: YES (reward product)
  Discount Type: Percentage
  Discount Value: 50%
  Tier: Buy 2, Get 1

Customer Cart:
  Smartphone × 1
  Tablet × 1
  Phone Case × 1

Result:
  - Smartphone: Paid at full price (trigger)
  - Tablet: Paid at full price (trigger)
  - Phone Case: 50% OFF (reward)
```

---

## 📋 **Source Types Explained**

### **1. All Products**
```
Source Type: All Products
Description: Promotion applies to entire catalog

Use Cases:
  ✅ Storewide sales
  ✅ Holiday promotions
  ✅ Anniversary sales

Configuration:
  Source Type: All Products
  (No additional product selection needed)

Example:
  Rule: "Storewide 20% Off"
  Source Type: All Products
  Discount Type: Percentage (20%)
  Result: All products get 20% discount
```

### **2. Product (Specific)**
```
Source Type: Product
Description: Promotion applies to selected products only

Use Cases:
  ✅ Product-specific promotions
  ✅ Targeted discounts
  ✅ Inventory clearance

Configuration:
  Source Type: Product
  Products: [Select specific products]
  Min Quantity: 1
  Max Quantity: 0

Example:
  Rule: "Premium Widget Special"
  Source Type: Product
  Selected Products: Widget A, Widget B, Widget C
  Discount: 15% off selected products
```

### **3. Category**
```
Source Type: Category
Description: Promotion applies to all products in category

Use Cases:
  ✅ Category-wide sales
  ✅ Seasonal promotions
  ✅ Department discounts

Configuration:
  Source Type: Category
  Category: Electronics > Computers
  Min Quantity: 1

Example:
  Rule: "Computer Accessories 25% Off"
  Source Type: Category
  Category: Computer Accessories
  Discount: 25% off all accessories
```

### **4. Manufacturer**
```
Source Type: Manufacturer
Description: Promotion applies to all products from manufacturer

Use Cases:
  ✅ Brand-specific promotions
  ✅ Manufacturer deals
  ✅ Vendor partnerships

Configuration:
  Source Type: Manufacturer
  Manufacturer: Nike
  Min Quantity: 1

Example:
  Rule: "Nike Week - 20% Off All Nike Products"
  Source Type: Manufacturer
  Manufacturer: Nike
  Discount: 20% off all Nike items
```

### **5. Vendor**
```
Source Type: Vendor
Description: Promotion applies to all products from vendor

Use Cases:
  ✅ Vendor-specific promotions
  ✅ Supplier partnerships
  ✅ Drop-shipping deals

Configuration:
  Source Type: Vendor
  Vendor: Acme Corporation
  Min Quantity: 1

Example:
  Rule: "Acme Corp Special - 10% Off"
  Source Type: Vendor
  Vendor: Acme Corporation
  Discount: 10% off all Acme products
```

---

## 🎨 **Attribute Selection**

### **Reward Attribute Selection Type**

```
Field Type: Dropdown
Options:
  1. Any - Any product variant qualifies
  2. Specific Values - Only selected variants qualify

When to Use "Any":
  ✅ Most common scenarios
  ✅ Simple reward selection
  ✅ Faster checkout experience

When to Use "Specific Values":
  ✅ Color/size specific rewards
  ✅ Attribute-based restrictions
  ✅ Variant-level control
```

### **Reward Attribute Value IDs**

```
Field Type: Multi-select (when Specific Values selected)
Description: Choose specific product attributes for rewards

Example Use Case:
  Rule: "Buy Shirt, Get Any Red Accessory Free"
  Reward Product: Accessories category
  Attribute Selection: Specific Values
  Selected Attribute: Color = Red
  Result: Only red accessories qualify as free reward

Configuration:
  1. Set Reward Attribute Selection Type = "Specific Values"
  2. Select attribute values (e.g., Color: Red, Size: Large)
  3. Only products with these attributes qualify as rewards
```

---

## 📊 **Tiers Configuration**

### **Understanding Tiers**

Tiers allow you to create progressive discount levels based on purchase quantity. Each tier represents a different reward level.

### **Tier Fields**

#### **Buy Quantity**
```
Field Type: Integer (required)
Description: Minimum quantity to trigger this tier

Examples:
  - Buy 2: Need 2 items to trigger
  - Buy 3: Need 3 items to trigger
  - Buy 5: Need 5 items to trigger

Best Practice:
  Start low for accessibility, create multiple tiers for progression
```

#### **Reward Quantity**
```
Field Type: Integer (required)
Description: How many items receive the discount

Examples:
  - Get 1: 1 item discounted
  - Get 2: 2 items discounted
  - Get 3: 3 items discounted

Best Practice:
  Match reward to buy quantity for best value
```

#### **Discount Value**
```
Field Type: Decimal (depends on discount type)
Description: The discount amount/percentage for this tier

Examples:
  Percentage: 50 (50% off)
  Fixed Amount: 10 ($10 off)
  Free Item: 0 (100% off)

Best Practice:
  Offer better deals for higher tiers
```

#### **Reward Product ID**
```
Field Type: Product selector (optional)
Description: Specific product to receive discount (for Free Item)

When to Use:
  ✅ Free gift with purchase
  ✅ Specific reward product
  ✅ Branded promotional items

When to Leave Blank:
  ✅ Any qualifying product can be reward
  ✅ Flexible reward selection
```

### **Multiple Tier Examples**

#### **Example 1: Progressive Quantity Discount**
```
Rule: "Buy More, Save More"

Tier 1:
  Buy 2, Get 1 (50% off)
  Customer buys 2 items → Gets 1 at 50% off

Tier 2:
  Buy 3, Get 1 (50% off)
  Customer buys 3 items → Gets 1 at 50% off

Tier 3:
  Buy 5, Get 2 (50% off)
  Customer buys 5 items → Gets 2 at 50% off

Best For: Volume discounts
```

#### **Example 2: Escalating Percentage**
```
Rule: "Percentage Rewards"

Tier 1:
  Buy 2, Get 1 (20% off)
  Customer buys 2 items → Gets 1 at 20% off

Tier 2:
  Buy 3, Get 1 (30% off)
  Customer buys 3 items → Gets 1 at 30% off

Tier 3:
  Buy 5, Get 1 (50% off)
  Customer buys 5 items → Gets 1 at 50% off

Best For: Tiered percentage rewards
```

#### **Example 3: Free Gifts**
```
Rule: "Free Gift with Purchase"

Tier 1:
  Buy $50, Get Free Gift Card ($10)
  Customer spends $50 → Gets $10 gift card free

Tier 2:
  Buy $100, Get Free Gift Card ($25)
  Customer spends $100 → Gets $25 gift card free

Tier 3:
  Buy $200, Get Free Gift Card ($50)
  Customer spends $200 → Gets $50 gift card free

Best For: Gift with purchase promotions
```

---

## 🎯 **Complete Configuration Examples**

### **Example 1: Basic Buy 2 Get 1 Free**

**Rule Info Tab:**
```
Name: Buy 2 Get 1 Free - Electronics
System Name: buy-2-get-1-free-electronics
Rule Type: Buy X Get Y
Discount Type: Free Item
Discount Scope: Matched Items Only
Priority: 1
Is Active: Yes
Is Exclusive: No
Stop Further Rules: No
```

**Products Tab:**
```
Source Type: Category
Category: Electronics
Is Reward Product: NO
Min Quantity: 2
Max Quantity: 0
```

**Tiers Tab:**
```
Tier 1:
  Buy Quantity: 2
  Reward Quantity: 1
  (No reward product needed - any qualifying item)
```

**Test Scenario:**
```
Customer Cart:
  - Smartphone × 2 units ($200 each = $400)
  - Tablet × 1 unit ($300)

Calculation:
  - Buy 2 items (Smartphone): Qualifies
  - Get 1 free: Cheapest eligible item (Tablet $300)
  - Final: $400 (smartphones) + $0 (free tablet) = $400
```

---

### **Example 2: Buy 1 Get 1 Half Price**

**Rule Info Tab:**
```
Name: Buy 1 Get 1 Half Price - Clothing
System Name: buy-1-get-1-half-price-clothing
Rule Type: Buy X Get Y
Discount Type: Percentage
Discount Value: 50
Discount Scope: Matched Items Only
Priority: 2
Is Active: Yes
Is Exclusive: No
Stop Further Rules: No
```

**Products Tab:**
```
Source Type: Category
Category: Clothing
Is Reward Product: NO
Min Quantity: 1
Max Quantity: 0
```

**Tiers Tab:**
```
Tier 1:
  Buy Quantity: 1
  Reward Quantity: 1
  (50% discount applied automatically)
```

**Test Scenario:**
```
Customer Cart:
  - Shirt × 2 units ($30 each = $60)

Calculation:
  - Buy 1 item: Qualifies
  - Get 1 half price: Second shirt at 50% off
  - Discount: $30 × 50% = $15
  - Final: $30 (full price) + $15 (50% off) = $45
```

---

### **Example 3: Combo Pricing - Fixed Bundle Price**

**Rule Info Tab:**
```
Name: Phone + Case Bundle
System Name: phone-case-bundle
Rule Type: Combo Pricing
Discount Type: Fixed Bundle Price
Discount Value: 89.99
Discount Scope: Matched Items Only
Priority: 1
Is Active: Yes
Is Exclusive: Yes
Stop Further Rules: Yes
```

**Products Tab:**
```
Source Type: Product
Selected Products:
  - Smartphone X (regular price: $100)
  - Phone Case Y (regular price: $20)
Is Reward Product: NO
Min Quantity: 1 (each)
Max Quantity: 0
```

**Test Scenario:**
```
Customer Cart:
  - Smartphone X × 1 unit ($100)
  - Phone Case Y × 1 unit ($20)

Calculation:
  - Regular total: $120
  - Bundle price: $89.99
  - Savings: $30.01
  - Final: $89.99 (fixed bundle price)
```

---

### **Example 4: Category-Wide Percentage Discount**

**Rule Info Tab:**
```
Name: Summer Sale - 20% Off All Shoes
System Name: summer-sale-20-shoes
Rule Type: Product Based
Discount Type: Percentage
Discount Value: 20
Discount Scope: Matched Items Only
Priority: 5
Is Active: Yes
Is Exclusive: No
Stop Further Rules: No
```

**Products Tab:**
```
Source Type: Category
Category: Shoes > All Shoes
Is Reward Product: NO (not applicable)
Min Quantity: 1
Max Quantity: 0
```

**Test Scenario:**
```
Customer Cart:
  - Running Shoes ($80)
  - Dress Shoes ($120)
  - Sandals ($40)
  - Watch ($200) - not in shoes category

Calculation:
  - Running Shoes: $80 - 20% = $64
  - Dress Shoes: $120 - 20% = $96
  - Sandals: $40 - 20% = $32
  - Watch: $200 (no discount - not shoes)
  - Final: $292 (saved $48)
```

---

### **Example 5: Subtotal-Based Tiered Discount**

**Rule Info Tab:**
```
Name: Spend More, Save More
System Name: spend-more-save-more
Rule Type: Subtotal Based
Discount Type: Percentage
Discount Value: (varies by tier)
Discount Scope: Whole Cart
Priority: 10
Is Active: Yes
Is Exclusive: No
Stop Further Rules: No
```

**Tiers Tab:**
```
Tier 1:
  Buy Quantity: 50 (cart subtotal)
  Discount Value: 10 (10% off)

Tier 2:
  Buy Quantity: 100 (cart subtotal)
  Discount Value: 15 (15% off)

Tier 3:
  Buy Quantity: 200 (cart subtotal)
  Discount Value: 20 (20% off)
```

**Test Scenario:**
```
Customer Cart Total: $150

Calculation:
  - Qualifies for Tier 2 ($100 threshold)
  - Discount: 15% of $150 = $22.50
  - Final: $150 - $22.50 = $127.50
```

---

## 🔧 **Advanced Configuration**

### **Multiple Rules with Priority**

**Rule 1 (Highest Priority):**
```
Name: Buy 2 Get 1 Free
Priority: 1
Discount: 100% off (1 item)
Products: Electronics
```

**Rule 2 (Medium Priority):**
```
Name: Buy 1 Get 1 Half Price
Priority: 2
Discount: 50% off (1 item)
Products: Electronics
```

**Rule 3 (Lowest Priority):**
```
Name: 20% Off All Electronics
Priority: 3
Discount: 20% off
Products: Electronics
```

**Processing Order:**
1. System processes Rule 1 first (gives 1 free item)
2. System processes Rule 2 second (gives 1 item at 50% off)
3. System processes Rule 3 third (gives 20% off remaining items)

**Result:** Maximum customer savings with automatic cheapest-item selection!

---

## ⚠️ **Important Configuration Tips**

### **Do's:**
```
✅ Use clear, descriptive rule names
✅ Set appropriate priorities (better deals first)
✅ Test thoroughly before activation
✅ Use "Stop Further Rules" for multi-offer scenarios
✅ Set Min Quantity = 1 for most promotions
✅ Leave Max Quantity = 0 for unlimited
✅ Use "Matched Items Only" for targeted offers
✅ Monitor performance analytics
✅ Create attention messages for customer guidance
```

### **Don'ts:**
```
❌ Don't create overlapping exclusive rules
❌ Don't forget to set Is Reward Product correctly
❌ Don't set unrealistic discount values
❌ Don't ignore profit margins
❌ Don't forget to test edge cases
❌ Don't use "Whole Cart" scope unnecessarily
❌ Don't create too many similar rules
❌ Don't forget to set priorities
```

---

## 🎓 **Quick Reference**

### **Common Rule Combinations:**

```
Buy X Get Y Free:
  Rule Type: Buy X Get Y
  Discount Type: Free Item
  Is Reward Product: Mixed (buy + reward)
  Stop Further Rules: Yes

Buy X Get Y Half Price:
  Rule Type: Buy X Get Y
  Discount Type: Percentage (50%)
  Is Reward Product: Mixed (buy + reward)
  Stop Further Rules: Yes

Bundle Deal:
  Rule Type: Combo Pricing
  Discount Type: Fixed Bundle Price
  Is Reward Product: NO
  Stop Further Rules: Yes

Category Sale:
  Rule Type: Product Based
  Discount Type: Percentage
  Source Type: Category
  Stop Further Rules: No

Storewide Sale:
  Rule Type: Product Based
  Discount Type: Percentage
  Source Type: All Products
  Discount Scope: Whole Cart
```

---

## 📞 **Need More Help?**

For additional assistance:
- Review test scenarios in this guide
- Check the analytics for rule performance
- Test configurations in development first
- Monitor customer feedback
- Adjust based on data

**Happy Configuring!** 🚀
