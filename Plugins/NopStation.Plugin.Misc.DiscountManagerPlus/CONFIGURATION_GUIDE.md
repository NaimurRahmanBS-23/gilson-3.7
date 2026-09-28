# DiscountManagerPlus Configuration Guide

This guide explains how to configure the DiscountManagerPlus plugin for a real store setup.

---

## 1. Enable the Plugin

Location: Admin ? DiscountManagerPlus ? Configuration

1. Set **IsEnabled = true**.
2. Optional:
   - **MaxRuleEvaluationTimeMs**: limit evaluation time (0 = no limit).
   - **EnablePromotionBadge**: show badge on product tiles.
   - **EnableCartSavingsBreakdown**: show savings + reward selection in cart.

Save.

---

## 2. Choose Where You Manage Rules

You have two options:

### Option A: Use **Default Discount Page** (Recommended for simple rules)

Location: Admin ? Promotions ? Discounts ? Edit

- Default discounts already support **Products / Categories / Manufacturers**.
- DiscountManagerPlus will **sync** those assignments into the rule automatically.
- Good for:
  - Simple product/category/manufacturer discounts

### Option B: Use **DiscountManagerPlus Rules Page** (Recommended for advanced rules)

Location: Admin ? DiscountManagerPlus ? Promotion Rules

- Use this for:
  - BOGO (Buy X Get Y)
  - Combo Pricing
  - Cart Condition rules
  - Subtotal-based rules
  - Attribute-based rewards

---

## 3. Rule Types and Required Sections

| Rule Type | Required Sections | What It Does |
|----------|------------------|--------------|
| ProductBased | Products | Discount applies to selected items |
| ComboPricing | Products | Bundle pricing for a set of items |
| BuyXGetY | Products + Tiers | Buy X get Y rewards |
| CartCondition | Conditions | Discount applies to whole cart |
| SubtotalBased | Conditions | Discount applies to whole cart if subtotal matches |

---

## 4. Products Section (ProductBased / Combo / BOGO)

Fields:

1. **Source Type**
   - Product
   - Category
   - Manufacturer
   - Vendor

2. **Source**
   - Product picker or dropdown depending on Source Type

3. **MinQuantity / MaxQuantity**
   - Each line is validated independently
   - MaxQuantity = 0 means unlimited

4. **Is Reward Product**
   - Only for BOGO and FreeItem rules
   - Reward must be Source Type = Product

5. **Reward Attribute Selection**
   - Any: customer selects attributes
   - SpecificValues: admin locks attributes

---

## 5. Tiers Section (BOGO / Combo)

Fields:
- MinQuantity / MaxQuantity
- RewardQuantity
- DiscountType
- DiscountValue
- AutoAddReward
- RewardProduct

BOGO logic:
- Total buy quantity is summed across buy products.
- Tier with matching Min/Max quantity is selected.
- Reward quantity can scale with buy quantity if configured.

---

## 6. Conditions Section (CartCondition / SubtotalBased)

### Condition Groups (AND/OR)
- Rows in the same group use AND/OR.
- Different groups are OR.

Example:
- G1: Shoes AND VIP
- G2: First order
- Result: (Shoes AND VIP) OR (First order)

### Subtotal Conditions
- Operators: GreaterThan, LessThan, Between, EqualTo
- Uses MinValue and MaxValue (Between only)

### Cart Condition Sources
- Products
- Categories
- Manufacturers
- Vendors
- Specification Attribute Options
- Product Attribute Values

Source data format examples:
- `124, 415, 411`
- `124:3-5`
- `124(Color|Size)`
- `124(Color|Size):3-5`

---

## 7. Reward Attribute Selection (Customer Flow)

If the reward product requires attributes:
1. The cart shows a “Choose options” button.
2. Popup opens automatically.
3. Customer selects attributes and clicks Add Reward.
4. Reward item is added and free/discount applied.

If you lock specific attribute values in the rule:
- Reward is auto-added without popup.

---

## 8. Offers Page

- Offers link appears in header if any active rules exist.
- Offers page route: `/promotions/offers`

---

## 9. Best Practice Setup

1. Use default discounts for simple category/product/manufacturer discounts.
2. Use DiscountManagerPlus rules for advanced campaigns:
   - BOGO
   - Combo Pricing
   - Cart Conditions
   - Subtotal ranges
   - Attribute-based rewards

This prevents overlapping discounts and keeps admin UX clean.

---

If you want a customer-facing guide or training material, say the word and I’ll prepare it.
