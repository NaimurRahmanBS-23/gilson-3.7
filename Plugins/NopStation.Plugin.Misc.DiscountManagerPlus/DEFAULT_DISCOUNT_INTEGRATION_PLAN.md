# DiscountManagerPlus Default Discount Integration Plan

## Summary

Goal: make DiscountManagerPlus fully controlled from the default nopCommerce Discount admin page. The Discount page becomes the management surface, while DiscountManagerPlus remains the evaluation engine.

Chosen approach:

- Embed a DiscountManagerPlus editor card into Discount Edit page using AdminWidgetZones.DiscountDetailsBlock.
- Support all DiscountManagerPlus rule types.
- Store a dedicated Discount-to-PromotionRule mapping in a new table.

---

## Objectives

1. Manage DiscountManagerPlus rules from the default Discount admin UI.
2. Keep default discounts active and avoid overlap by using DiscountManagerPlus for advanced rules only.
3. Preserve DiscountManagerPlus evaluation behavior and storefront components.
4. Keep Discount fields as the canonical high-level campaign metadata.

---

## High-Level Architecture

1. Discount remains the primary admin entity.
2. Each managed Discount can map to exactly one PromotionRule.
3. PromotionRule carries rule-type-specific logic and evaluation behavior.
4. Discount page embeds a DiscountManagerPlus editor card.
5. DiscountManagerPlus engine applies its rules; default discount application remains active for non-overlapping cases.

---

## Data Model Changes

New entity: `DiscountPromotionRuleMapping`

Fields:

- `Id`
- `DiscountId`
- `PromotionRuleId`
- `CreatedOnUtc`
- `UpdatedOnUtc`

New migration and builder added under plugin Data/Migrations and Data/Builders.

---

## Service Layer

New service: `IDiscountPromotionRuleMappingService`

Methods:

- `GetMappingByDiscountIdAsync(int discountId)`
- `GetMappingByRuleIdAsync(int ruleId)`
- `CreateMappingAsync(int discountId, int promotionRuleId)`
- `UpdateMappingAsync(DiscountPromotionRuleMapping mapping)`
- `DeleteMappingAsync(DiscountPromotionRuleMapping mapping)`

---

## UI Integration (Discount Page)

Add a new Admin widget block via `AdminWidgetZones.DiscountDetailsBlock`:

Card title: `DiscountManagerPlus`

Card contents:

1. Rule type selector
2. Discount type selector (mirrors DiscountManagerPlus types)
3. Discount value input
4. Buttons:
   - `Create / Link Rule`
   - `Save Rule`
   - `Open Full Rule Editor` (optional popup)
6. Embedded sections for:
   - Products
   - Tiers
   - Conditions

Avoid nested forms. Use AJAX endpoints to save PromotionRule data.

---

## Controller and Endpoints

Add new admin controller inside plugin:

`DiscountDiscountManagerPlusController`

Endpoints:

- `GetRuleEditor(int discountId)` -> returns partial view for DiscountManagerPlus editor card
- `SaveRuleInfo(DiscountPromotionRuleBindingModel model)` -> saves rule info + mapping
- `RuleProductList / RuleProductAddPopup / RuleProductEditPopup / RuleProductDelete` -> reuse existing endpoints or proxy
- `RuleTierList / RuleTierCreatePopup / RuleTierEditPopup / RuleTierDelete`
- `RuleConditionList / RuleConditionCreatePopup / RuleConditionEditPopup / RuleConditionDelete`

Alternatively reuse existing `PromotionRuleController` endpoints by passing `promotionRuleId` from mapping.

---

## Discount Field Synchronization Rules

When a Discount is mapped:

1. Discount name maps to PromotionRule name.
2. Discount active state maps to PromotionRule `IsActive`.
3. Discount start/end dates map to PromotionRule start/end.
4. Discount percentage / amount maps to PromotionRule `DiscountType` and `DiscountValue`.
5. Discount type is ignored for DiscountManagerPlus-specific rule types that use tiers.

A Discount update event consumer keeps mapped PromotionRule metadata synced.

---

## Prevent Double-Discounting

No automatic suppression is applied. Admins should map only advanced DiscountManagerPlus rules (BOGO, combo, advanced conditions) and keep simple discounts in default nopCommerce to avoid overlap.

---

## Localization

Add resources for:

- DiscountManagerPlus discount card labels
- Toggle labels and hints
- Error messages for mapping and sync failures
- Button text

---

## Documentation Updates

1. Add `DEFAULT_DISCOUNT_INTEGRATION_CONTEXT.md` describing the design.
2. Add `DEFAULT_DISCOUNT_INTEGRATION_PLAN.md` for operational planning.
3. Update README index with links to new docs.

---

## Test Scenarios

1. Discount with DiscountManagerPlus mapping:
   - Rule creates and saves properly.
   - Discount metadata syncs to rule.
   - No overlap is introduced by mapping (advanced rule types only).

2. Rule evaluation:
   - Discount applies through DiscountManagerPlus in cart for advanced rules.
   - No double discount when default discounts are kept for simple cases only.

3. Admin UX:
   - DiscountManagerPlus card loads correctly on Discount Edit.
   - Products / tiers / conditions management works.

4. Mapping lifecycle:
   - Delete discount removes mapping and rule (optional config).
   - Unlink rule keeps rule inactive (decision needed).

---

## Open Decisions (default assumptions)

1. Deleting a Discount deletes the mapped PromotionRule.
2. Unlinking a rule leaves PromotionRule inactive (to avoid orphaned active rules).
3. Discount requirements remain usable for non-DiscountManagerPlus discounts.

---

## Rollout Steps

1. Add mapping entity + migration.
2. Add mapping service + admin controller.
3. Add Discount page widget card and AJAX endpoints.
4. Add event consumer to sync Discount -> PromotionRule.
5. Update docs + README index.

---

## Success Criteria

1. All DiscountManagerPlus rules can be created and edited from Discount page.
2. Default discounts remain active; DiscountManagerPlus is used only for advanced rules to avoid overlap.
3. Rule evaluation and storefront behavior remain unchanged for non-mapped discounts.
