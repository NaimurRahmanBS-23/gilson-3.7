# DiscountManagerPlus: All 32 Rule Configuration Guide

## Scope
This guide is updated for the recent refactor, including new **Product <-> Tier mapping**.

Admin path:  
`Admin -> DiscountManagerPlus -> Promotion Rules -> Add new rule`

## What Changed (Recent Update)
1. `Rule Tier` now has `Applies to rule products` (multi-select).
2. A tier can target specific buy-side rule products.
3. One product can map to multiple tiers; one tier can map to multiple products.
4. If `Applies to rule products` is empty, that tier is global for the rule (backward-compatible behavior).
5. Runtime now scopes ProductBased/ComboPricing/BuyXGetY tier matching to mapped products only.

## Product-Tier Relation
1. Rule products define **what can match** in cart.
2. Tiers define **how much reward/discount** to apply.
3. Mapping binds them: `PromotionRuleProduct <-> PromotionRuleTier`.
4. Reward products are configured as reward rows; tier mapping targets buy-side rule products.

## Common Setup Fields
Use these in all rules:
1. `RuleType`: `ProductBased`, `ComboPricing`, `BuyXGetY`, `CartCondition`, `SubtotalBased`
2. `DiscountType`: `Percentage`, `FixedAmount`, `FixedBundlePrice`, `FreeItem`
3. `DiscountScope`: `MatchedItemsOnly` or `WholeCart`
4. `IsExclusive`: apply only this rule when matched
5. `StopFurtherRulesForMatchedLines`: stop further rule stacking for affected lines
6. `IsFlashEnabled`, `UsageLimitTotal`, `UsageLimitPerCustomer`, `UsageWindowStartUtc`, `UsageWindowEndUtc`
7. Condition fields: role, category, source, coupon, payment, country, order count, same-line, attribute mode

## Tier Mapping Usage Pattern
Use this whenever a rule has multiple products and different tier logic:
1. Add rule products first (Products card).
2. Add tier rows (Tiers card).
3. In each tier popup, set `Applies to rule products`.
4. Leave empty if the tier should apply to all buy products in the rule.

## Rule-by-Rule Setup (1-32)
1. Buy X Get Y Free: `RuleType=BuyXGetY`, add buy row(s), add reward row, tier `DiscountType=FreeItem`, set `RewardQuantity`, optional `AutoAddReward`.
2. Buy X Get Y Discounted: `RuleType=BuyXGetY`, buy row(s)+reward row, tier `DiscountType=Percentage/FixedAmount`; reward line only is discounted.
3. Cart Quantity Based Discounts: `RuleType=CartCondition`, set `QuantityMin/QuantityMax` condition, then add discount/tier.
4. Geo/Country Based Discount: condition `RequiredCountryCodesCsv` (example `US,CA`).
5. Payment Method Discounts: condition `RequiredPaymentMethodsCsv` with payment system names.
6. Customer Order Count Discounts: condition `RequiredOrderCountMin/Max` for `>=N`, `=N`, `N-M`.
7. Role/Customer Group Targeted Discounts: condition `RequiredCustomerRoleId`.
8. Conditional Discounts (AND/OR): multiple conditions, groups, parent condition tree.
9. Advanced Dynamic Pricing Rules: multiple tiers for ProductBased/CartCondition/SubtotalBased.
10. Social Sharing Discounts: configure social proof validation flow and required proof token context.
11. Product Mix and Match Discounts: `RuleType=ComboPricing`, add set products, configure tiers.
12. Category/Brand Combo Discounts: use product source rows by category/manufacturer + combo tiers.
13. Expiry-Sensitive Discounts: condition source `ExpiryDays` with configured range data.
14. Free Gift with Purchase: BuyXGetY with `FreeItem` tier and reward product.
15. Attribute-Based Discount: condition source `ProductAttributeValues` + discount rule.
16. Attribute + Quantity Combo: attribute condition + `QuantityMin/Max`; can use BuyXGetY or ProductBased.
17. Attribute + Category/Manufacturer Combo: combine category/manufacturer + attribute + optional `RequireSameLineMatch=true`.
18. Attribute-Based Mix & Match: ComboPricing with attribute composition conditions.
19. Attribute-Triggered Free Gift: BuyXGetY free reward + attribute condition on qualifying buy lines.
20. Tiered Progressive Attribute Discount: attribute condition + tier brackets by matched quantity.
21. Attribute + Role-Based Discount: role + attribute combined condition.
22. Attribute + Time-Based Discount: `StartDateUtc/EndDateUtc` plus attribute condition.
23. Attribute + Coupon Code Requirement: attribute condition AND `RequiredCouponCodesCsv`.
24. Fixed Bundle Discount: ComboPricing with `FixedAmount` or `FixedBundlePrice`.
25. Percentage Bundle Discount: ComboPricing with `Percentage`.
26. Attribute-Based Bundle Discount: ComboPricing + attribute rules.
27. Category/Manufacturer Bundle Discount: ComboPricing + category/manufacturer source rows.
28. Mix & Match Bundle: ComboPricing with set-count multiplier tiers.
29. Buy X Get Y Bundle: BuyXGetY where X is bundle buy set and Y is reward.
30. Tiered Bundle Discount: ComboPricing with multiple tier ranges.
31. Attribute + Role-Based Bundle Discount: ComboPricing + role + attribute + optional same-line match.
32. Time-Limited/Flash Bundle: ComboPricing or BuyXGetY + flash controls (window + caps).

## Practical Example with New Mapping
Case: same rule has `SOAP-A` and `SHAMPOO-B` as buy products, but different tiers.
1. Add two buy rule products: `SOAP-A`, `SHAMPOO-B`.
2. Tier-1: `Min=2`, `Discount=10%`, `Applies to rule products=[SOAP-A row]`.
3. Tier-2: `Min=3`, `Discount=15%`, `Applies to rule products=[SHAMPOO-B row]`.
4. Result: SOAP quantity triggers Tier-1 only; shampoo quantity triggers Tier-2 only.

## BXGY Examples
1. Buy 2 SOAP-A get 1 SOAP-A free:
   - `RuleType=BuyXGetY`
   - Buy row: SOAP-A, `MinQty=2`
   - Reward row: SOAP-A, `IsRewardProduct=true`
   - Tier: `DiscountType=FreeItem`, `RewardQuantity=1`
2. Buy 2 SOAP-A get 1 SOAP-A at 10%:
   - Same setup, but tier `DiscountType=Percentage`, `DiscountValue=10`
3. Buy 2 SOAP-A get 10% on SHAMPOO-B:
   - Buy row SOAP-A, reward row SHAMPOO-B
   - Tier `Percentage=10`

## Quick Validation Checklist
1. Rule is active and within date/flash window.
2. Products, tiers, and conditions are saved.
3. For mapped tiers, `Applies to rule products` contains expected buy products.
4. Discount affects only eligible lines (no cross-product leakage).
5. `IsExclusive` and `StopFurtherRulesForMatchedLines` behave as expected.
6. If linked, parent nopCommerce discount dates, coupon rules, native limits, and usage history remain authoritative.
