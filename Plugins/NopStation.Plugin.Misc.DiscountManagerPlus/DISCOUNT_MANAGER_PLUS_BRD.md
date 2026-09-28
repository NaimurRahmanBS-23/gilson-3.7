# DiscountManager Plus Business Requirements Document (BRD)

## Product Name

DiscountManager Plus

## Platform

nopCommerce

## Version

1.0

------------------------------------------------------------------------

# 1. Executive Summary

DiscountManager Plus is a high-performance,
rule-based promotion management system designed for medium to
large-scale ecommerce businesses operating on nopCommerce.

The system enables businesses to:

-   Create complex promotional campaigns
-   Define rule-based conditional discounts
-   Configure multi-layer discount logic
-   Handle high-volume traffic and large product catalogs (300K+
    products)
-   Maintain enterprise-level performance, scalability, and security

This solution replaces basic nopCommerce discount logic with a flexible,
extensible DiscountManagerPlus.

------------------------------------------------------------------------

# 2. Business Goals & Objectives

## 2.1 Strategic Goals

-   Increase Average Order Value (AOV)
-   Improve conversion rate
-   Enable advanced marketing campaigns
-   Support multi-store & multi-vendor environments
-   Offer enterprise-level reliability and performance

## 2.2 Operational Goals

-   Reduce manual campaign configuration time
-   Allow marketing team to configure promotions without developer
    support
-   Support 100+ active rules simultaneously
-   Ensure cart performance remains under 100ms rule evaluation time

------------------------------------------------------------------------

# 3. Scope

## 3.1 In Scope

-   Advanced rule-based discount system
-   Combo pricing engine
-   Buy X Get Y logic
-   Attribute-based promotion rules
-   Cart-level conditional rules
-   Subtotal-based promotions
-   Multi-category / manufacturer rules
-   Enterprise performance optimization
-   Reporting & analytics

## 3.2 Out of Scope (Phase 1)

-   AI-based auto promotions
-   Loyalty reward system
-   Dynamic pricing based on AI
-   External ERP integration (optional Phase 2)

------------------------------------------------------------------------

# 4. Functional Requirements

## 4.1 Promotion Rule Engine

### 4.1.1 Product-Based Rules

Admin can configure:

Buy X products from: - Selected Products - Categories - Manufacturers -
Vendors

Then apply discount to: - Same products - Different selected products -
Entire cart

Supported discount types: - Percentage - Fixed amount - Fixed bundle
price - 100% discount (Free item)

------------------------------------------------------------------------

### 4.1.2 Combo (Bundle) Pricing

The system must allow:

-   Fixed bundle price override
-   Required combination logic
-   Optional combination logic
-   Minimum quantity per product
-   Multiple bundle tiers

Example: Laptop + Mouse + Bag → Fixed price = \$1000

------------------------------------------------------------------------

### 4.1.3 Attribute-Based Rules

Discount can be applied if:

-   Product has specific Specification Attribute
-   Product has specific Product Attribute Value
-   Attribute value quantity condition is met

------------------------------------------------------------------------

### 4.1.4 Quantity-Based Promotions

-   Discount if product quantity ≥ X
-   Discount if quantity within range (X--Y)
-   Tier-based quantity pricing

Example: - 5--10 units → 5% - 11--20 units → 10%

------------------------------------------------------------------------

### 4.1.5 Cart Condition Rules

Discount applied if:

-   Specific product INCLUDED
-   Specific product EXCLUDED
-   Cart contains products from multiple categories
-   Cart contains products from multiple manufacturers/vendors

------------------------------------------------------------------------

### 4.1.6 Subtotal-Based Rules

-   Cart subtotal \> X
-   Cart subtotal \< X
-   Subtotal range (X--Y)

------------------------------------------------------------------------

### 4.1.7 Buy X Get Y (BOGO)

-   Buy X quantity
-   Get Y product (Free / Discounted / Fixed price)

Supports: - Multiple reward tiers - Auto-add reward to cart (optional
toggle)

------------------------------------------------------------------------

# 5. Enterprise Performance Requirements

## 5.1 Performance Benchmarks

-   Rule evaluation time \< 100ms
-   Support 300K+ products
-   Support 100+ active rules
-   No noticeable delay on cart page

## 5.2 Optimization Requirements

-   In-memory rule caching
-   Indexed SQL queries
-   Precompiled rule conditions
-   No full product table scanning
-   Efficient LINQ expression building
-   Async processing support

------------------------------------------------------------------------

# 6. Scalability Requirements

Must support:

-   Multi-store
-   Multi-vendor
-   Large product catalog
-   High traffic (1000+ concurrent users)

Must work with:

-   Distributed cache (Redis)
-   Load-balanced environments

------------------------------------------------------------------------

# 7. Security Requirements

-   Follow nopCommerce plugin architecture
-   Use service-layer validation
-   Prevent SQL injection
-   Role-based admin access control
-   Audit logging for rule changes
-   Support GDPR compliance (no sensitive data stored)

------------------------------------------------------------------------

# 8. Reporting & Analytics

Admin dashboard must show:

-   Rule usage count
-   Revenue generated per rule
-   Discount amount given per rule
-   Conversion impact
-   Top performing promotions

Export formats: - CSV - Excel

------------------------------------------------------------------------

# 9. UI/UX Requirements

## Admin Panel

-   Clean enterprise UI
-   Advanced filtering
-   Searchable product selector
-   Drag-and-drop priority sorting
-   Real-time rule validation preview
-   Promotion conflict warning system

## Frontend

-   Promotion badge
-   Combo offer label
-   Automatic savings calculation display
-   Dynamic cart savings breakdown

------------------------------------------------------------------------

# 10. Architecture Requirements

System must follow:

-   Clean Architecture pattern
-   Dependency Injection
-   Repository pattern
-   Service layer separation
-   Rule Engine abstraction layer
-   Extensible rule provider interface

Must allow:

-   Future extension via custom rule providers
-   API exposure for mobile apps
-   Headless commerce compatibility

------------------------------------------------------------------------

# 11. Reliability & Maintainability

-   Backward compatibility support
-   Logging & monitoring
-   Exception handling with fallback
-   Unit-testable rule engine
-   Minimum 80% code coverage (recommended)

------------------------------------------------------------------------

# 12. Monetization Model (Enterprise)

Possible Business Models:

-   One-time License
-   Annual Subscription
-   SaaS Licensing per store
-   White-label for agencies

------------------------------------------------------------------------

# 13. Risk Analysis

  Risk                      Mitigation
  ------------------------- -------------------------------------
  Rule conflicts            Priority system + conflict detector
  Performance degradation   Caching + optimized queries
  Complex UI                Guided wizard mode
  Upgrade compatibility     Follow nopCommerce plugin standard

------------------------------------------------------------------------

# 14. Success Criteria

-   No cart slowdown
-   Increased AOV by at least 10%
-   Marketing team can create promotions without developers
-   Zero rule miscalculation
-   Enterprise client adoption

------------------------------------------------------------------------

# 15. Future Roadmap (Enterprise+)

-   AI-driven promotion suggestion
-   Real-time A/B testing engine
-   Behavioral-based discount logic
-   Customer segmentation engine
-   API-first microservice version
