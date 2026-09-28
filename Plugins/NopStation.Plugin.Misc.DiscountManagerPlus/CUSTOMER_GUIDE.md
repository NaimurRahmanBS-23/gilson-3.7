# DiscountManagerPlus Customer Guide

## 1. What This Does for Customers

This plugin applies offers automatically in the storefront when your cart matches an active promotion rule.

You do not need a coupon code for these rules.

---

## 2. Where Customers See Offers

1. Offers page: `/promotions/offers`
2. Header link: `View all offers` (shows when active offers exist)
3. Product badge: `Promotion available` (on eligible products)
4. Cart savings section: per-rule savings + total savings

---

## 3. How Automatic Offers Work

When products are added to cart:

1. System checks active offers.
2. Matching offers are applied automatically.
3. Discount is reflected in cart pricing.
4. Savings breakdown appears in cart (if enabled by store admin).

---

## 4. Offer Types Customers Can Receive

## 4.1 Product-Based Offer

Discount applies when specific products (or defined product groups) are in your cart.

Example:

1. Buy 2 of Product A
2. Get 10% off Product A lines

## 4.2 Combo/Bundle Offer

Discount applies when a required combination of products is purchased together.

Example:

1. Buy Product A + Product B together
2. Bundle price becomes fixed at a lower amount

## 4.3 Buy X Get Y (BOGO)

Reward is given when buy quantity conditions are met.

Example:

1. Buy 2 of Product A
2. Get 1 of Product B free (or discounted)

## 4.4 Cart Condition Offer

Discount applies when cart/customer conditions are met.

Example:

1. VIP customer with required item in cart
2. Receives extra fixed discount

## 4.5 Subtotal-Based Offer

Discount applies when subtotal reaches threshold/range.

Example:

1. Cart subtotal above 500
2. Get 10% off

---

## 5. Customer-Friendly Examples

### Example A: “Spend More, Save More”

1. Subtotal over 300 -> 5% off
2. Subtotal over 600 -> 10% off

### Example B: “Outfit Combo”

1. Shoes + Socks purchased together
2. Fixed bundle discount applied automatically

### Example C: “Starter Deal”

1. Buy 2 essentials
2. Get 1 accessory free

---

## 6. Why Offer Amount May Change

Offer results can vary if:

1. Product quantity changes
2. A required item is removed
3. Cart subtotal crosses threshold
4. Offer date expires
5. Another higher-priority exclusive offer applies

---

## 7. Frequently Asked Questions

### Q1: Do I need a coupon code?

No. These offers are automatic.

### Q2: Where do I see my savings?

In cart/order summary, under promotion savings (if enabled by the store).

### Q3: Why didn’t my offer apply?

Possible reasons:

1. Quantity requirement not met
2. Required product missing
3. Subtotal condition not met
4. Offer expired or not active for current store

### Q4: Can multiple offers apply together?

Sometimes yes. But if an exclusive higher-priority rule applies, other rules may stop.

### Q5: What if I remove a reward item?

Depending on store configuration, reward may be recalculated or removed when cart no longer qualifies.

---

## 8. Best Practices for Customers

1. Check `/promotions/offers` before checkout.
2. Watch product badges for eligible items.
3. Review cart savings block before placing order.
4. Keep required quantities in cart to retain discount.

---

## 9. Support

If expected offer is not applying:

1. Capture cart contents and quantities.
2. Share screenshot of cart totals/savings.
3. Contact store support team with product IDs and quantities.
