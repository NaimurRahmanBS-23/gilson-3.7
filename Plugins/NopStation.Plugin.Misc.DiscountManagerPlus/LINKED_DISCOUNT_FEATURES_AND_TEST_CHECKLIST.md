# DiscountManagerPlus Linked Discount Features And Test Checklist

## Feature Coverage

### Admin features

- Promotion rule can optionally link to one existing nopCommerce discount.
- Promotion rule edit page shows a linked discount selector.
- Promotion rule edit page shows a `Carry default discount` toggle only when a linked discount is selected.
- Linked discount selection is validated before save.
- Saving a linked promotion rule syncs native discount requirements automatically.
- Changing the linked discount resyncs both the previous and the new native discount.
- Deleting a linked promotion rule removes or updates the managed native requirement wiring.

### Native discount integration features

- Multiple promotion rules can link to the same nopCommerce discount.
- Linked nopCommerce discounts get a managed top-level `AND` wrapper requirement when needed.
- Linked nopCommerce discounts get a managed DiscountManagerPlus carry/suppress requirement.
- Advanced DiscountManagerPlus condition requirements remain separate from linked carry/suppress requirements.
- Native requirement metadata is cleaned up when linked requirements are deleted.

### Runtime features

- A linked promotion rule is eligible only when the linked nopCommerce discount is also eligible.
- Linked discount eligibility respects native nopCommerce checks such as active flag, dates, coupon code, limitation count, gift-card restrictions, and other requirements.
- The plugin skips only its own carry/suppress requirement during linked discount eligibility checks to avoid circular blocking.
- If `Carry default discount` is enabled, both the nopCommerce discount and the plugin rule can apply together.
- If `Carry default discount` is disabled, the plugin rule can apply while the linked nopCommerce discount is suppressed for that cart.
- Existing standalone promotion rules continue to work without linking to a nopCommerce discount.
- Plugin promotion evaluation still runs even if the older global `Use default nopCommerce discount pipeline` setting is enabled.

## Test Checklist

| Case ID | Area | Scenario | Steps | Expected Result |
| --- | --- | --- | --- | --- |
| DISCOUNTMANAGERPLUS_TC_001 | Admin | Create standalone rule | Open `/PromotionRule/Create`; leave linked discount empty; save valid rule. | Rule saves successfully with no linked discount and `Carry default discount` disabled. |
| DISCOUNTMANAGERPLUS_TC_002 | Admin | Create linked rule with carry on | Create a native nop discount first; open `/PromotionRule/Create`; select that discount; enable `Carry default discount`; save. | Rule saves successfully and keeps the selected native discount link. |
| DISCOUNTMANAGERPLUS_TC_003 | Admin | Create linked rule with carry off | Create a native nop discount first; open `/PromotionRule/Create`; select that discount; disable `Carry default discount`; save. | Rule saves successfully and keeps the selected native discount link. |
| DISCOUNTMANAGERPLUS_TC_004 | Admin | Hide carry toggle when no link | Open create or edit page; clear the linked discount selector. | `Carry default discount` row is hidden or disabled and the checkbox is reset to false. |
| DISCOUNTMANAGERPLUS_TC_005 | Admin | Reject invalid linked discount | Post a promotion rule using a deleted or non-existing linked discount id. | Save is blocked and a validation error is shown for `Linked nopCommerce discount`. |
| DISCOUNTMANAGERPLUS_TC_006 | Admin | Change linked discount | Edit a rule already linked to discount A; switch it to discount B; save. | Rule updates successfully and both discount A and discount B requirement trees are resynced. |
| DISCOUNTMANAGERPLUS_TC_007 | Admin | Remove linked discount | Edit a linked rule; clear the linked discount; save. | Rule becomes standalone and the old linked discount removes the plugin carry requirement if no other linked rules need it. |
| DISCOUNTMANAGERPLUS_TC_008 | Admin | Delete linked rule | Delete a promotion rule that is linked to a native discount. | Rule is deleted and the linked discount requirement tree is cleaned up or updated. |
| DISCOUNTMANAGERPLUS_TC_009 | Native requirements | Wrapper creation | Link the first promotion rule to a native discount and save. | A managed top-level `AND` wrapper and a DiscountManagerPlus carry/suppress requirement are created for that native discount. |
| DISCOUNTMANAGERPLUS_TC_010 | Native requirements | Reuse existing wrapper | Link a second promotion rule to the same native discount and save. | No duplicate wrapper is created; the existing managed wrapper is reused. |
| DISCOUNTMANAGERPLUS_TC_011 | Native requirements | Preserve existing user requirements | Link a promotion rule to a native discount that already has nopCommerce requirements. | Existing user requirements still exist and are nested under the managed wrapper. |
| DISCOUNTMANAGERPLUS_TC_012 | Native requirements | Advanced requirement isolation | Configure an advanced DiscountManagerPlus condition requirement from the nopCommerce discount requirement UI. | Advanced condition requirement still works and is not mistaken for the linked carry/suppress requirement. |
| DISCOUNTMANAGERPLUS_TC_013 | Eligibility | Linked discount inactive | Link a promotion rule to an inactive nopCommerce discount; make the rule otherwise match. | Plugin rule does not apply. |
| DISCOUNTMANAGERPLUS_TC_014 | Eligibility | Linked discount expired | Link a promotion rule to an expired nopCommerce discount; make the rule otherwise match. | Plugin rule does not apply. |
| DISCOUNTMANAGERPLUS_TC_015 | Eligibility | Linked discount requires coupon but coupon missing | Link a promotion rule to a coupon-based nopCommerce discount; do not enter the coupon; make the rule otherwise match. | Plugin rule does not apply. |
| DISCOUNTMANAGERPLUS_TC_016 | Eligibility | Linked discount valid with coupon | Link a promotion rule to a coupon-based nopCommerce discount; enter the correct coupon; make the rule otherwise match. | Plugin rule becomes eligible. |
| DISCOUNTMANAGERPLUS_TC_017 | Eligibility | Linked discount limitation reached | Link a promotion rule to a native discount with limitation already exhausted. | Plugin rule does not apply. |
| DISCOUNTMANAGERPLUS_TC_018 | Carry behavior | Carry on applies both | Link a promotion rule to a native discount; enable `Carry default discount`; make both eligible. | Native nopCommerce discount and plugin promotion are both applied. |
| DISCOUNTMANAGERPLUS_TC_019 | Carry behavior | Carry off suppresses native discount | Link a promotion rule to a native discount; disable `Carry default discount`; make both eligible. | Plugin promotion applies and the linked native discount does not apply. |
| DISCOUNTMANAGERPLUS_TC_020 | Carry behavior | No plugin match leaves native discount alone | Link a promotion rule to a native discount; disable `Carry default discount`; keep native discount eligible but make plugin rule not match. | Native nopCommerce discount still applies normally because suppression is not triggered. |
| DISCOUNTMANAGERPLUS_TC_021 | Multi-link | Mixed carry flags on same native discount | Link two promotion rules to the same native discount, one with carry on and one with carry off; match both. | The carry-off rule suppresses the native discount, while both plugin rules can still be evaluated. |
| DISCOUNTMANAGERPLUS_TC_022 | Multi-link | Only carry-on rules matched | Link two promotion rules to the same native discount and set both to carry on; match them. | Native discount remains applied together with matched plugin rules. |
| DISCOUNTMANAGERPLUS_TC_023 | Conflict handling | Linked parent ignored when carry on | Create a linked rule with carry on where the native discount affects the same lines or cart total. | Plugin rule is not blocked by its own linked parent discount. |
| DISCOUNTMANAGERPLUS_TC_025 | Standalone regression | Standalone product-based rule | Use an existing standalone product-based rule with no linked discount. | Behavior remains unchanged from previous standalone flow. |
| DISCOUNTMANAGERPLUS_TC_026 | Standalone regression | Standalone BOGO rule | Use an existing standalone BOGO rule with no linked discount. | BOGO still applies through the plugin engine. |
| DISCOUNTMANAGERPLUS_TC_027 | Global setting regression | Older default-pipeline setting enabled | Enable `Use default nopCommerce discount pipeline`; evaluate a linked BOGO or plugin rule. | Plugin promotion evaluation still runs; advanced native requirement evaluation continues to depend on that setting. |
| DISCOUNTMANAGERPLUS_TC_028 | Cart surface | Cart page totals | Apply a linked rule with carry on, then with carry off. | Cart totals reflect the correct combination of native and plugin discounts in both cases. |
| DISCOUNTMANAGERPLUS_TC_029 | Checkout surface | Checkout totals | Repeat the linked carry-on and carry-off scenarios through checkout. | Checkout totals match cart behavior and remain consistent until order placement. |
| DISCOUNTMANAGERPLUS_TC_030 | Product surface | Product page pricing or badges | Use a linked rule that affects product display where applicable. | Product page pricing/badges remain correct and do not double-count the linked native discount. |
| DISCOUNTMANAGERPLUS_TC_031 | Order placement | Successful order with carry on | Place an order where the linked native discount and plugin rule both apply. | Order completes successfully and the combined discounts are reflected correctly. |
| DISCOUNTMANAGERPLUS_TC_032 | Order placement | Successful order with carry off | Place an order where the linked native discount is suppressed by the plugin rule. | Order completes successfully and only the plugin promotion effect is present for the linked campaign. |
| DISCOUNTMANAGERPLUS_TC_033 | Usage history | Native limitation after orders | Use a linked discount with native limitation rules across multiple orders. | Native limitation counts continue to control linked rule eligibility correctly. |
| DISCOUNTMANAGERPLUS_TC_034 | Cleanup | Last linked rule removed | Remove the last promotion rule linked to a native discount. | Managed carry requirement is removed and empty managed wrapper is deleted. |
| DISCOUNTMANAGERPLUS_TC_035 | Cleanup | Wrapper with user requirements remains | Remove the last linked rule from a discount that still has user-defined native requirements. | Plugin carry requirement is removed, while the remaining user requirements continue to work. |

## Recommended Smoke Set

- DISCOUNTMANAGERPLUS_TC_002
- DISCOUNTMANAGERPLUS_TC_003
- DISCOUNTMANAGERPLUS_TC_006
- DISCOUNTMANAGERPLUS_TC_011
- DISCOUNTMANAGERPLUS_TC_015
- DISCOUNTMANAGERPLUS_TC_018
- DISCOUNTMANAGERPLUS_TC_019
- DISCOUNTMANAGERPLUS_TC_020
- DISCOUNTMANAGERPLUS_TC_025
- DISCOUNTMANAGERPLUS_TC_026
- DISCOUNTMANAGERPLUS_TC_028
- DISCOUNTMANAGERPLUS_TC_029
