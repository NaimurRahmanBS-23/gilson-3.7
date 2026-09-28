# DiscountManagerPlus Rule Scenario Setup Guide

Updated: `2026-03-14`

## Summary
Use this guide to decide which `DiscountManagerPlus` rule type to use and how to configure it quickly.

Common flow:
1. Open or create the parent nopCommerce discount.
2. Configure native fields there if needed: coupon, dates, `N times only`, `N times per customer`, maximum discount amount, maximum discounted quantity.
   Linked plugin rules honor `Maximum discounted quantity` from the parent discount at runtime, including when `Carry default discount = No`.
3. In the `DiscountManagerPlus` section, click `Add rule`.
4. Choose `Rule type`, `Discount type`, `Discount scope`, and `Carry default discount`.
5. Save.
6. Configure the `Products`, `Tiers`, and `Conditions` cards as required.

Rule type quick guide:
- `BuyXGetY`: reward, BOGO, free gift, cheaper item free, discounted reward product.
- `ProductBased`: discount matched products, category/brand/attribute-targeted product promotions.
- `ComboPricing`: bundle, mix-and-match, fixed bundle price, tiered bundle discount.
- `CartCondition`: role, country, payment, order-count, conditional AND/OR, cart-quantity rules.
- `SubtotalBased`: spend threshold or subtotal band discounts.

## Scenario Mapping
| # | Scenario | Brief description | Best setup |
|---|---|---|---|
| 1 | Buy X Get Y Free | Buy qualifying product and receive a free reward item. | `BuyXGetY` -> add buy/reward rows in `Products` -> `Tiers` with `Discount type = FreeItem` |
| 2 | Buy X Get Y Discounted | Buy one product and get another at a discount. | `BuyXGetY` -> reward tier `Percentage` or `FixedAmount` |
| 3 | Cart Quantity Based Discount | Discount starts when total cart quantity reaches a limit. | `CartCondition` -> `Conditions` with quantity min/max |
| 4 | Geo / Country Based Discount | Discount depends on customer billing or shipping country. | `CartCondition` -> `Conditions > Required countries` |
| 5 | Payment Method Discount | Discount applies only for a selected payment method. | `CartCondition` -> `Conditions > Required payment methods` |
| 6 | Customer Order Count Discount | Discount applies on a customer's specific order number or range. | `CartCondition` -> `Conditions > Min/Max paid-completed order count` |
| 7 | Role / Customer Group Discount | VIP, wholesale, or registered users get special pricing. | `CartCondition` or `ProductBased` -> `Conditions > Required customer role` |
| 8 | Conditional Discount | Discount requires multiple cart conditions with AND/OR logic. | `CartCondition` -> multiple `Conditions` using group and logical operator |
| 9 | Advanced Dynamic Pricing | Discount changes by quantity band or threshold. | `ProductBased`, `ComboPricing`, or `SubtotalBased` -> multiple `Tiers` |
| 10 | Social Sharing Discount | Discount applies only when storefront social-proof flow is completed. | `CartCondition` plus social-proof storefront token flow |
| 11 | Product Mix & Match Discount | Discount applies when a defined product set exists in cart. | `ComboPricing` -> set products in `Products` -> discount in `Tiers` |
| 12 | Category / Brand Combo Discount | Products from defined categories or brands trigger a discount together. | `ComboPricing` or `CartCondition` -> category/manufacturer sources |
| 13 | Expiry-Sensitive Discount | Near-expiry items get discounted. | `CartCondition` or `ProductBased` -> `Conditions > Source type = Expiry days` |
| 14 | Free Gift with Purchase | Customer gets a gift product after meeting buy criteria. | `BuyXGetY` or `ProductBased + FreeItem` |
| 15 | Attribute-Based Discount | Discount only for matching attributes like color or size. | `ProductBased` -> `Conditions > Product attribute values` or `Specification attribute options` |
| 16 | Attribute + Quantity Combo | Attribute-matched quantity triggers a discount or reward. | `ProductBased` or `BuyXGetY` -> attribute condition + quantity min/max |
| 17 | Attribute + Category / Manufacturer Combo | Attribute and category/brand must match together. | `ProductBased` -> attribute condition + category/manufacturer targeting |
| 18 | Attribute Mix & Match / Bundle | Discount applies for a mixed set of attribute-matched items. | `ComboPricing` -> bundle products + attribute conditions |
| 19 | Attribute-Triggered Free Gift | Gift appears only when attribute-specific products are purchased. | `BuyXGetY` or `ProductBased + FreeItem` + attribute condition |
| 20 | Tiered / Progressive Attribute Discount | Discount percentage increases as more matching attribute products are added. | `ProductBased` -> attribute condition + multiple `Tiers` |
| 21 | Attribute + Role-Based Discount | Only certain customer roles get attribute-specific discounts. | `ProductBased` -> attribute condition + role condition |
| 22 | Attribute + Time-Based Discount | Attribute-matched products are discounted only during a campaign window. | Parent native discount handles dates, plugin rule handles attribute logic |
| 23 | Attribute + Coupon Code Requirement | Coupon is required for attribute-specific discount. | Parent native discount handles coupon, plugin rule handles attribute logic |
| 24 | Fixed Bundle Discount | A fixed amount or fixed bundle price applies to a predefined set. | `ComboPricing` -> `FixedAmount` or `FixedBundlePrice` |
| 25 | Percentage Bundle Discount | Bundle gets percentage off. | `ComboPricing` -> `Discount type = Percentage` |
| 26 | Attribute-Based Bundle Discount | Bundle discount only when bundle items have matching attributes. | `ComboPricing` + attribute condition |
| 27 | Category / Manufacturer Bundle Discount | Bundle depends on category or manufacturer mix. | `ComboPricing` -> category/manufacturer source rows |
| 28 | Mix & Match Bundle | Customer chooses any N items from a set and gets a discount. | `ComboPricing` -> add eligible products -> use `Tiers` |
| 29 | Buy X Get Y Bundle | Buy a bundle and get an extra product free or discounted. | `BuyXGetY` -> add buy bundle rows + reward tier |
| 30 | Tiered Bundle Discount | Bundle discount improves as more items are included. | `ComboPricing` -> multiple `Tiers` |
| 31 | Attribute + Role-Based Bundle Discount | Bundle discount also depends on role and attributes. | `ComboPricing` + attribute + role conditions |
| 32 | Time-Limited / Flash Bundle | Bundle works only inside a campaign window or native usage limit. | Parent native discount handles time/limits, plugin handles bundle logic |
| 33 | Free Gift Bundle | Free item is added when a product bundle is purchased. | `BuyXGetY` or `ComboPricing + FreeItem` |

## Extra Promotion Examples
| Scenario | Brief description | Best setup |
|---|---|---|
| Buy one expensive product, get cheaper product free | Reward is a lower-priced or explicit reward product. | `BuyXGetY` with `Specific reward product` or `Cheapest qualified buy item` |
| Buy one, get one free same product | Same SKU acts as both buy and reward. | `BuyXGetY` |
| Buy two different products, get third discounted | Two buy products unlock a discounted reward item. | `BuyXGetY` |
| Buy specific products, get cheapest cart item free | Cheapest product in cart becomes free. | `BuyXGetY` with reward mode `Cheapest item in cart` |
| Stack discount / bundle stack | Native discount remains active and plugin adds extra promotion. | Parent native discount + linked plugin rule with `Carry default discount = Yes` |
| Buy X quantity, get same product discount | Discount applies to the matched product itself. | `ProductBased` |
| Buy X, get Y at 50% off | Reward product is discounted rather than free. | `BuyXGetY` |
| Predefined bundle at special price | Several products together receive one combined discount. | `ComboPricing` |
| Spend amount, get discount | Cart subtotal unlocks a discount. | `SubtotalBased` |
| Category quantity discount | Quantity inside one category unlocks a discount. | `ProductBased` with category source |

## Setup Notes
- Put coupon logic in the parent nopCommerce discount, not in plugin conditions.
- Put `N times only` and `N times per customer` in the parent nopCommerce discount.
- Use `Carry default discount = Yes` when both native discount and plugin rule should apply together.
- Use `Carry default discount = No` when only the plugin rule should apply, while still consuming parent discount usage history.
- Use `Require same line match` when attribute/category/vendor/source checks must belong to the same cart line.
- Use `Tiers` when one rule needs multiple quantity bands or bundle bands.
- For `CartCondition`, condition quantity fields filter the matched condition quantity. Tier `Min quantity` and `Max quantity` also use that matched condition quantity, not unrelated cart lines. If the rule only uses context checks like country, payment, role, or order count, total cart quantity is used as the fallback tier metric.
