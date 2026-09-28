# DiscountManagerPlus Feature Groups

This document summarizes supported features grouped by category.

## Group Count

- 5 promotion rule groups
- 4 reward and selection features
- 3 storefront experience features
- 3 admin and governance features

## 1) Promotion Rule Groups

### Product Based

- Percentage discount on eligible products
- Fixed amount discount on eligible products
- Free item reward from configured reward products

### Combo Pricing

- Percentage discount on full bundle sets
- Fixed amount discount on full bundle sets
- Fixed bundle price (bundle subtotal vs target price)

### Buy X Get Y (BOGO and Tiered Rewards)

- Tier-based reward logic
- Free item reward tiers
- Discounted reward tiers

### Cart Condition

- Cart-level percentage discounts
- Cart-level fixed amount discounts

### Subtotal Based

- Subtotal threshold and range checks
- Percentage or fixed amount discounts

## 2) Reward and Selection Features

- Reward product definition by product or category
- Manual reward selection for free items
- Reward attributes selection support
- Auto-add reward when configured

## 3) Storefront Experience

- Promotion badge on product cards
- Offers page at /promotions/offers
- Cart reward selection modal

## 4) Admin and Governance

- Rule priority and exclusivity
- Rule setup validation guardrails
- Usage history and analytics

## Notes

- Rule types and discount types are validated by the rule matrix in the admin UI.
- Some features depend on rule type, discount type, and tier settings.
