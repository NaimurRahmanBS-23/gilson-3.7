# DiscountManagerPlus Context

## 1) What this plugin does

`NopStation.Plugin.Misc.DiscountManagerPlus` adds rule-based promotions to nopCommerce and applies discounts directly in cart pricing.

It supports:

- Rule evaluation for multiple rule types
- Cart price integration through `GetShoppingCartItemUnitPriceEvent`
- Promotion badge on product cards
- Cart savings breakdown
- Public "All Offers" page for customers

---

## 2) Admin pages

- Configuration: `/Admin/DiscountManagerPlus/Configure`
- Rule list: `/Admin/PromotionRule/List`
- Create rule: `/Admin/PromotionRule/Create`
- Edit rule: `/Admin/PromotionRule/Edit/{id}`

---

## 3) Customer-facing pages/components

- Offers page: `/promotions/offers`
- Header link widget: "View all offers" (shows only when active offers exist)
- Product card badge: "Promotion available" (when applicable)
- Cart savings block: per-rule savings and total savings

---

## 4) Rule types and discount types

### Rule type

- `ProductBased`
- `ComboPricing`
- `BuyXGetY`
- `CartCondition`
- `SubtotalBased`

### Discount type

- `Percentage`
- `FixedAmount`
- `FixedBundlePrice`
- `FreeItem`

### Allowed combinations

- `ProductBased` -> `Percentage`, `FixedAmount`, `FreeItem`
- `ComboPricing` -> `Percentage`, `FixedAmount`, `FixedBundlePrice`
- `BuyXGetY` -> Uses tiers (main rule discount is not used)
- `CartCondition` -> `Percentage`, `FixedAmount`
- `SubtotalBased` -> `Percentage`, `FixedAmount`

Invalid combinations are blocked in validation.

---

## 5) Quick admin setup guide

1. Go to `/Admin/DiscountManagerPlus/Configure`.
2. Enable the plugin (`IsEnabled`).
3. Optional: enable badge and cart savings breakdown.
4. Create a rule from `/Admin/PromotionRule/Create`.
5. Save once, then configure products/tiers/conditions from popup sections.
6. Test on storefront with a real cart.

---

## 6) How each rule works

### ProductBased

Use when discount depends on selected products and quantities.

- Add buy products in **Products**
- Set `MinQuantity` per row
- For `FreeItem`, add one reward row with `IsRewardProduct = true`

### ComboPricing

Use bundle pricing logic.

- Add required combo products in **Products**
- `setCount` = number of complete sets found in cart
- Discount:
  - `FixedBundlePrice`: `(matched sets subtotal) - (bundle price * setCount)`
  - `Percentage`: `%` on matched sets subtotal
  - `FixedAmount`: `amount * setCount`

### BuyXGetY

Use tiered BOGO logic.

- Add buy/reward products in **Products**
- Add tiers in **Tiers**
- Tier controls reward quantity, discount type, and optional auto-add

### CartCondition

Use product presence conditions.

- Add conditions in **Conditions**
- Supports required product and excluded product checks

### SubtotalBased

Use subtotal threshold conditions.

- Add conditions with operator:
  - `GreaterThan`
  - `LessThan`
  - `Between`
  - `EqualTo`

---

## 7) Product rows meaning

In product popup:

- `ProductId`: target product
- `MinQuantity`: minimum required quantity for that row
- `IsRewardProduct`:
  - Used for `BuyXGetY`
  - Also used for `ProductBased + FreeItem`
  - Not used for `ComboPricing`

---

## 8) Customer experience flow

1. Customer sees "View all offers" link and can open `/promotions/offers`.
2. Customer adds products to cart.
3. DiscountManagerPlus evaluates active rules.
4. Discount is distributed to cart line prices.
5. Customer sees savings in cart breakdown.

No coupon code is required for these rules.

---

## 9) Troubleshooting checklist

If a promotion is not applying:

1. Confirm plugin is enabled in configuration.
2. Confirm rule is active and date range is valid.
3. Confirm store scope (`LimitedToStore`) matches current store.
4. Confirm rule type and discount type combination is valid.
5. Confirm required products and `MinQuantity` are satisfied.
6. For `FreeItem`, confirm a reward product is configured.
7. For `BuyXGetY`, confirm at least one valid tier exists.
8. Check if another earlier exclusive rule stopped processing.

---

## 10) Notes for maintainers

- Pricing integration is event-based (`GetShoppingCartItemUnitPriceEvent`).
- Rule evaluation uses request-level caching to avoid repeated recalculation.
- Order usage tracking is recorded on order placement.
- Public offers page model is prepared by `PromotionOfferModelFactory`.
