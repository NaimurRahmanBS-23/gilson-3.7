# NopStation DiscountManagerPlus Documentation

## Documentation Index

- **Professional Guide:** `PROFESSIONAL_DOCUMENTATION.md` ← Start here for business users
- Admin manual: `ADMIN_USER_MANUAL.md`
- Feature groups: `FEATURE_GROUPS.md`
- Customer guide: `CUSTOMER_GUIDE.md`
- Technical context: `context.md`
- Discount integration plan: `DEFAULT_DISCOUNT_INTEGRATION_PLAN.md`
- Discount integration context: `DEFAULT_DISCOUNT_INTEGRATION_CONTEXT.md`
- Product backlog and user stories: `PRODUCT_BACKLOG_AND_USER_STORIES.md`
- BRD: `DISCOUNT_MANAGER_PLUS_BRD.md`
- Feature comparison: `FEATURE_COMPARISON.md`
- Agent instructions: `AGENTS.md`
- Agent templates: `CommonStructure&AgentPromptTemplates.md`

## Overview

`NopStation.Plugin.Misc.DiscountManagerPlus` provides rule-based promotions for nopCommerce.
It evaluates active promotion rules and applies discounts directly to cart item unit prices.

## Core Features

- Promotion rule groups:
  - Product based
  - Combo pricing
  - Buy X Get Y (BOGO and tiered rewards)
  - Cart condition
  - Subtotal based
- Reward and selection:
  - Reward product by product or category
  - Manual reward selection
  - Reward attributes selection
  - Auto-add rewards
- Storefront experience:
  - Promotion badge on product cards
  - Offers page: `/promotions/offers`
  - Cart reward selection modal
- Admin and governance:
  - Rule priority and exclusivity
  - Validation guardrails
  - Usage history and analytics

## Admin: Where to Manage

- Configuration: `/Admin/DiscountManagerPlus/Configure`
- Promotion rules list: `/Admin/PromotionRule/List`
- Create rule: `/Admin/PromotionRule/Create`
- Edit rule: `/Admin/PromotionRule/Edit/{id}`

## Configuration (Admin)

Go to `Admin > NopStation > DiscountManagerPlus > Configuration`

Fields:

- `IsEnabled`: enables/disables all promotion processing
- `MaxRuleEvaluationTimeMs`: max time budget for evaluating rules
- `EnablePromotionBadge`: shows badge on product listing/details
- `EnableCartSavingsBreakdown`: shows savings block in cart

## Rule Setup Flow (Admin)

1. Create a rule with name and system name.
2. Select `Rule type`.
3. Select valid `Discount type` (depends on rule type).
4. Set `Discount value` if applicable.
5. Save once.
6. Add Products / Tiers / Conditions from popup sections.
7. Test with storefront cart.

## Rule Type vs Discount Type Matrix

- `ProductBased` -> `Percentage`, `FixedAmount`, `FreeItem`
- `ComboPricing` -> `Percentage`, `FixedAmount`, `FixedBundlePrice`
- `BuyXGetY` -> Tier-based (main rule discount fields are not used)
- `CartCondition` -> `Percentage`, `FixedAmount`
- `SubtotalBased` -> `Percentage`, `FixedAmount`

Invalid combinations are blocked by validation.

## Rule Types Explained

### 1) ProductBased

Use this when discount depends on specific product quantities.

- Configure required products in **Products**
- Use `MinQuantity` per product row
- For `FreeItem`, add reward product row with `IsRewardProduct = true`

### 2) ComboPricing

Use this for bundle-style offers.

- Add all required combo products with `MinQuantity`
- Engine calculates complete set count from cart quantities
- Discount behavior:
  - `FixedBundlePrice`: compares set subtotal vs bundle price
  - `Percentage`: applies % to matched set subtotal
  - `FixedAmount`: applies fixed discount per set

### 3) BuyXGetY

Use tier-driven reward logic.

- Configure buy/reward products
- Configure tiers (`MinQuantity`, `MaxQuantity`, discount/reward settings)
- Optional: auto-add reward product to cart

### 4) CartCondition

Use product presence conditions in cart.

- Required product: must be in cart
- Excluded product: must not be in cart

### 5) SubtotalBased

Use subtotal threshold/range checks.

Operators:

- `GreaterThan`
- `LessThan`
- `Between`
- `EqualTo`

## Product Popup Fields

- `ProductId`: target product
- `MinQuantity`: required quantity for that product row
- `IsRewardProduct`:
  - used for `BuyXGetY`
  - used for `ProductBased + FreeItem`
  - not used for `ComboPricing`

## Customer Experience

- Customers can open `/promotions/offers` to see active offers.
- A header link "View all offers" appears when active offers exist.
- Product badge indicates potential promotions.
- Cart shows per-rule savings and total savings when enabled.

## Examples

### Example A: Combo Pricing

- Rule type: `ComboPricing`
- Products: A (min 1), B (min 1)
- Discount type: `FixedBundlePrice`
- Discount value: `100`

If A+B regular total is 140, discount for one set is 40.
If cart has 2 complete sets, discount applies per set.

### Example B: Product Based Free Item

- Rule type: `ProductBased`
- Discount type: `FreeItem`
- Products:
  - Buy product X (min 2, `IsRewardProduct = false`)
  - Reward product Y (`IsRewardProduct = true`)

When cart has 2x X, discount equals reward item value.

### Example C: Subtotal Based

- Rule type: `SubtotalBased`
- Discount type: `Percentage`
- Condition: `GreaterThan 500`
- Discount value: `10`

Cart subtotal above 500 gets 10% discount.

## Troubleshooting

If discount is not applying, check:

1. Plugin is enabled.
2. Rule is active.
3. Date range is valid.
4. Store scope matches current store.
5. Rule type + discount type is valid.
6. Required products and quantities are satisfied.
7. Reward product is configured for `FreeItem`.
8. BuyXGetY tiers are configured.
9. Exclusive rule earlier in priority is stopping further rules.

## Technical Notes

- Cart integration: `GetShoppingCartItemUnitPriceEvent`
- Order usage logging occurs on order placement
- Rule evaluation uses request-level caching for performance
- Active rules use cache manager for lookup optimization

## Additional Reference

- See `context.md` in this plugin for concise internal context and implementation notes.
