# DiscountManagerPlus Easy Test Setup

## 1. One-Time Environment Setup
1. Enable plugin:
`Admin -> Configuration -> Local plugins -> DiscountManagerPlus -> Configure -> Plugin enabled = true`
2. Keep default nop discounts for conflict tests:
create at least one default product discount and one order-level discount.
3. Create roles:
`VIP`, `Wholesale`.
4. Create customers:
`vip@test.com` (VIP role), `regular@test.com` (Registered only).
5. Create categories:
`Shoes`, `Apparel`, `BundleSet`.
6. Create manufacturers:
`Nike`, `Adidas`.
7. Create products with enough stock:
`P1 Shoes Red`, `P2 Shoes Blue`, `P3 Shirt XL`, `P4 Shirt L`, `P5 GiftItem`, `P6 NearExpiry`.
8. Assign product mappings:
`P1,P2 -> Shoes`; `P3,P4 -> Apparel`; set manufacturer as needed.
9. Add attributes:
Color: `Red`,`Blue`; Size: `L`,`XL`.  
Assign to products (`P1 Red`, `P2 Blue`, `P3 XL`, `P4 L`).
10. Payment methods:
enable two methods (`CashOnDelivery`, `CheckMoneyOrder`).
11. Coupon codes:
prepare `ATTR10`, `VIPONLY`, `SOCIAL10`.

## 2. Rule Templates (Create Once, Reuse in Tests)
Use: `Admin -> DiscountManagerPlus -> Promotion Rules -> Add new rule`.

1. `R2_BXGY_Discounted`:
RuleType `BuyXGetY`, tier discount type `%` or fixed, reward product `P2`.
2. `R3_CartQty`:
RuleType `CartCondition`, condition `QuantityMin=5`, discount `%`.
3. `R4_Country`:
Condition `RequiredCountryCodesCsv=US,CA`.
4. `R5_Payment`:
Condition `RequiredPaymentMethodsCsv=Payments.CheckMoneyOrder`.
5. `R6_OrderCount`:
Condition `RequiredOrderCountMin/Max`.
6. `R9_DynamicTier_Product`:
RuleType `ProductBased`, add tiers `1-2=5%`, `3+=10%`.
7. `R10_Social`:
Condition `RequiredCouponCodesCsv=SOCIAL_PROOF`.
8. `R13_Expiry`:
Condition source type `ExpiryDays`, source data with range.
9. `R17_SameLine`:
Enable `RequireSameLineMatch=true`, combine category + attribute.
10. `R18_SetCombo`:
RuleType `ComboPricing`, define set products and tier.
11. `R20_ProgressiveAttr`:
Attribute source + quantity tiers.
12. `R23_AttrCoupon`:
Attribute condition + `RequiredCouponCodesCsv=ATTR10`.
13. `R28_MixMatch`:
ComboPricing with multi-item set and multiplier tier.
14. `R31_RoleAttrBundle`:
Role condition + attribute + bundle product scope.
15. `R32_Flash`:
`IsFlashEnabled=true`, set usage window + caps.

## 3. Fast Execution Steps Per Test Case
For each test:
1. Login as target customer.
2. Add exact products/qty to cart.
3. Apply coupon if required.
4. Select payment method if required.
5. Go cart/checkout and capture line discounts.

## 4. Expected Checks (15 Cases)
1. BOGO discounted applies only to reward line.
2. Cart qty threshold triggers without product-source dependency.
3. Nth order triggers only on configured order index/range.
4. Dynamic tier boundaries choose correct tier.
5. Same-line match rejects split-line false positive.
6. Set/mix-match multiplier scales discount by set count.
7. Progressive attribute discount increases by matched quantity.
8. Coupon + attribute requires both.
9. Country rule fails when country missing and no fallback token.
10. Payment method rule triggers only on selected system name.
11. Social proof token works once (second attempt fails after consume path is wired).
12. Expiry rule discounts only near-expiry items.
13. Flash rule stops after cap reached or outside window.
14. Exclusive rule suppresses all others.
15. Non-exclusive overlap accumulates only on eligible shared lines.

## 5. Stacking Validation
1. Set one rule `IsExclusive=true` and verify only that rule applies.
2. Set `StopFurtherRulesForMatchedLines=true` on one non-exclusive rule and verify only matched lines stop stacking.
3. For linked rules, verify parent discount carry behavior matches `CarryDefaultDiscount`.

## 6. Regression Sanity
1. Existing ProductBased percent/fixed still applies.
2. Existing ComboPricing fixed bundle still applies.
3. Existing BuyXGetY free-item auto-add and selection still works.
4. No unrelated product line receives discount leakage.
