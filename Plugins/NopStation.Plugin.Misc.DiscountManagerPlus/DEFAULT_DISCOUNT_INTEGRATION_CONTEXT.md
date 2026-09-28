# Default Discount Integration Context

## Purpose

This document defines how DiscountManagerPlus integrates with the default nopCommerce Discount admin page so that all rule configuration is controlled from the standard Discount UI.

## Scope

- Manage DiscountManagerPlus rules from Discount Edit page.
- Keep Discount as the admin-facing object.
- DiscountManagerPlus remains the runtime discount engine.
- Default discounts remain active; DiscountManagerPlus is used for advanced rules to avoid overlap.

## Key Design Decisions

1. Integration approach: embed DiscountManagerPlus editor inside Discount page.
2. Rule scope: all DiscountManagerPlus rule types.
3. Data storage: new mapping table linking Discount and PromotionRule.

## Mapping Entity

`DiscountPromotionRuleMapping`

Fields:

- `DiscountId`
- `PromotionRuleId`
- timestamps

This provides one-to-one mapping between Discount and PromotionRule.

## UI Integration

- Use `AdminWidgetZones.DiscountDetailsBlock` to inject a DiscountManagerPlus card.
- Use AJAX to save PromotionRule and mapping to avoid nested forms.
- Reuse existing PromotionRule product/tier/condition grids.

## Data Synchronization

Discount -> PromotionRule:

- Name
- Active state
- Start date
- End date
- Discount amount or percentage (when rule type uses DiscountValue)

PromotionRule-specific fields remain in PromotionRule.

## Preventing Double Discount

- No automatic suppression is applied.
- Admins should map only advanced DiscountManagerPlus rules (BOGO, combo, advanced conditions) and keep simple discounts in default nopCommerce.

## Compatibility

- Non-mapped discounts behave exactly as default nopCommerce discounts.
- Existing DiscountManagerPlus rule list remains, but Discount page becomes primary editor.

## Rollout Notes

- Migration is required for mapping table.
- Add localization resources for new UI.
- Update documentation index in README.

## Risks

1. Discount edit page complexity increases.
2. Need careful sync to avoid mismatch between Discount and PromotionRule values.
3. Double-discount risk if mapping state or requirement rule fails.

## Success Criteria

1. Discount page can fully configure DiscountManagerPlus rules.
2. Applied discounts come from DiscountManagerPlus only for advanced rules, while default discounts remain for simple cases.
3. Admin UX parity with existing PromotionRule editor.
