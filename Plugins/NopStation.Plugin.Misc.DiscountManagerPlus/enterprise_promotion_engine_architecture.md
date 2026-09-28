# Enterprise DiscountManagerPlus -- Discount Extension Architecture

## 1. Design Principle

The **DiscountManagerPlus Plugin** must not replicate features that already
exist in the default **nopCommerce discount system**. Instead, the
plugin will function as an **extension layer** that enhances the
existing discount functionality by introducing **rule-based conditions
and requirement groups**.

The plugin will depend on the native nopCommerce discount architecture
and dynamically control **when, how, and for whom** those discounts are
applied.

This approach ensures: - Full compatibility with nopCommerce core
updates - Reduced system complexity - Reuse of existing discount logic -
High flexibility for marketing campaigns

------------------------------------------------------------------------

# 2. System Architecture

### Layered Architecture

**Layer 1 --- nopCommerce Core** - Discount Types - Coupon Codes - Buy X
Get Y - Assigned to Products - Assigned to Categories - Assigned to
Order Subtotal - Discount Limitations

**Layer 2 --- DiscountManagerPlus Plugin** - Rule Engine - Requirement
Groups - Customer Targeting - Campaign Conditions - Context-based Rules

**Layer 3 --- Promotion Execution** - Validation Engine - Rule
Evaluation - Discount Activation - Logging & Analytics

The plugin acts as a **rule controller**, not a discount creator.

------------------------------------------------------------------------

# 3. Rule Engine Concept

The DiscountManagerPlus introduces **dynamic rule evaluation** that
determines if an existing nopCommerce discount can be applied.

### Rule Categories

**Customer Context** - Customer Role - Customer Segment - First-time
Buyer - Purchase History - Lifetime Spend

**Order Context** - Cart Subtotal - Product Category - Manufacturer -
Cart Quantity

**Session Context** - Device Type - App vs Web - Campaign Source -
Referral Source

**Time Context** - Campaign Period - Flash Sale Window - Time-based
activation

------------------------------------------------------------------------

# 4. Requirement Groups

Requirement Groups allow administrators to create **logical rule
combinations**.

Supported Logic:

-   Group 1 (AND)
-   Group 2 (OR)
-   Nested Conditions

Example Structure:

Discount → Requirement Group

Group 1 - Customer Role = VIP - Cart Subtotal \> \$150

Group 2 - Customer Role = Wholesale - Purchase Count ≥ 5

If **any group evaluates TRUE**, the discount becomes eligible.

------------------------------------------------------------------------

# 5. Example Use Cases

## Example 1 --- Subtotal Discount with Role Restriction

**Default nopCommerce discount:**

-   Discount Type: Assigned to Order Subtotal
-   Condition: Subtotal ≥ \$200
-   Discount: 10%

**DiscountManagerPlus Rules:**

Requirement Group - Customer Role = Wholesale - Country = Germany -
Customer Lifetime Spend \> \$500

Result:

Only **Wholesale customers in Germany with sufficient purchase history**
receive the subtotal discount.

------------------------------------------------------------------------

## Example 2 --- Buy X Get Y with Campaign Rules

**Default nopCommerce discount:**

-   Buy 2 Product A
-   Get 1 Product B Free

**DiscountManagerPlus Conditions:**

Requirement Group 1 - Customer Role = VIP - Cart Subtotal \> \$100

Requirement Group 2 - Customer has placed ≥ 3 previous orders - Device
Type = Mobile

Result:

Promotion activates only if the **rule groups pass validation**.

------------------------------------------------------------------------

## Example 3 --- Campaign-based Promotion

**Default Discount:**

-   Discount: 15% off Category "Electronics"

**DiscountManagerPlus Rules:**

-   Campaign = Black Friday
-   Customer Segment = Returning Customer
-   Time Window = 10PM -- 2AM

Result:

The discount is dynamically activated **only during the campaign window
for returning customers**.

------------------------------------------------------------------------

# 6. Plugin Dependency Model

The plugin should **directly depend on nopCommerce discount entities**.

Primary Entities:

-   Discount
-   DiscountRequirement
-   Customer
-   Order
-   ShoppingCart

The plugin extends:

DiscountRequirementRule

Custom rule providers can be added dynamically.

Example:

-   CustomerRoleRequirementRule
-   PurchaseHistoryRequirementRule
-   GeoLocationRequirementRule
-   DeviceTypeRequirementRule

------------------------------------------------------------------------

# 7. Dynamic Rule Evaluation Flow

1.  Customer adds item to cart
2.  nopCommerce detects available discounts
3.  DiscountManagerPlus intercepts discount evaluation
4.  Rule Engine evaluates requirement groups
5.  If rule validation passes → Discount applied
6.  Otherwise → Discount ignored

------------------------------------------------------------------------

# 8. Performance Considerations

For enterprise-scale stores:

-   Rule evaluation must stay under **50--100ms**
-   Use **cached rule sets**
-   Precompile rule trees
-   Avoid repeated database queries

Recommended techniques:

-   In-memory caching
-   Expression trees
-   Rule indexing

------------------------------------------------------------------------

# 9. Advantages of This Architecture

-   No duplication of nopCommerce features
-   Easy integration with existing discounts
-   Highly flexible marketing campaigns
-   Scalable for large catalogs
-   Future-proof plugin design

------------------------------------------------------------------------

# 10. Future Extensions

-   AI-driven promotions
-   Customer behavior targeting
-   Real-time campaign triggers
-   A/B promotion testing
-   Marketing automation integration
