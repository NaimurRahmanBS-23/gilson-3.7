# DiscountManagerPlus: 32 Rules Click-by-Click Setup (With Sample Values)

Updated: `2026-05-08`

## Runtime Notes (Current Behavior)
1. Cart savings renders inside cart totals (`<table class="cart-total">`) as a promotion savings section.
2. "You saved" uses final applied line-level discounts, not only configured rule value.
3. `BuyXGetY` reward discounts are applied to reward lines only. Buy lines are not discounted by reward-line logic unless the tier reward mode intentionally targets qualified buy lines.
4. `IsExclusive=true` means only that eligible rule applies and other promotion rules are ignored.
5. `StopFurtherRulesForMatchedLines=true` blocks additional promotion-rule discounts only on the matched/discounted lines.
6. `Rule Tier` now supports `Applies to rule products`. Leave it empty to make the tier global for that rule.
7. `BuyXGetY` item-level reward tiers now support reward targeting modes for `FreeItem`, `Percentage`, and `FixedAmount`:
   - `Specific reward product`
   - `Cheapest qualified buy item`
   - `Cheapest item in cart`
8. `RewardQty` means how many reward units get discounted each time the tier is triggered.
9. `CartCondition` tiers use the matched condition quantity. If the rule only uses context checks like role, country, payment method, or order count, total cart quantity is used as the fallback tier metric.

## 0. Shared Test Data (Use for all rules)
Create these once:
1. Categories: `Soap`, `Shampoo`, `Shirt`, `Shoes`, `BundleSet`
2. Manufacturers: `FreshCare`, `Nike`
3. Products:
   - `SOAP-A` (Category: Soap, Manufacturer: FreshCare)
   - `SOAP-B` (Category: Soap, Manufacturer: FreshCare)
   - `SHAMPOO-A` (Category: Shampoo, Manufacturer: FreshCare)
   - `SHIRT-RED-XL` (Category: Shirt)
   - `SHIRT-BLUE-L` (Category: Shirt)
   - `SHOES-BLUE-NIKE` (Category: Shoes, Manufacturer: Nike)
   - `SOCKS-GIFT`
4. Roles: `VIP`, `Wholesale`
5. Coupons: `ATTR10`, `VIP20`, `BUNDLE15`, `SOCIAL10`
6. Payment methods enabled: `Payments.CheckMoneyOrder`, `Payments.CashOnDelivery`

Common path for a new rule:
`Admin -> DiscountManagerPlus -> Promotion Rules -> Add new rule`

Common save flow:
1. Fill the `Info` card.
2. Click `Save`.
3. Open `Products`, `Tiers`, and `Conditions` cards as needed.
4. Configure and save popup rows.

Common verification flow:
1. Add only the needed products for the rule.
2. Confirm only expected line(s) get discount.
3. Confirm unrelated lines keep normal price.
4. Check the `Promotion savings` block in cart totals and verify rule amount + total savings.

---

## 1) Buy X Get Y Free
Sample: Buy 2 `SOAP-A`, get 1 `SOAP-A` free.
1. Rule Info:
   - `RuleType=BuyXGetY`
   - `Name=R1_BOGO_Free`
   - `IsActive=true`
2. Products:
   - Buy row: `SourceType=Product`, `Product=SOAP-A`, `MinQty=2`, `IsReward=false`
   - Reward row: `SourceType=Product`, `Product=SOAP-A`, `MinQty=1`, `IsReward=true`
3. Tier:
   - `MinQty=2`
   - `MaxQty=0`
   - `DiscountType=FreeItem`
   - `RewardQty=1`
   - `Reward mode=Specific reward product`
   - `AutoAddReward=true`
Expected: 1 reward unit is discounted 100%.

## 2) Buy X Get Y Discounted
Sample: Buy 2 `SOAP-A`, get `SHAMPOO-A` 10% off.
1. Rule Info:
   - `RuleType=BuyXGetY`
   - `Name=R2_BXGY_10PCT`
2. Products:
   - Buy: `SOAP-A`, `MinQty=2`, `IsReward=false`
   - Reward: `SHAMPOO-A`, `MinQty=1`, `IsReward=true`
3. Tier:
   - `MinQty=2`
   - `MaxQty=0`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
   - `RewardQty=1`
   - `Reward mode=Specific reward product`
Expected: only the reward line gets the 10% discount.

## 3) Cart Quantity Based
Sample: 5+ total items => 10% off.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
   - `DiscountScope=WholeCart`
2. Conditions:
   - `QuantityMin=5`
Expected: rule triggers from total cart quantity.

## 4) Geo/Country Based
Sample: US/CA customers get 8%.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=8`
2. Condition:
   - `RequiredCountryCodesCsv=US,CA`
Expected: discount applies only for those countries.

## 5) Payment Method Based
Sample: Check/MoneyOrder gets $5 off.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=FixedAmount`
   - `DiscountValue=5`
2. Condition:
   - `RequiredPaymentMethodsCsv=Payments.CheckMoneyOrder`
Expected: applies only when that payment method is selected.

## 6) Order Count Based
Sample: 5th order bonus 15%.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=15`
2. Condition:
   - `RequiredOrderCountMin=5`
   - `RequiredOrderCountMax=5`
Expected: only the customer whose paid/completed order count is exactly 5 gets it.

## 7) Role/Customer Group
Sample: VIP gets 20%.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Condition:
   - `RequiredCustomerRole=VIP`
Expected: only VIP customers are eligible.

## 8) Conditional AND/OR
Sample: (VIP AND SOAP-A in cart) OR first order.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=12`
2. Conditions:
   - Group 1 row 1: `RequiredRole=VIP`
   - Group 1 row 2: `RequiredProduct=SOAP-A`, `LogicalOperator=AND`
   - Group 2 row 1: `IsFirstOrderOnly=true`
Expected: either group can satisfy the rule.

## 9) Dynamic Pricing Tiers
Sample: `SOAP-A` qty 1-2 => 5%, 3+ => 10%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
2. Products:
   - `SOAP-A`, `MinQty=1`
3. Tiers:
   - Tier 1: `Min=1`, `Max=2`, `DiscountType=Percentage`, `DiscountValue=5`
   - Tier 2: `Min=3`, `Max=0`, `DiscountType=Percentage`, `DiscountValue=10`
Expected: the matching quantity bracket decides the discount.

## 10) Social Sharing Discount
Sample: storefront social proof token required.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Condition:
   - `RequiredCouponCodesCsv=SOCIAL_PROOF`
Expected: valid evaluation context token is required.

## 11) Product Mix & Match
Sample: Buy `SOAP-A` + `SHAMPOO-A` => 10%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Products:
   - `SOAP-A`, `MinQty=1`
   - `SHAMPOO-A`, `MinQty=1`
Expected: both products must be present to form the combo set.

## 12) Category/Brand Combo
Sample: Soap category + FreshCare manufacturer => 12%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=12`
2. Products:
   - `SourceType=Category`, `Category=Soap`, `MinQty=1`
   - `SourceType=Manufacturer`, `Manufacturer=FreshCare`, `MinQty=1`
Expected: bundle logic applies when both sources participate.

## 13) Expiry-Sensitive
Sample: near-expiry items (0-7 days) => 20%.
1. Rule Info:
   - `RuleType=CartCondition`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Condition:
   - `ConditionSourceType=ExpiryDays`
   - `ConditionSourceData=0:0-7`
Expected: only near-expiry items qualify.

## 14) Free Gift with Purchase
Sample: buy shampoo, free socks.
1. Rule Info:
   - `RuleType=BuyXGetY`
2. Products:
   - Buy: `SHAMPOO-A`, `MinQty=1`
   - Reward: `SOCKS-GIFT`, `IsReward=true`
3. Tier:
   - `DiscountType=FreeItem`
   - `RewardQty=1`
   - `Reward mode=Specific reward product`
   - `AutoAddReward=true`
Expected: gift is auto-added and fully discounted.

## 15) Attribute-Based
Sample: red shirts 10%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Products:
   - `SourceType=Category`, `Category=Shirt`
3. Condition:
   - `SourceType=ProductAttributeValues`
   - `SourceData=<red option id>`
Expected: only matching attribute lines get discounted.

## 16) Attribute + Quantity
Sample: 3 XL shirts => 1 free socks.
1. Rule Info:
   - `RuleType=BuyXGetY`
2. Products:
   - Buy row for shirt source, `MinQty=3`
   - Reward row `SOCKS-GIFT`
3. Tier:
   - `DiscountType=FreeItem`
   - `RewardQty=1`
4. Condition:
   - `SourceType=ProductAttributeValues` for `Size=XL`
Expected: only XL quantity contributes to the buy threshold.

## 17) Attribute + Category/Manufacturer
Sample: blue Nike shoes 15%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
   - `DiscountValue=15`
2. Products:
   - `SourceType=Manufacturer`, `Manufacturer=Nike`
3. Condition:
   - `RequiredCategory=Shoes`
   - `SourceType=ProductAttributeValues` for blue
   - `RequireSameLineMatch=true`
Expected: all checks must match the same cart line.

## 18) Attribute Mix & Match
Sample: 2 red + 1 blue item => 20%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Products:
   - Add the participating items/source rows
3. Conditions:
   - Attribute source rows for red and blue composition
Expected: discount applies only when the required mix is present.

## 19) Attribute-Triggered Free Gift
Sample: buy XL shirt, free socks.
1. Rule Info:
   - `RuleType=BuyXGetY`
2. Products:
   - Buy shirt row
   - Reward socks row
3. Tier:
   - `DiscountType=FreeItem`
   - `RewardQty=1`
4. Condition:
   - Attribute source for `XL`
Expected: reward appears only when XL items qualify.

## 20) Progressive Attribute Discount
Sample: red items 1-2 => 5%, 3-4 => 10%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
2. Products:
   - relevant category/source row
3. Condition:
   - red attribute source
4. Tiers:
   - `1-2 => 5%`
   - `3-4 => 10%`
Expected: more matching attribute quantity gets the higher tier.

## 21) Attribute + Role-Based
Sample: VIP + size L jackets => 20%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Conditions:
   - `RequiredRole=VIP`
   - attribute source for `L`
Expected: both role and attribute must pass.

## 22) Attribute + Time-Based
Sample: red dresses 15% during campaign dates.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
   - `DiscountValue=15`
   - set `StartDateUtc/EndDateUtc` on the rule or linked parent discount
2. Condition:
   - red attribute source
Expected: rule works only inside the configured date window.

## 23) Attribute + Coupon
Sample: `ATTR10` + red attribute => 10%.
1. Rule Info:
   - `RuleType=ProductBased`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Condition:
   - red attribute source
   - `RequiredCouponCodesCsv=ATTR10`
Expected: both coupon and attribute must match.

## 24) Fixed Bundle Discount
Sample: Soap+Shampoo bundle $5 off.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=FixedAmount`
   - `DiscountValue=5`
2. Products:
   - `SOAP-A`, `MinQty=1`
   - `SHAMPOO-A`, `MinQty=1`
Expected: fixed amount is deducted from the matched bundle set.

## 25) Percentage Bundle Discount
Sample: bundle 15% off.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=15`
2. Products:
   - add the bundle rows
Expected: percentage applies to matched bundle subtotal.

## 26) Attribute-Based Bundle
Sample: XL shirt bundle 10%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Products:
   - shirt bundle rows
3. Condition:
   - attribute source for `XL`
Expected: only attribute-matching bundle sets get discounted.

## 27) Category/Manufacturer Bundle
Sample: Nike shoes + Nike shirt => 20%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Products:
   - category/manufacturer source rows, or explicit product rows if you want tighter scope
Expected: bundle logic works across that category/brand mix.

## 28) Mix & Match Bundle
Sample: choose any 3 from set => 10%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=10`
2. Products:
   - add all allowed items
3. Tiers:
   - add `Min=3`
Expected: any qualifying set of 3 items receives the configured bundle discount.

## 29) Buy X Get Y Bundle
Sample: Laptop+Mouse => Headphone discount/free.
1. Rule Info:
   - `RuleType=BuyXGetY`
2. Products:
   - buy rows for the bundle
   - reward row for the reward product
3. Tier:
   - `DiscountType=FreeItem` or `Percentage`
   - set `RewardQty`
   - set `Reward mode`
Expected: buy bundle unlocks the configured reward logic.

## 30) Tiered Bundle Discount
Sample: 2 items=5%, 3 items=10%, 4 items=15%.
1. Rule Info:
   - `RuleType=ComboPricing`
2. Products:
   - add the bundle set
3. Tiers:
   - `2-2 => 5%`
   - `3-3 => 10%`
   - `4+ => 15%`
Expected: higher bundle size selects the higher tier.

## 31) Attribute + Role Bundle
Sample: VIP + large-size bundle => 20%.
1. Rule Info:
   - `RuleType=ComboPricing`
   - `DiscountType=Percentage`
   - `DiscountValue=20`
2. Conditions:
   - `RequiredRole=VIP`
   - attribute source for `L`
   - `RequireSameLineMatch=true` if the checks must stay on the same line
Expected: role and attribute both gate the bundle discount.

## 32) Flash Bundle
Sample: weekend bundle, cap 100 uses.
1. Rule Info:
   - `RuleType=ComboPricing` or `BuyXGetY`
   - `IsFlashEnabled=true`
   - `UsageLimitTotal=100`
   - `UsageLimitPerCustomer=2`
   - set `UsageWindowStartUtc/UsageWindowEndUtc`
2. Configure products/tiers as normal.
Expected: promotion stops outside the window or after usage caps are reached.

---

## Tier Mapping Example
Case: the same rule has `SOAP-A` and `SHAMPOO-A` as buy products, but different tiers.
1. Add two buy product rows:
   - `SOAP-A`
   - `SHAMPOO-A`
2. Add Tier 1:
   - `Min=2`
   - `Discount=10%`
   - `Applies to rule products=[SOAP-A row]`
3. Add Tier 2:
   - `Min=3`
   - `Discount=15%`
   - `Applies to rule products=[SHAMPOO-A row]`
Expected: SOAP quantity triggers only Tier 1, shampoo quantity triggers only Tier 2.

## Reward Mode Examples
### A) Specific reward product
Sample: Buy 2 `SOAP-A`, get `SHAMPOO-A` 10% off.
1. Add buy row: `SOAP-A`
2. Add reward row: `SHAMPOO-A`, `IsReward=true`
3. Tier:
   - `DiscountType=Percentage`
   - `DiscountValue=10`
   - `RewardQty=1`
   - `Reward mode=Specific reward product`
Expected: only `SHAMPOO-A` can receive the reward discount.

### B) Cheapest qualified buy item
Sample: Buy any 2 soaps, get the cheaper qualified soap at 50% off.
1. Add buy rows for qualifying soaps
2. Do not add a reward product row
3. Tier:
   - `DiscountType=Percentage`
   - `DiscountValue=50`
   - `RewardQty=1`
   - `Reward mode=Cheapest qualified buy item`
Expected: the discount applies to the cheapest qualifying buy-side item.

### C) Cheapest item in cart
Sample: Buy 2 `Asus Laptop`, get the cheapest cart item at 50% off.
1. Add buy row: `Asus Laptop`, `MinQty=2`
2. Do not add a reward product row
3. Tier:
   - `DiscountType=Percentage`
   - `DiscountValue=50`
   - `RewardQty=1`
   - `Reward mode=Cheapest item in cart`
Expected:
   - `Asus x2 + HTC x1` => `HTC` gets 50%
   - `Asus x3 + HTC x1` => `HTC` still gets 50% because it remains the cheapest cart item

## Reward Qty Examples
1. `RewardQty=1`:
   - one reward unit is discounted each time the tier is triggered
2. `RewardQty=2`:
   - two reward units are discounted each time the tier is triggered
3. For repeating/cumulative quantity:
   - qualifying qty `2`, `RewardQty=1` => 1 reward unit
   - qualifying qty `4`, `RewardQty=1` => 2 reward units
   - qualifying qty `6`, `RewardQty=1` => 3 reward units

## Extra: Soap + Shampoo Examples
Case A: Buy 2 Soap get 1 Soap free:
1. `RuleType=BuyXGetY`
2. Buy product `SOAP-A`, `MinQty=2`
3. Reward product `SOAP-A`
4. Tier `FreeItem`, `RewardQty=1`, `Reward mode=Specific reward product`

Case B: Buy 2 Soap get Shampoo 10%:
1. `RuleType=BuyXGetY`
2. Buy product `SOAP-A`, `MinQty=2`
3. Reward product `SHAMPOO-A`
4. Tier `Percentage=10`, `RewardQty=1`, `Reward mode=Specific reward product`

Case C: Buy 2 Soap get cheapest qualifying soap 50%:
1. `RuleType=BuyXGetY`
2. Buy products `SOAP-A`, `SOAP-B`
3. No reward product row
4. Tier `Percentage=50`, `RewardQty=1`, `Reward mode=Cheapest qualified buy item`

Where to view savings:
1. Open cart page.
2. Go to the totals panel.
3. Inside the `cart-total` area, check `Promotion savings`:
   - per-rule rows
   - total savings
   - `View all offers` link
