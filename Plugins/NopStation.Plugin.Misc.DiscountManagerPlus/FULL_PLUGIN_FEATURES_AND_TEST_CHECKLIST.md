# DiscountManagerPlus Full Features And Test Checklist

## Full Feature List

### Core plugin controls

- Global plugin enable/disable
- Store-aware configuration support
- Max rule evaluation time budget
- Promotion badge toggle
- Cart savings breakdown toggle
- Default nopCommerce discount pipeline toggle

### Promotion rule management

- Promotion rule list, create, edit, delete
- Rule setup assistant and setup-status summary
- Priority-based rule ordering
- Exclusive rule behavior
- Rule date range support
- Store-limited rules
- Flash controls:
  - usage limit total
  - usage limit per customer
  - usage window start/end
- Rule usage history logging

### Rule types

- Product-based promotions
- Combo pricing promotions
- Buy X Get Y promotions
- Cart condition promotions
- Subtotal-based promotions

### Discount behaviors

- Percentage discount
- Fixed amount discount
- Fixed bundle price discount
- Free item discount
- Matched-items-only scope
- Whole-cart scope
- Stop further rules for matched lines
- Cumulative with default discount conflict handling

### Rule product configuration

- Product source
- Category source
- Manufacturer source
- Vendor source
- Min quantity and max quantity
- Reward product support
- Reward attribute selection:
  - any customer-selected attributes
  - specific attribute values

### Rule tier configuration

- Tier create, edit, delete
- Quantity-based tier selection
- Tier reward quantity
- Tier reward product
- Auto-add reward support
- Tier mapping to selected buy products

### Rule conditions

- Nested/grouped condition logic
- AND / OR interaction
- Parent-child condition hierarchy
- Subtotal operators:
  - greater than
  - less than
  - between
  - equal to
- Required product
- Excluded product
- Required category
- Required vendor
- Required customer role
- First-order-only
- New-customer-only
- Required country codes
- Required payment methods
- Required coupon codes
- Required order-count min/max
- Total quantity min/max
- Same-line matching
- Attribute match mode:
  - any
  - all

### Advanced condition source types

- Products
- Categories
- Manufacturers
- Vendors
- Specification attribute options
- Product attribute values
- Expiry days
- Device type
- Sales channel
- Campaign source
- Referral source

### Social-proof and context-aware logic

- `SOCIAL_PROOF` coupon marker support
- Social share event tracking
- One-time proof token validation flow
- Request-context coupon, payment, country, and source evaluation

### Admin tools

- Rule setup status and validation guardrails
- Usage history and linked discount visibility
- Analytics dashboard
- Aggregate promotion analytics totals

### Storefront features

- Product promotion badge
- Header offers link
- Public offers page
- Cart savings widget
- Reward selection UI for configurable reward products
- Reward auto-add synchronization

### nopCommerce native discount integration

- Advanced DiscountManagerPlus conditions as native nopCommerce discount requirements
- Promotion rule optional link to a native nopCommerce discount
- Carry linked native discount with plugin rule
- Suppress linked native discount when carry is off
- Managed native requirement wrapper sync
- Linked native discount eligibility gating

## Full Test Checklist

| Case ID | Area | Scenario | Steps | Expected Result |
| --- | --- | --- | --- | --- |
| DISCOUNTMANAGERPLUS_TC_001 | Configuration | Enable plugin | Open configuration, enable plugin, save. | Plugin saves successfully and promotion processing is enabled. |
| DISCOUNTMANAGERPLUS_TC_002 | Configuration | Disable plugin | Disable plugin and save. | Plugin storefront/admin runtime behavior stops applying promotions. |
| DISCOUNTMANAGERPLUS_TC_003 | Configuration | Promotion badge toggle | Enable badge, save, then disable and save. | Product badge appears only when the toggle is enabled. |
| DISCOUNTMANAGERPLUS_TC_004 | Configuration | Cart savings toggle | Enable cart savings breakdown, save, then disable and save. | Cart savings widget respects the toggle. |
| DISCOUNTMANAGERPLUS_TC_005 | Configuration | Default pipeline toggle | Enable and disable `Use default nopCommerce discount pipeline`. | Native DiscountManagerPlus requirements respect the setting, while plugin promotion runtime remains stable. |
| DISCOUNTMANAGERPLUS_TC_006 | Configuration | Store-specific override | Change configuration in one store scope only. | Store override is saved and does not leak into other stores. |
| DISCOUNTMANAGERPLUS_TC_007 | Rules | Create basic rule | Create a new promotion rule with valid base info. | Rule saves successfully. |
| DISCOUNTMANAGERPLUS_TC_008 | Rules | Edit rule | Edit an existing rule and save changes. | Changes persist correctly. |
| DISCOUNTMANAGERPLUS_TC_009 | Rules | Delete rule | Delete an existing rule. | Rule is removed and related child data is cleaned up. |
| DISCOUNTMANAGERPLUS_TC_010 | Rules | Priority ordering | Create two overlapping rules with different priorities. | Lower priority value is evaluated first. |
| DISCOUNTMANAGERPLUS_TC_011 | Rules | Exclusive behavior | Mark a matching rule as exclusive with another lower-priority rule also eligible. | Exclusive rule applies and later rules do not. |
| DISCOUNTMANAGERPLUS_TC_012 | Rules | Store limitation | Limit a rule to one store and test in another store. | Rule applies only in the configured store. |
| DISCOUNTMANAGERPLUS_TC_013 | Rules | Date limitation | Set a future start date or expired end date. | Rule does not apply outside the active range. |
| DISCOUNTMANAGERPLUS_TC_014 | Rules | Flash total usage limit | Set `UsageLimitTotal` and consume it. | Rule stops applying after the total limit is reached. |
| DISCOUNTMANAGERPLUS_TC_015 | Rules | Flash per-customer usage limit | Set `UsageLimitPerCustomer` and use rule repeatedly with same customer. | Rule stops applying for that customer after the limit is reached. |
| DISCOUNTMANAGERPLUS_TC_016 | Rules | Flash usage window | Set usage window start/end and test inside/outside window. | Rule applies only during the usage window. |
| DISCOUNTMANAGERPLUS_TC_017 | ProductBased | Percentage discount | Create product-based percentage rule and add qualifying cart items. | Percentage discount is applied to matched items. |
| DISCOUNTMANAGERPLUS_TC_018 | ProductBased | Fixed amount discount | Create product-based fixed amount rule and add qualifying items. | Fixed amount is applied correctly. |
| DISCOUNTMANAGERPLUS_TC_019 | ProductBased | Free item reward | Create product-based free-item rule with reward product. | Reward discount is applied only when buy criteria are satisfied. |
| DISCOUNTMANAGERPLUS_TC_020 | ProductBased | Category source | Configure product-based rule using category source. | Products from the category qualify correctly. |
| DISCOUNTMANAGERPLUS_TC_021 | ProductBased | Manufacturer source | Configure product-based rule using manufacturer source. | Matching manufacturer products qualify correctly. |
| DISCOUNTMANAGERPLUS_TC_022 | ProductBased | Vendor source | Configure product-based rule using vendor source. | Matching vendor products qualify correctly. |
| DISCOUNTMANAGERPLUS_TC_023 | ProductBased | Min and max quantity | Configure min/max quantity on rule product rows. | Rule uses quantity boundaries correctly. |
| DISCOUNTMANAGERPLUS_TC_024 | ComboPricing | Fixed bundle price | Create combo pricing rule with fixed bundle price. | Discount equals regular set subtotal minus bundle price. |
| DISCOUNTMANAGERPLUS_TC_025 | ComboPricing | Percentage combo | Create combo rule with percentage discount. | Percentage applies to the matched combo subtotal. |
| DISCOUNTMANAGERPLUS_TC_026 | ComboPricing | Fixed amount combo | Create combo rule with fixed amount. | Fixed amount applies per qualifying set. |
| DISCOUNTMANAGERPLUS_TC_027 | ComboPricing | Multiple complete sets | Add quantities for multiple full sets. | Discount scales by number of complete sets only. |
| DISCOUNTMANAGERPLUS_TC_028 | BuyXGetY | Free reward tier | Create Buy X Get Y rule with free reward tier. | Reward quantity and discount are applied correctly. |
| DISCOUNTMANAGERPLUS_TC_029 | BuyXGetY | Discounted reward tier | Configure Buy X Get Y tier with percentage/fixed reward discount. | Discount is allocated only to reward lines. |
| DISCOUNTMANAGERPLUS_TC_030 | BuyXGetY | Tier escalation | Configure multiple tiers and meet the higher tier threshold. | Best matching tier is selected correctly. |
| DISCOUNTMANAGERPLUS_TC_031 | BuyXGetY | Auto-add reward | Configure auto-add reward and qualify cart. | Reward is automatically synchronized into cart when possible. |
| DISCOUNTMANAGERPLUS_TC_032 | Reward attributes | Specific reward attribute values | Configure reward with specific attribute values. | Reward is added/validated using the configured attribute values. |
| DISCOUNTMANAGERPLUS_TC_033 | Reward attributes | Customer-selected reward attributes | Use reward product requiring selection. | Reward selection UI is shown and customer can complete reward selection. |
| DISCOUNTMANAGERPLUS_TC_034 | CartCondition | Required product | Create cart-condition rule with required product. | Rule applies only when required product is in cart. |
| DISCOUNTMANAGERPLUS_TC_035 | CartCondition | Excluded product | Configure excluded product. | Rule is blocked when excluded product is present. |
| DISCOUNTMANAGERPLUS_TC_036 | CartCondition | Required category | Configure required category. | Rule applies only when cart contains that category. |
| DISCOUNTMANAGERPLUS_TC_037 | CartCondition | Required vendor | Configure required vendor. | Rule applies only when cart contains that vendor. |
| DISCOUNTMANAGERPLUS_TC_038 | CartCondition | Required customer role | Configure required customer role. | Only customers in that role qualify. |
| DISCOUNTMANAGERPLUS_TC_039 | CartCondition | First order only | Configure first-order-only condition. | Rule applies only for customers with no previous orders. |
| DISCOUNTMANAGERPLUS_TC_040 | CartCondition | New customer only | Configure new-customer-only condition. | Rule applies only for customers meeting new-customer criteria. |
| DISCOUNTMANAGERPLUS_TC_041 | CartCondition | Country and payment method | Configure required country and payment method conditions. | Rule applies only when both context values match. |
| DISCOUNTMANAGERPLUS_TC_042 | CartCondition | Required coupon codes | Configure required coupon codes CSV and test with matching/non-matching code. | Rule qualifies only with the allowed coupon codes. |
| DISCOUNTMANAGERPLUS_TC_043 | CartCondition | Required order count range | Configure min/max paid order count. | Rule applies only when customer order count is inside the range. |
| DISCOUNTMANAGERPLUS_TC_044 | CartCondition | Total quantity range | Configure total quantity min/max. | Rule applies only when total cart quantity matches the range. |
| DISCOUNTMANAGERPLUS_TC_045 | Conditions | Group logic AND/OR | Create grouped conditions with multiple operators. | Group logic is evaluated exactly as configured. |
| DISCOUNTMANAGERPLUS_TC_046 | Conditions | Nested parent-child logic | Configure nested child conditions under a parent condition. | Nested logic is evaluated correctly without cyclic issues. |
| DISCOUNTMANAGERPLUS_TC_047 | Conditions | Same-line matching | Enable same-line match on source/product/category/vendor conditions. | Conditions only pass when all required criteria occur on the same cart line. |
| DISCOUNTMANAGERPLUS_TC_048 | Conditions | Attribute match mode any/all | Configure product attribute value condition with `Any`, then `All`. | Matching behavior changes correctly between the two modes. |
| DISCOUNTMANAGERPLUS_TC_049 | Source types | Specification attribute option | Configure source type using specification attribute options. | Only products with the configured spec options qualify. |
| DISCOUNTMANAGERPLUS_TC_050 | Source types | Product attribute values | Configure source type using product attribute values. | Only products with the configured attribute values qualify. |
| DISCOUNTMANAGERPLUS_TC_051 | Source types | Expiry days | Configure source type `ExpiryDays`. | Condition evaluates based on configured expiry-day logic. |
| DISCOUNTMANAGERPLUS_TC_052 | Source types | Device type | Configure source type `DeviceType`. | Rule responds correctly to device context. |
| DISCOUNTMANAGERPLUS_TC_053 | Source types | Sales channel | Configure source type `SalesChannel`. | Rule responds correctly to channel context. |
| DISCOUNTMANAGERPLUS_TC_054 | Source types | Campaign source | Configure source type `CampaignSource`. | Rule responds correctly to campaign query/header values. |
| DISCOUNTMANAGERPLUS_TC_055 | Source types | Referral source | Configure source type `ReferralSource`. | Rule responds correctly to referral context. |
| DISCOUNTMANAGERPLUS_TC_056 | Social proof | SOCIAL_PROOF marker success | Configure required coupon CSV containing `SOCIAL_PROOF` and supply a valid proof token. | Rule applies when a valid social proof token is available. |
| DISCOUNTMANAGERPLUS_TC_057 | Social proof | SOCIAL_PROOF marker failure | Use invalid or missing token with `SOCIAL_PROOF` requirement. | Rule does not apply. |
| DISCOUNTMANAGERPLUS_TC_060 | Conflict handling | Stop further rules for matched lines | Apply one rule with stop-further-lines enabled and another rule targeting same lines. | Later rule does not add extra discounts on already-blocked lines. |
| DISCOUNTMANAGERPLUS_TC_065 | Usage history | Rule usage log on order placement | Place an order using a plugin promotion. | Usage history entry is created for the rule. |
| DISCOUNTMANAGERPLUS_TC_066 | Analytics | Analytics dashboard totals | Open analytics after rule usage data exists. | Totals and per-rule analytics are populated correctly. |
| DISCOUNTMANAGERPLUS_TC_067 | Public UI | Product badge | Enable badge and view qualifying product listings/details. | Promotion badge is visible where expected. |
| DISCOUNTMANAGERPLUS_TC_068 | Public UI | Offers header link | Keep at least one active offer available. | Header offers link appears and points to `/promotions/offers`. |
| DISCOUNTMANAGERPLUS_TC_069 | Public UI | Offers page | Open public offers page with active and inactive rules. | Only active/eligible offers are shown correctly. |
| DISCOUNTMANAGERPLUS_TC_070 | Cart UI | Cart savings breakdown | Enable cart savings and apply one or more rules. | Cart savings section shows rule-level and total savings. |
| DISCOUNTMANAGERPLUS_TC_071 | Cart UI | Pending reward selection | Qualify for a configurable reward product. | Reward selection UI appears and can submit reward choices. |
| DISCOUNTMANAGERPLUS_TC_072 | Native requirements | Advanced condition requirement create/edit | Add DiscountManagerPlus advanced condition requirement from nopCommerce discount settings. | Requirement saves and validates through nopCommerce requirement flow. |
| DISCOUNTMANAGERPLUS_TC_073 | Linked native discount | Create linked rule with carry on | Link a promotion rule to a native discount and enable `Carry default discount`. | Both native and plugin discounts can apply together. |
| DISCOUNTMANAGERPLUS_TC_074 | Linked native discount | Create linked rule with carry off | Link a promotion rule to a native discount and disable `Carry default discount`. | Plugin rule applies and linked native discount is suppressed when plugin rule matches. |
| DISCOUNTMANAGERPLUS_TC_075 | Linked native discount | Linked discount eligibility gate | Link a rule to inactive, expired, coupon-missing, and valid native discounts. | Plugin rule eligibility follows native discount validity. |
| DISCOUNTMANAGERPLUS_TC_076 | Linked native discount | Shared parent discount | Link multiple promotion rules to the same native discount. | Managed wrapper/carry requirement stay consistent and no duplicate wrapper is created. |
| DISCOUNTMANAGERPLUS_TC_077 | Linked native discount | Linked discount cleanup | Remove or delete the last linked promotion rule for a native discount. | Managed carry requirement is removed and wrapper is cleaned up when no longer needed. |
| DISCOUNTMANAGERPLUS_TC_078 | Checkout | Totals consistency | Verify matching promotion behavior in cart and checkout. | Totals remain consistent through checkout. |
| DISCOUNTMANAGERPLUS_TC_079 | Order placement | Place order with plugin-only rule | Complete order where only plugin rule applies. | Order succeeds and totals/usage history are correct. |
| DISCOUNTMANAGERPLUS_TC_080 | Order placement | Place order with linked carry-on rule | Complete order where both linked native and plugin rules apply. | Order succeeds with correct combined discount effect. |

## Recommended End-To-End Smoke Set

- DISCOUNTMANAGERPLUS_TC_001
- DISCOUNTMANAGERPLUS_TC_007
- DISCOUNTMANAGERPLUS_TC_017
- DISCOUNTMANAGERPLUS_TC_024
- DISCOUNTMANAGERPLUS_TC_028
- DISCOUNTMANAGERPLUS_TC_034
- DISCOUNTMANAGERPLUS_TC_045
- DISCOUNTMANAGERPLUS_TC_056
- DISCOUNTMANAGERPLUS_TC_058
- DISCOUNTMANAGERPLUS_TC_063
- DISCOUNTMANAGERPLUS_TC_066
- DISCOUNTMANAGERPLUS_TC_067
- DISCOUNTMANAGERPLUS_TC_070
- DISCOUNTMANAGERPLUS_TC_072
- DISCOUNTMANAGERPLUS_TC_073
- DISCOUNTMANAGERPLUS_TC_074
- DISCOUNTMANAGERPLUS_TC_078
- DISCOUNTMANAGERPLUS_TC_079

## Related Files

- Linked-discount-only checklist: `LINKED_DISCOUNT_FEATURES_AND_TEST_CHECKLIST.md`
- Admin usage guide: `ADMIN_USER_MANUAL.md`
- Configuration guide: `CONFIGURATION_GUIDE.md`
- Customer guide: `CUSTOMER_GUIDE.md`
