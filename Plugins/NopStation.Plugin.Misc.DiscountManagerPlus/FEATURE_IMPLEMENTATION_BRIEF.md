# DiscountManagerPlus Partial+No Feature Implementation Brief

## Implemented Core Runtime Refactor
- Added `PromotionEvaluationContext` and context-first overloads:
  - `EvaluateCartAsync(PromotionEvaluationContext context)`
  - `BuildLineDiscountMapAsync(PromotionEvaluationContext context)`
- Kept backward-compatible overloads and delegated legacy signatures to context-based flow.
- Added strict line-level discount handling support through `AppliedPromotion.LineDiscounts`.

## New Rule/Condition Capabilities
- `PromotionRule`:
  - `StopFurtherRulesForMatchedLines`
  - `UsageLimitTotal`
  - `UsageLimitPerCustomer`
  - `UsageWindowStartUtc`
  - `UsageWindowEndUtc`
  - `IsFlashEnabled`
- `PromotionRuleCondition`:
  - `RequiredCountryCodesCsv`
  - `RequiredPaymentMethodsCsv`
  - `RequiredCouponCodesCsv`
  - `RequiredOrderCountMin`
  - `RequiredOrderCountMax`
  - `RequireSameLineMatch`
  - `AttributeMatchModeId` (`Any`/`All`)
- Added `PromotionSocialShareEvent` entity for social-proof token validation.

## Product-Based and Bundle Behavior
- Buy X Get Y discounted now allocates discounts only on reward lines.
- Reward-line discounts support percentage/fixed tier discounts using exact line allocations.
- Tier evaluation enabled for:
  - `ProductBased`
  - `CartCondition`
  - `SubtotalBased`
  - existing `ComboPricing` and `BuyXGetY`
- Same-line condition mode ensures item-bound checks match on one cart line.

## Context-Driven Conditions
- Coupon, payment method, country, order-count conditions evaluated from `PromotionEvaluationContext`.
- Global cart-quantity condition works independently from source matching.
- Expiry-sensitive source added via `ConditionSourceType.ExpiryDays`.
- Optional social-proof gating via `SOCIAL_PROOF` marker in required coupon CSV.

## Flash Controls
- Added flash usage window + usage caps at rule level.
- Rule eligibility blocks when flash window/cap conditions are exceeded.

## Admin Setup (New Fields)
- Rule form:
  - stop further rules for matched lines
  - flash enabled + usage caps + usage window
- Condition popup:
  - country/payment/coupon/order-count
  - same-line toggle
  - attribute match mode

## Migration Summary
- Added migration: `RuleAndConditionAdvancedControlsMigration`
- Added columns for rule/condition advanced controls.
- Added social-share event table creation.
