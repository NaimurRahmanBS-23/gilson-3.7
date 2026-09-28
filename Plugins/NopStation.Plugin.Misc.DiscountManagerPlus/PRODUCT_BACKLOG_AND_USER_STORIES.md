# DiscountManagerPlus Product Backlog and User Stories

## 1. Product Vision and Goals

`NopStation.Plugin.Misc.DiscountManagerPlus` is the campaign execution layer for advanced promotions in nopCommerce.

Primary goals:

1. Let marketing/admin teams configure complex promotions without developer dependency.
2. Keep cart discount behavior deterministic and explainable.
3. Improve conversion, AOV, and promotion visibility for customers.
4. Provide analytics and usage evidence for promotion optimization.
5. Maintain safe performance under enterprise load with clear guardrails.

---

## 2. Personas

1. Promotion Admin
2. Marketing Manager
3. Merchandising Manager
4. Customer
5. QA/Support Engineer

---

## 3. Current Capability Baseline (What Exists)

Current implemented baseline:

1. Rule types: ProductBased, ComboPricing, BuyXGetY, CartCondition, SubtotalBased.
2. Admin CRUD for rules, products, tiers, and conditions.
3. Cart integration via shopping cart unit-price event consumer.
4. Explainability tools: conflict scanner and rule sandbox.
5. Storefront widgets: promotion badge, cart savings, public offers page.
6. Data visibility: usage history and analytics dashboard.
7. Guardrails: activation validation, request-level caching, timeout control.

---

## 4. Backlog Model and Scoring Rules (MoSCoW)

1. Must: required for safe production use.
2. Should: high value, near-term after Must.
3. Could: nice-to-have acceleration.
4. Won't (now): explicitly deferred.

Dependency decision policy:

1. Every Must and Should item has an explicit dependency decision.
2. Dependencies can reference story IDs, existing subsystem baseline, or nopCommerce contracts.

---

## 5. Epics List

1. `PE-EP-01` Rule Authoring, Guardrails, and Setup UX
2. `PE-EP-02` Rule Evaluation Integrity and Performance
3. `PE-EP-03` Explainability and Admin Decision Support
4. `PE-EP-04` Storefront Offer Visibility and Customer Clarity
5. `PE-EP-05` Usage Intelligence and Reporting
6. `PE-EP-06` Campaign Lifecycle and Operations
7. `PE-EP-07` Optimization and Experimentation

---

## 6. Must-Have Backlog Items

| Story ID | Title | Epic | Priority | Release | Dependency Decision |
|---|---|---|---|---|---|
| PE-US-001 | Rule activation guardrails by type | PE-EP-01 | Must | v1.x Stabilization | Validator + rule type matrix |
| PE-US-004 | BOGO setup assistant and reward-product UX clarity | PE-EP-01 | Must | v1.x Stabilization | PE-US-001 + tier editor stability |
| PE-US-005 | Combo pricing setup validation and explainability | PE-EP-01 | Must | v1.x Stabilization | PE-US-001 + combo evaluator |
| PE-US-006 | Condition group UX (AND/OR) with visual logic preview | PE-EP-01 | Must | v1.x Stabilization | Condition model baseline |
| PE-US-007 | Rule usage history parity with default nop discount expectations | PE-EP-05 | Must | v1.x Stabilization | Usage tracking + order/customer lookup |
| PE-US-008 | Analytics baseline accuracy | PE-EP-05 | Must | v1.x Stabilization | PE-US-007 data quality |
| PE-US-009 | Storefront offer visibility consistency | PE-EP-04 | Must | v1.x Stabilization | Widget integration + active rules |
| PE-US-010 | Performance guardrail visibility | PE-EP-02 | Must | v1.x Stabilization | Timeout setting + logging pipeline |

---

## 7. Should-Have Backlog Items

| Story ID | Title | Epic | Priority | Release | Dependency Decision |
|---|---|---|---|---|---|
| PE-US-011 | Export analytics/reporting (CSV/Excel) | PE-EP-05 | Should | v2.0 Expansion | PE-US-008 |
| PE-US-012 | Rule cloning and versioning workflow | PE-EP-06 | Should | v2.0 Expansion | PE-US-001 + CRUD stability |
| PE-US-013 | Draft/scheduled rule lifecycle states | PE-EP-06 | Should | v2.0 Expansion | PE-US-012 |
| PE-US-014 | Priority conflict recommendations | PE-EP-03 | Should | v2.0 Expansion | PE-US-002 |
| PE-US-015 | Rule simulation presets and saved test carts | PE-EP-03 | Should | v2.0 Expansion | PE-US-003 |
| PE-US-016 | Advanced targeting templates | PE-EP-01 | Should | v2.0 Expansion | PE-US-006 |
| PE-US-017 | Bulk product selection UX | PE-EP-01 | Should | v2.0 Expansion | PE-US-005 + selector baseline |

---

## 8. Could-Have Backlog Items

| Story ID | Title | Epic | Priority | Release | Dependency Decision |
|---|---|---|---|---|---|
| PE-US-018 | Campaign calendar view | PE-EP-06 | Could | v2.x+ | PE-US-013 |
| PE-US-019 | A/B promotion experiment flags | PE-EP-07 | Could | v2.x+ | PE-US-013 + PE-US-008 |
| PE-US-020 | Offer eligibility explanation widget | PE-EP-04 | Could | v2.x+ | PE-US-009 + PE-US-006 |
| PE-US-021 | Notification hooks for rule anomalies | PE-EP-06 | Could | v2.x+ | PE-US-010 + PE-US-008 |
| PE-US-022 | Promotion recommendation insights | PE-EP-07 | Could | v2.x+ | PE-US-008 + PE-US-011 |

---

## 9. Won't-Have-Now Backlog Items

| Backlog ID | Item | Priority | Deferred Reason | Revisit Trigger |
|---|---|---|---|---|
| PE-WN-001 | AI-driven auto-promotion generation | Won't (now) | Governance and explainability overhead | Stable v2 analytics and experimentation |
| PE-WN-002 | External ERP campaign sync | Won't (now) | External contract and support model required | Enterprise connector demand |
| PE-WN-003 | Real-time machine-learning discount optimization | Won't (now) | High risk and operational complexity | Mature experimentation pipeline |

---

## 10. Full User Stories Catalog with IDs

| Story ID | Title | Persona | Epic | Priority | Planned Slice |
|---|---|---|---|---|---|
| PE-US-001 | Rule activation guardrails by type | Promotion Admin | PE-EP-01 | Must | v1.x |
| PE-US-004 | BOGO setup assistant and reward-product UX clarity | Promotion Admin | PE-EP-01 | Must | v1.x |
| PE-US-005 | Combo pricing setup validation and explainability | Merchandising Manager | PE-EP-01 | Must | v1.x |
| PE-US-006 | Condition group UX with visual logic preview | Promotion Admin | PE-EP-01 | Must | v1.x |
| PE-US-007 | Rule usage history parity | Marketing Manager | PE-EP-05 | Must | v1.x |
| PE-US-008 | Analytics baseline accuracy | Marketing Manager | PE-EP-05 | Must | v1.x |
| PE-US-009 | Storefront offer visibility consistency | Customer | PE-EP-04 | Must | v1.x |
| PE-US-010 | Performance guardrail visibility | QA/Support Engineer | PE-EP-02 | Must | v1.x |
| PE-US-011 | Export analytics/reporting | Marketing Manager | PE-EP-05 | Should | v2.0 |
| PE-US-012 | Rule cloning and versioning workflow | Promotion Admin | PE-EP-06 | Should | v2.0 |
| PE-US-013 | Draft/scheduled lifecycle states | Promotion Admin | PE-EP-06 | Should | v2.0 |
| PE-US-014 | Priority conflict recommendations | Promotion Admin | PE-EP-03 | Should | v2.0 |
| PE-US-015 | Simulation presets and saved test carts | QA/Support Engineer | PE-EP-03 | Should | v2.0 |
| PE-US-016 | Advanced targeting templates | Marketing Manager | PE-EP-01 | Should | v2.0 |
| PE-US-017 | Bulk product selection UX | Merchandising Manager | PE-EP-01 | Should | v2.0 |
| PE-US-018 | Campaign calendar view | Marketing Manager | PE-EP-06 | Could | v2.x+ |
| PE-US-019 | A/B promotion experiment flags | Marketing Manager | PE-EP-07 | Could | v2.x+ |
| PE-US-020 | Offer eligibility explanation widget | Customer | PE-EP-04 | Could | v2.x+ |
| PE-US-021 | Notification hooks for anomalies | QA/Support Engineer | PE-EP-06 | Could | v2.x+ |
| PE-US-022 | Promotion recommendation insights | Marketing Manager | PE-EP-07 | Could | v2.x+ |

---

## 11. Acceptance Criteria per Story (Given/When/Then Format)

Each story includes: Story ID/title, persona, user story sentence, business value, priority, dependencies, at least 3 acceptance criteria, negative/edge cases, and test notes.
### PE-US-001 Rule activation guardrails by type

- Persona: Promotion Admin
- User story: As a Promotion Admin, I want activation guardrails by rule type so that non-working rules cannot be activated.
- Business value: Prevents production misconfiguration.
- Priority: Must
- Dependencies: Validator pipeline, rule type/discount type matrix.
- Acceptance criteria:
  1. Given BOGO without tiers, when Active is saved, then save is blocked with a tier-required message.
  2. Given ProductBased + FreeItem without reward product, when Active is saved, then save is blocked.
  3. Given CartCondition/SubtotalBased without conditions, when Active is saved, then save is blocked.
- Negative/edge cases: Inactive incomplete rules can be saved; store switch cannot bypass guardrail; valid configurations remain activatable.
- Test notes and measurable success criteria: 100% invalid activation attempts fail in regression suite.

### PE-US-004 BOGO setup assistant and reward-product UX clarity

- Persona: Promotion Admin
- User story: As a Promotion Admin, I want BOGO setup guidance and reward-product clarity so that Buy X Get Y rules are configured correctly.
- Business value: Fewer broken BOGO campaigns.
- Priority: Must
- Dependencies: PE-US-001, tier UI baseline.
- Acceptance criteria:
  1. Given BOGO type selected, when edit page loads, then assistant explicitly asks for products and tiers.
  2. Given reward product context, when product popup opens, then reward toggle behavior is clear.
  3. Given valid buy/reward/tier configuration, when active save occurs, then activation succeeds.
- Negative/edge cases: Missing buy products blocked; invalid tier ranges blocked; reward quantity zero blocked.
- Test notes and measurable success criteria: BOGO setup validation covers all tier variants.

### PE-US-005 Combo pricing setup validation and explainability

- Persona: Merchandising Manager
- User story: As a merchandiser, I want combo setup validation and clear discount explanation so that bundle pricing is financially correct.
- Business value: Protects margin.
- Priority: Must
- Dependencies: PE-US-001, combo evaluator.
- Acceptance criteria:
  1. Given fixed bundle price combo with complete set, when simulated, then discount equals subtotal minus bundle price.
  2. Given incomplete set, when evaluated, then combo rule does not apply.
  3. Given multiple complete sets, when evaluated, then discount scales by set count.
- Negative/edge cases: Bundle price above subtotal yields no discount; duplicate rows aggregate min qty; category/vendor combo rows match correctly.
- Test notes and measurable success criteria: 0 high-severity combo pricing defects in regression.

### PE-US-006 Condition group UX (AND/OR) with visual logic preview

- Persona: Promotion Admin
- User story: As a Promotion Admin, I want clear group logic controls and preview so that condition behavior is predictable.
- Business value: Reduces logic mistakes.
- Priority: Must
- Dependencies: Condition grouping model.
- Acceptance criteria:
  1. Given multiple rows in same group, when operators are set, then preview reflects AND/OR correctly.
  2. Given rows across groups, when saved, then final logic is OR across groups.
  3. Given first row in group, when listed, then it is marked as group start.
- Negative/edge cases: ConditionGroup <= 0 blocked; empty cart-condition criteria blocked; subtotal between requires valid min/max.
- Test notes and measurable success criteria: Preview and execution outcomes match in test matrix.

### PE-US-007 Rule usage history parity with default nop discount expectations

- Persona: Marketing Manager
- User story: As a marketing manager, I want rule usage history similar to default discount workflows so that reporting is trusted.
- Business value: Better campaign review.
- Priority: Must
- Dependencies: Usage write pipeline on order placement.
- Acceptance criteria:
  1. Given rule usage on order, when history grid loads, then order/customer/discount row appears.
  2. Given deleted order, when history grid loads, then deleted label is shown without broken edit link.
  3. Given multiple rows, when paging, then ordering and totals remain consistent.
- Negative/edge cases: Deleted customer handled; no-history rule shows empty grid; malformed references do not crash UI.
- Test notes and measurable success criteria: History row count reconciles with usage table count.
