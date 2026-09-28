# DiscountManagerPlus Feature Setup (Brief)

## 1. Prerequisites
1. Go to `Admin > NopStation > DiscountManagerPlus > Configure`.
2. Enable the plugin.
3. Open a discount: `Admin > Discounts > Edit/{DiscountId}`.
4. In the **DiscountManagerPlus** card:
   - Check `Enable DiscountManagerPlus sync`
   - Select rule settings
   - Click `Save promotion rule`

## 2. Feature Types

### Product-Based (Percentage / Fixed)
- nop Discount Type: `Assigned to products` or `Assigned to categories`
- Rule Type: `ProductBased`
- Discount Type: `Percentage` or `FixedAmount`
- Configure products from the `Products` section

### Product-Based (Free Item)
- nop Discount Type: `Assigned to products`
- Rule Type: `ProductBased`
- Discount Type: `FreeItem`
- Configure buy/reward products in `Products`

### Cart Condition
- nop Discount Type: `Assigned to order subtotal` or `Assigned to order total`
- Rule Type: `CartCondition`
- Discount Type: `Percentage` or `FixedAmount`
- Scope: `WholeCart`
- Configure logic in `Conditions`

### Subtotal Based
- nop Discount Type: `Assigned to order subtotal` or `Assigned to order total`
- Rule Type: `SubtotalBased`
- Discount Type: `Percentage` or `FixedAmount`
- Scope: `WholeCart`
- Configure subtotal conditions in `Conditions`

### Combo Pricing
- nop Discount Type: usually `Assigned to products`
- Rule Type: `ComboPricing`
- Discount Type: `FixedBundlePrice` (or `%` / `FixedAmount`)
- Configure `Products` and `Tiers`

### Buy X Get Y
- nop Discount Type: usually `Assigned to products`
- Rule Type: `BuyXGetY`
- Discount Type: usually `FreeItem`
- Configure `Products` and `Tiers`

## 3. Requirement Groups
- You can combine DiscountManagerPlus with default nop requirement groups.
- For supported rule types, plugin requirement is attached automatically during save.
- You can still add native nop requirements (coupon, role, etc.) for combined logic.

## 4. Notes
- `Assigned to shipping` is not supported in current integration.
- If you disable `Enable DiscountManagerPlus sync`, the mapping remains but evaluation is disabled for that discount.
