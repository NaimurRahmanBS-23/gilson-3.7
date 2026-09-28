# DiscountManagerPlus Admin User Manual

## 1. Purpose

`NopStation.Plugin.Misc.DiscountManagerPlus` lets admins create advanced automatic promotions on top of nopCommerce discounts.

It supports:

1. Product-based discounts
2. Combo and bundle pricing
3. Buy X Get Y promotions
4. Cart condition rules
5. Subtotal-based rules
6. Reward selection and auto-add
7. Usage history and analytics

**For a comprehensive business-focused guide covering all three main rule types, see `PROFESSIONAL_DOCUMENTATION.md`.**

For a grouped breakdown of features by rule type, see `FEATURE_GROUPS.md`.

---

## 2. Admin Pages

1. Configuration: `/Admin/DiscountManagerPlus/Configure`
2. Rule list: `/Admin/PromotionRule/List`
3. Rule create: `/Admin/PromotionRule/Create`
4. Rule edit: `/Admin/PromotionRule/Edit/{id}`
5. Analytics dashboard: `/Admin/PromotionAnalytics/List`

---

## 3. First-Time Setup

1. Open `Configuration`.
2. Enable `IsEnabled`.
3. Optionally enable:
   1. `EnablePromotionBadge`
   2. `EnableCartSavingsBreakdown`
4. Save.
5. Create the first rule.

---

## 4. Rule Creation Workflow

1. Open `Add new rule`.
2. Fill base info:
   1. Name
   2. System name
   3. Rule type
   4. Discount type
   5. Discount value
   6. Priority
   7. Active / exclusive flags
   8. Date range
   9. Store scope
3. Save once.
4. Configure required sections:
   1. Products
   2. Tiers
   3. Conditions
5. Review setup status and linked discount behavior.
6. Activate the rule.

---

## 5. Rule Type Matrix

1. `ProductBased`
   1. Discount types: `Percentage`, `FixedAmount`, `FreeItem`
   2. Requires: Products
2. `ComboPricing`
   1. Discount types: `Percentage`, `FixedAmount`, `FixedBundlePrice`
   2. Requires: Products
3. `BuyXGetY`
   1. Tier-driven
   2. Requires: Products and tiers
4. `CartCondition`
   1. Discount types: `Percentage`, `FixedAmount`
   2. Requires: Conditions
5. `SubtotalBased`
   1. Discount types: `Percentage`, `FixedAmount`
   2. Requires: Conditions

---

## 6. Core Fields

### Priority

1. Lower number runs earlier.
2. Use unique priorities for deterministic results.

### Exclusive

1. If eligible, this rule prevents other plugin rules from applying.

### Stop further rules for matched lines

1. Stops later plugin rules from discounting the same matched cart lines.

### Carry default discount

1. `Yes`: linked nopCommerce discount stays applied together with the plugin rule.
2. `No`: only plugin logic applies, but parent discount history and limits can still remain authoritative.

### Flash controls

1. Used for standalone plugin rules that need plugin-side usage windows or caps.
2. Linked rules should normally use the parent nopCommerce discount for dates and native limits.

---

## 7. Validation Guardrails

Activation is blocked when setup is incomplete:

1. `BuyXGetY` requires buy products and at least one tier.
2. `ProductBased + FreeItem` requires a reward product.
3. `CartCondition` and `SubtotalBased` require at least one condition.
4. Product, combo, and BOGO rules require products.

---

## 8. Conditions

Conditions can filter by:

1. Product
2. Category
3. Vendor
4. Customer role
5. Country
6. Payment method
7. Paid/completed order count
8. Source values like campaign, referral, device, channel, attributes, or expiry days

Use `Require same-line match` when multiple line-scoped checks must belong to the same cart row.

---

## 9. Usage History and Analytics

### Rule usage history

Shows:

1. Date and time
2. Order
3. Customer
4. Order total
5. Discount applied
6. Usage source

### Linked rule behavior

1. Linked rules can use parent nopCommerce discount history for native limits.
2. Per-rule plugin usage rows are still recorded for rule-level history and analytics.

### Analytics dashboard

Shows:

1. Usage count
2. Total discount
3. Impacted orders and customers
4. Revenue impact
5. Order impact rate

---

## 10. Admin Checklist

1. Plugin enabled
2. Rule active and within valid dates
3. Store scope correct
4. Rule type and discount type valid
5. Required products, tiers, and conditions configured
6. Priority and exclusivity intentional
7. Linked discount settings intentional when parent-first behavior is used

---

## 11. Troubleshooting

### Rule not applying

1. Check configuration is enabled.
2. Check active status, date range, and store scope.
3. Check products, tiers, and conditions are saved.
4. Check quantity thresholds.
5. Check whether an earlier exclusive rule wins first.
6. Check parent discount coupon, limitation, and native requirement state for linked rules.

### Usage history missing

1. Confirm the order really used the rule.
2. Confirm linked rule carry behavior and parent discount relation.
3. Confirm order-placement history writing is enabled through the plugin event flow.

---

## 12. Ready-to-Use Scenarios

### Weekend shoes campaign

1. Rule type: `ProductBased`
2. Scope: Shoes category
3. Discount: `15%`
4. Date range: weekend only
5. Priority: `2`

### Bundle kit pricing

1. Rule type: `ComboPricing`
2. Products: Laptop + Bag + Mouse
3. Discount type: `FixedBundlePrice`
4. Bundle price: `1200`
5. Priority: `3`

### VIP first-order launch

1. Rule type: `CartCondition`
2. Conditions:
   1. Required role `VIP`
   2. First order only
3. Discount: `FixedAmount 25`
4. Priority: `1`

---

## 13. Notes

1. Promotions are auto-applied unless the parent nopCommerce discount requires a coupon.
2. Linked rules should use the parent nopCommerce discount as the source of truth for native limits and usage history.
3. Keep priorities unique for predictable rule execution.
