# DiscountManagerPlus: Professional Promotion Guide

## Executive Summary

The DiscountManagerPlus plugin enables sophisticated promotional strategies across your e-commerce storefront. This guide covers five primary rule categories that support the most common business promotion scenarios:

1. **Subtotal-Based Rules** – Promotions triggered by cart spending thresholds
2. **Product-Based Rules** – Promotions triggered by specific product purchases
3. **Combo Pricing Rules** – Special pricing for product bundles
4. **Cart Condition Rules** – Promotions triggered by cart, customer, and context conditions
5. **Buy X Get Y (BOGO) Rules** – Tier-based rewards that improve with purchase quantity

Each rule type supports flexible discount models including percentage discounts, fixed-amount discounts, and free product rewards.

### Complete Feature Overview

| Feature Area | What It Covers |
|---|---|
| **Rule management** | Create, edit, delete, prioritize, and deactivate promotion rules |
| **Product-based promotions** | Category, manufacturer, vendor, and product-driven discounts |
| **Combo pricing** | Fixed bundle price, percentage, and fixed amount bundle discounts |
| **Cart conditions** | Product, category, vendor, country, payment, role, coupon, quantity, and order history conditions |
| **Subtotal rules** | Cart subtotal thresholds and range-based discounts |
| **BOGO and tiers** | Quantity tiers, escalating rewards, and free-item logic |
| **Reward handling** | Auto-add rewards, reward selection modal, and reward attributes |
| **Storefront features** | Promotion badge, offers page, cart savings breakdown, reward UI |
| **Governance** | Priority, exclusivity, date ranges, usage limits, and analytics |
| **Integration** | Linked nopCommerce discount support and native requirement sync |

---

## Part 1: Subtotal-Based Rules

### 1.1 Purpose

Subtotal-Based Rules apply promotions when customers reach specific spending thresholds, regardless of which products they purchase. This strategy encourages larger order values and simplifies promotion messaging.

**Common Use Cases:**
- Spend $50+ to receive 10% off
- Free shipping when cart exceeds $100
- Tiered discounts: spend $50 (10% off), spend $100 (15% off), spend $200 (20% off)
- Seasonal promotions with minimum order values

### 1.2 How It Works

When a customer adds items to their cart, the plugin:

1. Calculates the total cart subtotal (sum of all item prices × quantities)
2. Compares the subtotal against configured thresholds
3. If the threshold is met, applies the specified discount or reward
4. Updates the cart in real-time to reflect savings

**Key Characteristics:**
- Evaluated automatically—no coupon codes required
- Applies to the entire cart when conditions are met
- Can stack with other non-exclusive rules
- Savings appear in the cart breakdown

### 1.3 Configuration Options

| Option | Description | Example |
|--------|-------------|---------|
| **Operator** | Comparison method | `GreaterThan`, `LessThan`, `Between`, `EqualTo` |
| **Threshold Amount** | Spending target | `$100.00` |
| **Range (for "Between")** | Min and max values | `$50.00 - $200.00` |
| **Discount Type** | Reward method | `Percentage` (10%), `FixedAmount` ($15) |
| **Discount Value** | Amount or percentage | `10%` or `$15.00` |
| **Priority** | Execution order | `1` (runs first), `5` (runs fifth) |

### 1.4 Supported Features

| Feature | Subtotal-Based | Notes |
|---------|---|---|
| **Percentage Discount** | ✓ | Apply X% off the entire cart |
| **Fixed Discount** | ✓ | Apply $X off the entire cart |
| **Free Product Reward** | ✗ | Not supported for subtotal rules |
| **Reward Selection Modal** | ✗ | Customers cannot choose rewards |
| **Auto-Add Rewards** | ✗ | Not applicable |
| **Multi-Store Support** | ✓ | Configure per store |
| **Date Range Limits** | ✓ | Set start and end dates |
| **Exclusivity** | ✓ | Prevent other rules from stacking |

### 1.5 Step-by-Step Admin Configuration

**Navigate to:** `Admin > NopStation > DiscountManagerPlus > Promotion Rules > Create`

#### Step 1: Basic Information
1. Enter **Rule Name**: "Minimum Order Discount" or similar
2. Enter **System Name**: "min-order-discount-50" (no spaces, lowercase)
3. Select **Rule Type**: Choose "Subtotal-Based"
4. Select **Discount Type**: Choose "Percentage" or "FixedAmount"
5. Enter **Discount Value**: e.g., `10` (for 10%) or `15.00` (for $15 fixed)

#### Step 2: Rule Settings
1. Set **Priority**: Assign a unique number (lower = runs earlier)
2. Toggle **Active**: Enable the rule
3. Toggle **Exclusive** (optional): Check to prevent other rules from applying
4. Set **Date Range** (optional): Specify start/end dates for limited campaigns
5. Select **Stores**: Choose which stores this rule applies to
6. Click **Save**

#### Step 3: Add Condition
1. Scroll to **Conditions** section
2. Click **Add New Condition**
3. In the popup:
   - **Operator**: Select "GreaterThan" (spends more than)
   - **Amount**: Enter `50.00`
4. Click **Save Condition**

#### Step 4: Verify and Activate
1. Review the rule summary
2. Click **Save** again to finalize
3. Test in storefront: Add items totaling >$50 to cart
4. Verify discount appears in cart summary

**Pro Tip:** Use the "Between" operator to create tiered discounts:
- Rule 1: `Between $50-$99` → 5% off
- Rule 2: `Between $100-$199` → 10% off
- Rule 3: `GreaterThan $200` → 15% off

### 1.6 Example Scenarios

#### Scenario A: Black Friday Minimum Order Promotion
- **Goal:** Drive larger orders during Black Friday (Nov 28 - Dec 1)
- **Rule Type:** Subtotal-Based
- **Operator:** GreaterThan $75
- **Discount:** 20% off
- **Date Range:** 2024-11-28 to 2024-12-01
- **Priority:** 1 (highest)
- **Result:** Any cart over $75 receives 20% off automatically

#### Scenario B: Q1 Loyalty Tiered Discount
- **Goal:** Encourage repeat customers with scaling rewards
- **Create 3 Rules:**

  | Rule | Operator | Amount | Discount | Exclusive |
  |------|----------|--------|----------|-----------|
  | Rule 1 | GreaterThan | $50 | 5% off | No |
  | Rule 2 | Between | $100-$250 | 10% off | No |
  | Rule 3 | GreaterThan | $250 | 15% off | No |

- **Note:** Set different priorities (1, 2, 3) so only the highest tier applies
- **Activation:** Automatic based on current cart subtotal

#### Scenario C: Free Shipping Threshold
- **Goal:** Free shipping for orders over $99
- **Rule Type:** Subtotal-Based
- **Operator:** GreaterThan $99
- **Discount Type:** FixedAmount
- **Discount Value:** $9.99 (shipping fee amount)
- **Store Scope:** All stores
- **Result:** Cart shows "-$9.99 Shipping" when subtotal exceeds $99

### 1.7 Limitations and Conditions

| Limitation | Impact | Workaround |
|-----------|--------|-----------|
| Cannot select specific products | Discount applies universally | Use Product-Based rules if product selection needed |
| No reward product option | Only discounts allowed | Combine with BuyXGetY rule for free items |
| Cart subtotal only | Doesn't include shipping/tax | Confirm store's subtotal definition |
| Single threshold per rule | Must create multiple rules for tiers | Create separate rules with different operators |
| Automatic application | Customers cannot opt-out | This is intentional for guaranteed visibility |

---

## Part 2: Product-Based Rules

### 2.1 Purpose

Product-Based Rules apply promotions when customers purchase specific products or product combinations. This allows targeted discounting based on inventory strategy, cross-sell opportunities, or seasonal product promotion.

**Common Use Cases:**
- Clearance: "All T-Shirts 30% off"
- Bundling: "Buy 2 units of Product X, get 20% off"
- Cross-sell: "Buy Product A, get 10% off Product B"
- New product launch: "New item first 100 purchases receive $5 off"
- Category-based: "All electronics 15% off"

### 2.2 How It Works

When a customer adds a specific product to their cart:

1. Plugin detects the product in the cart
2. Checks if the purchase quantity meets the rule's minimum requirement
3. If condition is met, applies the discount to that product line
4. Calculates savings based on the discount type and value

**Key Characteristics:**
- Targeted to specific products or categories
- Can require minimum quantities (e.g., "buy 2+ units")
- Supports percentage and fixed-amount discounts
- Can reward with free products (Premium Feature)
- Works with product attribute selection (size, color, etc.)

### 2.3 Configuration Options

| Option | Description | Example |
|--------|-------------|---------|
| **Target Product(s)** | Which products trigger the rule | Product ID #45 (T-Shirt) |
| **Min Quantity** | Minimum units required | `2` (buy 2 to qualify) |
| **Discount Type** | Reward method | `Percentage`, `FixedAmount`, `FreeItem` |
| **Discount Value** | Amount or percentage | `20%` or `$10.00` |
| **Reward Product (FreeItem only)** | Free product to add | Product ID #88 (Socks) |
| **Priority** | Execution order | `1` (runs first) |
| **Exclusivity** | Block other rules | Enable to prevent stacking |

### 2.4 Supported Features

| Feature | Product-Based | Notes |
|---------|---|---|
| **Percentage Discount** | ✓ | Apply X% off the target product(s) |
| **Fixed Discount** | ✓ | Apply $X off each unit |
| **Free Product Reward** | ✓ | Auto-add or customer-selectable |
| **Reward Selection Modal** | ✓ | Customers choose from eligible rewards |
| **Auto-Add Rewards** | ✓ | Automatically add free item to cart |
| **Reward Attributes** | ✓ | Customers select size, color, options |
| **Multi-Store Support** | ✓ | Configure per store |
| **Date Range Limits** | ✓ | Set promotional period |
| **Exclusivity** | ✓ | Prevent competing rules |

### 2.5 Step-by-Step Admin Configuration

**Navigate to:** `Admin > NopStation > DiscountManagerPlus > Promotion Rules > Create`

#### Step 1: Basic Information
1. Enter **Rule Name**: "T-Shirt Summer Sale" or similar
2. Enter **System Name**: "tshirt-summer-sale" (lowercase, no spaces)
3. Select **Rule Type**: Choose "Product-Based"
4. Select **Discount Type**: 
   - Choose "Percentage" for % off
   - Choose "FixedAmount" for $X off
   - Choose "FreeItem" for free product reward
5. Enter **Discount Value**: e.g., `20` (20%) or `15.00` ($15)

#### Step 2: Rule Settings
1. Set **Priority**: Unique number (lower = earlier execution)
2. Toggle **Active**: Enable the rule
3. Toggle **Exclusive** (optional): Prevent other rules from stacking
4. Set **Date Range** (optional): Campaign start/end dates
5. Select **Stores**: Which stores apply
6. Click **Save**

#### Step 3: Configure Products
1. Scroll to **Products** section
2. Click **Add New Product**
3. In the popup:
   - **Product**: Search and select the target product (e.g., "Blue T-Shirt")
   - **Min Quantity**: Enter `2` (e.g., "buy 2 to get discount")
   - **Is Reward Product**: Leave unchecked (this is the product being discounted)
4. Click **Save Product**
5. Repeat for additional products if needed

#### Step 4: Configure Free Item (if Discount Type = "FreeItem")
1. Click **Add New Product** again
2. In the popup:
   - **Product**: Select the reward/free product (e.g., "Socks")
   - **Min Quantity**: Enter `1`
   - **Is Reward Product**: Check this box ✓
3. Click **Save Product**

#### Step 5: Configure Reward Behavior (if Free Item)
1. Scroll to **Reward Settings** section (if visible)
2. Choose **Reward Behavior**:
   - **Auto-Add**: Free item automatically added to cart
   - **Customer Selection**: Modal popup lets customer choose which free item
3. If selection modal enabled:
   - **Max Selections**: How many free items customer can choose
   - **Reward Per Quantity**: Free items granted per target quantity
4. Click **Save**

#### Step 6: Verify and Activate
1. Review rule summary: Target product, minimum quantity, discount/reward
2. Click **Save** to finalize
3. Test in storefront:
   - Add 2+ units of target product to cart
   - Verify discount/free item appears
   - If selection modal: verify popup shows reward options

**Pro Tip for Free Item Rewards:**
- Enable "Customer Selection" to let buyers choose which free item variant (size, color, etc.)
- This increases engagement and reduces cart abandonment

### 2.6 Example Scenarios

#### Scenario A: Clearance Bundle – Buy 2 Get Discount
- **Goal:** Clear old inventory by encouraging multiple purchases
- **Rule Type:** Product-Based
- **Target Product:** "Red T-Shirt (Season 2023)" - Product ID #45
- **Min Quantity:** 2
- **Discount Type:** FixedAmount
- **Discount Value:** $10 off per unit (so 2 units = $20 total)
- **Result:** Customer buys 2 red T-shirts → receives $20 discount automatically

#### Scenario B: Cross-Sell with Free Product
- **Goal:** Bundle popular products together
- **Rule Type:** Product-Based
- **Target Product:** "Laptop (Model X)" - Product ID #120
- **Min Quantity:** 1
- **Discount Type:** FreeItem
- **Reward Product:** "USB Cable" - Product ID #201
- **Reward Behavior:** Auto-Add
- **Result:** Customer buys laptop → USB cable automatically added free to cart

#### Scenario C: Premium Reward Selection
- **Goal:** Let customers choose free reward when buying high-value product
- **Rule Type:** Product-Based
- **Target Product:** "Premium Phone Case" - Product ID #88
- **Min Quantity:** 1
- **Discount Type:** FreeItem
- **Reward Products:** Three options—
  - "Screen Protector" (ID #201)
  - "Phone Stand" (ID #202)
  - "Cleaning Cloth Pack" (ID #203)
- **Reward Behavior:** Customer Selection Modal
- **Max Selections:** 1 (choose one free item)
- **Result:** Customer buys phone case → modal appears offering choice of 3 free accessories

#### Scenario D: Multi-Product Bundle with Percentage Discount
- **Goal:** Discount a specific product only
- **Rule Type:** Product-Based
- **Target Products:** 
  - "Winter Jacket" - Min Quantity 1
  - "Wool Hat" - Min Quantity 1
- **Discount Type:** Percentage
- **Discount Value:** 15%
- **Condition:** Only apply when both items in cart
- **Result:** If customer has both items → 15% off the jacket

### 2.7 Limitations and Conditions

| Limitation | Impact | Workaround |
|-----------|--------|-----------|
| Target product must be explicitly selected | Cannot wildcard entire categories | Use multiple rules for category (one per product) |
| Free item is static | Same reward for all customers | Create multiple rules with different rewards |
| Requires minimum quantity | Cannot discount single unit purchases | Set Min Quantity to 1 |
| Selection modal requires eligibility | Customers see modal only when rule qualifies | Clear communication in product description helps |
| Reward attributes locked at rule creation | Cannot change reward options after launch | Create new rule with updated reward options |
| Cannot tier rewards | Same discount regardless of quantity bought | Use BuyXGetY rule type for tiered rewards |

---

## Part 3: Combo Pricing Rules

### 3.1 Purpose

Combo Pricing Rules apply special pricing when customers purchase specific product combinations. This enables bundle offers, kit pricing, and "buy together and save" promotions that encourage larger basket sizes and increase average order value.

**Common Use Cases:**
- "Laptop + Mouse + Case Bundle: $1,299 (save $200)"
- "Complete Home Office Kit: Monitor + Keyboard + Chair for $599"
- "Breakfast Bundle: Coffee + Croissants + Pastries for $15.99"
- "Summer Starter Pack: Sunscreen + Hat + Sunglasses for $49.99"
- "Back-to-School Bundle: Backpack + Pencils + Notebook for $34.99"

### 3.2 How It Works

When a customer has multiple specified products in their cart:

1. Plugin detects each required product in the cart
2. Calculates how many complete "sets" of the combo are eligible
3. Applies the combo discount only to complete sets
4. Remaining units (if any) are not discounted
5. Savings appear as a combo discount line in cart summary

**Example:**
- Combo requires: Laptop (1) + Mouse (1) + Case (1)
- Customer cart: Laptop × 2, Mouse × 2, Case × 2
- **Complete sets:** 2 (customer has all combos 2 times)
- **Discount:** Applied to both complete sets; extra units unaffected

**Key Characteristics:**
- All specified products must be present in cart
- Discount applies only to complete sets
- Supports three discount models: Fixed Bundle Price, Percentage, or Fixed Amount
- High-visibility in cart (clearly labeled as "Bundle Discount")
- Works for any number of product combinations

### 3.3 Configuration Options

| Option | Description | Example |
|--------|-------------|---------|
| **Combo Products** | List of required items | Laptop + Mouse + Case |
| **Min Quantity per Product** | Required count of each | 1 laptop, 1 mouse, 1 case |
| **Discount Type** | Pricing method | `FixedBundlePrice`, `Percentage`, `FixedAmount` |
| **Discount Value** | Bundle price or % off | `1299.00` or `15%` or `50.00` |
| **Priority** | Execution order | `1` (runs first) |
| **Exclusivity** | Block other rules | Enable to prevent stacking |

### 3.4 Discount Type Explanations

#### Type A: Fixed Bundle Price
- **Use When:** You want to set an exact price for the complete bundle
- **How It Works:** If combo subtotal is > bundle price, customer pays bundle price
- **Example:** Laptop ($999) + Mouse ($49) + Case ($50) = $1,098 normally → **Bundle Price: $999**
- **Benefit:** Clear, simple pricing; easy to communicate

#### Type B: Percentage Discount
- **Use When:** You want a % off the combo subtotal
- **How It Works:** Applies X% discount to sum of combo items
- **Example:** Laptop ($999) + Mouse ($49) + Case ($50) = $1,098 → **10% off = -$109.80 savings**
- **Benefit:** Scales with product prices; flexible for seasonal adjustments

#### Type C: Fixed Amount Discount
- **Use When:** You want a fixed dollar amount off the combo
- **How It Works:** Applies $X discount to the combo total
- **Example:** Laptop ($999) + Mouse ($49) + Case ($50) = $1,098 → **$100 off = $998 final**
- **Benefit:** Marketing clarity; easy to communicate savings

### 3.5 Supported Features

| Feature | Combo Pricing | Notes |
|---------|---|---|
| **Fixed Bundle Price** | ✓ | Set exact price for combo |
| **Percentage Discount** | ✓ | Apply X% off combo subtotal |
| **Fixed Discount** | ✓ | Apply $X off combo subtotal |
| **Free Product Reward** | ✗ | Cannot add free items (only pricing) |
| **Reward Selection Modal** | ✗ | Not applicable for bundle pricing |
| **Multi-Unit Combos** | ✓ | Support 2-unit, 3-unit, or more combos |
| **Multi-Store Support** | ✓ | Configure per store |
| **Date Range Limits** | ✓ | Seasonal bundling |
| **Exclusivity** | ✓ | Prevent competing rules |

### 3.6 Step-by-Step Admin Configuration

**Navigate to:** `Admin > NopStation > DiscountManagerPlus > Promotion Rules > Create`

#### Step 1: Basic Information
1. Enter **Rule Name**: "Summer Office Bundle" or similar
2. Enter **System Name**: "summer-office-bundle" (lowercase, no spaces)
3. Select **Rule Type**: Choose "Combo Pricing"
4. Select **Discount Type**:
   - Choose "FixedBundlePrice" to set exact price
   - Choose "Percentage" for % off
   - Choose "FixedAmount" for $X off
5. Enter **Discount Value**: e.g., `1199.00` (bundle price), `15` (15%), or `100.00` ($100)

#### Step 2: Rule Settings
1. Set **Priority**: Unique number (lower = earlier execution)
2. Toggle **Active**: Enable the rule
3. Toggle **Exclusive** (optional): Prevent other combos from stacking
4. Set **Date Range** (optional): Campaign period
5. Select **Stores**: Which stores apply
6. Click **Save**

#### Step 3: Add Combo Products
1. Scroll to **Products** section
2. Click **Add New Product** for first combo item:
   - **Product**: Search "Monitor" (example)
   - **Min Quantity**: `1`
   - **Is Reward Product**: Leave unchecked
3. Click **Save Product**
4. Repeat for each combo item:
   - Click **Add New Product**
   - **Product**: "Keyboard"
   - **Min Quantity**: `1`
   - Click **Save Product**
5. Continue for third item (Chair, Desk, etc.)

**Pro Tip:** Add products in the order you want them to appear in combo display (e.g., most expensive first).

#### Step 4: Verify Configuration
1. Review the Products section: Should show all 3-4 items with Min Quantity 1 each
2. Check Discount Value is set correctly:
   - Fixed Bundle Price: `1499.00` (what customer pays for entire combo)
   - Or Percentage: `20` (20% off combo subtotal)
   - Or Fixed Amount: `250.00` ($250 off combo)
3. Click **Save** to finalize

#### Step 5: Test in Storefront
1. Add one of each combo item to cart (e.g., monitor + keyboard + chair)
2. Verify combo discount appears in cart summary
3. Verify savings amount is correct
4. Test with multiple combos (e.g., 2 of each item) → should show 2 combo discounts
5. Test with partial combos (e.g., monitor + keyboard only) → should NOT apply discount

**Validation:** If partial combos don't receive discount, that's correct behavior—all items required.

### 3.7 Example Scenarios

#### Scenario A: Technology Bundle with Fixed Price
- **Goal:** Create attractive tech starter kit with fixed price point
- **Rule Type:** Combo Pricing
- **Combo Products:**
  - Monitor (24") - Min Qty: 1
  - Keyboard (Mechanical) - Min Qty: 1
  - Mouse (Wireless) - Min Qty: 1
- **Discount Type:** FixedBundlePrice
- **Bundle Price:** $399.99
  - (Normal prices: Monitor $199 + Keyboard $89 + Mouse $39 = $327, but bundle at $399 since high-end)
  - Actually: Monitor $299 + Keyboard $149 + Mouse $79 = $527 normal → Bundle at $399 = $128 savings
- **Priority:** 2
- **Result:** When customer has all 3 items → "Technology Bundle: $399.99" (save $128)

#### Scenario B: Seasonal Gift Bundle with Percentage Off
- **Goal:** Holiday bundle with flexible discount as prices change
- **Rule Type:** Combo Pricing
- **Combo Products:**
  - Winter Jacket - Min Qty: 1
  - Wool Hat - Min Qty: 1
  - Gloves - Min Qty: 1
  - Scarf - Min Qty: 1
- **Discount Type:** Percentage
- **Discount Value:** 20% off combo subtotal
- **Date Range:** Nov 1 - Dec 25
- **Priority:** 3
- **Result:** Any customer with all 4 winter items → 20% off total (recalculated daily based on prices)

#### Scenario C: Multi-Unit Bulk Combo
- **Goal:** Incentivize larger volume purchases
- **Rule Type:** Combo Pricing
- **Combo Products:**
  - Bulk Coffee Beans (1kg) - Min Qty: 2
  - Coffee Filter Pack - Min Qty: 2
  - Coffee Mug - Min Qty: 2
- **Discount Type:** FixedAmount
- **Discount Value:** $15.00 off per combo set
- **Note:** If customer has 2 of each → 1 combo discount applies. If customer has 4 of coffee but 2 of filters/mugs → only 1 combo discount (limited by filters/mugs).
- **Result:** Encourages bulk purchases while managing inventory

#### Scenario D: Premium Back-to-School Bundle
- **Goal:** Attract families shopping for multiple students
- **Rule Type:** Combo Pricing
- **Combo Products:**
  - Backpack - Min Qty: 1
  - Pencil Set - Min Qty: 1
  - Notebook Set - Min Qty: 1
  - Lunchbox - Min Qty: 1
- **Discount Type:** FixedBundlePrice
- **Bundle Price:** $69.99
  - (Normal: Backpack $29.99 + Pencils $12.99 + Notebooks $14.99 + Lunchbox $19.99 = $77.96)
  - Save $7.97
- **Exclusivity:** Enabled (prevent other rules from stacking)
- **Result:** Clear, simple pricing for bulk school shopping

### 3.8 Limitations and Conditions

| Limitation | Impact | Workaround |
|-----------|--------|-----------|
| All combo items required | Partial combos get no discount | Educate customers on complete combo requirement |
| No free item option | Cannot add bonus products | Use Product-Based rule for free items |
| Scales by complete sets | 2 items with 1 of each = 1 discount only | This is intentional; clearly explain set requirements |
| Cannot vary by customer segment | Same pricing for all | Create separate combos for VIP vs regular |
| Discount appears as one line | Cannot show individual discounts per item | Transparency is good—shows bundle value |
| Cannot nest combos | Cannot have "bundle of bundles" | Create rule-of-rules structure or manual workaround |

---

## Part 4: Buy X Get Y (BOGO) Rules

### 4.1 Purpose

Buy X Get Y (BOGO) Rules enable tier-based reward promotions where customers receive rewards based on purchase quantities. This is the most flexible rule type for incentivizing bulk purchases with graduated rewards. Rewards improve as customers buy more units.

**Common Use Cases:**
- "Buy 2 Get 1 Free" – purchase 2 units, receive 1 free
- "Buy 3+ Get 20% Off" – tiered discounts based on quantity
- "Bulk Pricing" – different prices at different quantity thresholds (1-5 units: $10, 6-10 units: $8, 11+: $6)
- "Volume Rewards" – buy more, get better rewards (units 1-3: $1 off each, 4-6: $2 off each, 7+: $3 off each)
- "Loyalty Tiers" – regular customers get escalating rewards with each purchase tier
- "Free Gift Escalation" – buy 2 get socks free, buy 5 get socks + shirt free, buy 10 get complete outfit free

### 4.2 How It Works

BOGO rules operate on a tier-based system tied to purchase quantities:

1. Customer adds product(s) to cart
2. Plugin detects the purchase quantity
3. Plugin identifies which tier(s) the quantity qualifies for
4. Applies the corresponding reward for each tier reached
5. Rewards can be discounts, free products, or price reductions
6. Multiple tiers can stack if quantity exceeds each tier's minimum

**Tier Structure Example:**
- **Tier 1:** Buy 1-2 units → 5% discount
- **Tier 2:** Buy 3-5 units → 10% discount + free sample
- **Tier 3:** Buy 6+ units → 15% discount + free gift box + priority shipping

**Key Characteristics:**
- Tier-driven: each quantity range has its own reward
- Progressive: rewards improve as quantity increases
- Flexible: supports multiple reward types (discounts, free products, attributes)
- Transparent: customers can see what they'll earn at each tier
- Auto-scalable: quantity-based automatic eligibility

### 4.3 Configuration Options

| Option | Description | Example |
|--------|-------------|---------|
| **Buy Product** | Product(s) customer must purchase | Product ID #45 (T-Shirt) |
| **Tiers** | Quantity-based reward levels | Min Qty 2, Max Qty 5 → 10% off |
| **Min Quantity (per tier)** | Minimum units for this tier | `2` (starts at 2 units) |
| **Max Quantity (per tier)** | Maximum units for this tier | `5` (tier ends at 5 units) |
| **Discount Type (per tier)** | Reward method | `Percentage`, `FixedAmount`, `FreeItem` |
| **Discount Value (per tier)** | Amount or percentage | `10%` or `5.00` per unit |
| **Reward Product (FreeItem)** | Free product to add | Product ID #88 (Socks) |
| **Priority** | Execution order | `1` (runs first) |

### 4.4 Supported Features

| Feature | BOGO | Notes |
|---------|---|---|
| **Percentage Discount** | ✓ | Applied per unit or to total |
| **Fixed Discount** | ✓ | $X off per unit or per set |
| **Free Product Reward** | ✓ | Auto-add free item at tier threshold |
| **Reward Selection Modal** | ✓ | Customers choose free product variant |
| **Reward Attributes** | ✓ | Select size, color of free item |
| **Tiered Escalation** | ✓ | Different rewards per quantity tier |
| **Auto-Add Rewards** | ✓ | Automatically add free items |
| **Multi-Store Support** | ✓ | Configure per store |
| **Date Range Limits** | ✓ | Seasonal BOGO campaigns |
| **Exclusivity** | ✓ | Prevent competing rules |

### 4.5 Step-by-Step Admin Configuration

**Navigate to:** `Admin > NopStation > DiscountManagerPlus > Promotion Rules > Create`

#### Step 1: Basic Information
1. Enter **Rule Name**: "Buy 2 Get Free Socks" or similar
2. Enter **System Name**: "buy2-free-socks" (lowercase, no spaces)
3. Select **Rule Type**: Choose "Buy X Get Y"
4. Note: **Discount Type and Value fields are NOT used for BOGO** – these are configured per tier instead

#### Step 2: Rule Settings
1. Set **Priority**: Unique number (lower = earlier execution)
2. Toggle **Active**: Enable the rule
3. Toggle **Exclusive** (optional): Prevent other rules from stacking
4. Set **Date Range** (optional): Campaign period
5. Select **Stores**: Which stores apply
6. Click **Save**

#### Step 3: Configure Buy Products
1. Scroll to **Products** section
2. Click **Add New Product**:
   - **Product**: Search and select the product to buy (e.g., "Blue T-Shirt")
   - **Min Quantity**: Leave blank or `0` (this is handled by tiers)
   - **Is Reward Product**: Leave unchecked (this is the product customer buys)
3. Click **Save Product**
4. Repeat for additional "buy" products if this rule applies to multiple items

#### Step 4: Add Tiers (Critical Step)
1. Scroll to **Tiers** section
2. Click **Add New Tier** for Tier 1:
   - **Min Quantity**: `2` (minimum units to qualify)
   - **Max Quantity**: `5` (tier applies from 2-5 units)
   - **Discount Type**: Choose:
     - "Percentage" (e.g., 10% off)
     - "FixedAmount" (e.g., $2 off per unit)
     - "FreeItem" (free product reward)
   - **Discount Value**: Enter amount (e.g., `10` for 10%, or `2.00` for $2)
   - **Reward Product** (if FreeItem selected): Choose free product
   - Click **Save Tier**

3. Click **Add New Tier** for Tier 2:
   - **Min Quantity**: `6` (next tier starts at 6)
   - **Max Quantity**: `10`
   - **Discount Type**: "Percentage"
   - **Discount Value**: `15` (better reward than tier 1)
   - Click **Save Tier**

4. Click **Add New Tier** for Tier 3 (optional):
   - **Min Quantity**: `11`
   - **Max Quantity**: `999` (no upper limit)
   - **Discount Type**: "Percentage"
   - **Discount Value**: `20` (best reward)
   - Click **Save Tier**

**Pro Tip:** Always ensure tiers don't overlap:
- Tier 1: Min 1, Max 5
- Tier 2: Min 6, Max 10
- Tier 3: Min 11, Max 999

#### Step 5: Configure Free Item Behavior (if using FreeItem)
1. If any tier uses "FreeItem" discount type:
2. Scroll to **Reward Settings** section (if visible)
3. Choose **Reward Behavior**:
   - **Auto-Add**: Free item automatically added when tier qualifies
   - **Customer Selection**: Modal popup lets customer choose variant
4. Click **Save**

#### Step 6: Test and Verify
1. Test in storefront with different quantities:
   - Add 2 units → verify Tier 1 discount applies
   - Add 6 units → verify Tier 2 discount applies (or new discount on extra units)
   - Add 12 units → verify Tier 3 discount applies
2. Verify discount amounts are calculated correctly
3. If free item: verify reward product added to cart
4. If selection modal: verify popup appears at correct tier

**Key Testing Notes:**
- BOGO typically applies to a specific quantity count
- Some implementations apply the discount to all units once the tier is reached
- Others apply escalating discounts (tier 1 units get tier 1 discount, tier 2 units get tier 2 discount)
- Confirm your store's configuration by testing in checkout

### 4.6 Example Scenarios

#### Scenario A: Classic Buy 2 Get 1 Free
- **Goal:** Encourage bulk purchases of popular item
- **Rule Type:** Buy X Get Y
- **Buy Product:** "Blue T-Shirt" (Product ID #45)
- **Tiers:**
  - Tier 1: Min 2, Max 2 → FreeItem "Socks" (auto-add)
  - Tier 2: Min 3, Max 5 → FreeItem "Socks" + 10% off
  - Tier 3: Min 6+ → FreeItem "Socks" + 15% off + free shipping
- **Result:** 
  - Customer buys 2 shirts → gets 1 free socks pair
  - Customer buys 3 shirts → gets socks + 10% off
  - Customer buys 6 shirts → gets socks + 15% off

#### Scenario B: Tiered Volume Pricing
- **Goal:** Reduce per-unit cost as customer buys more
- **Rule Type:** Buy X Get Y
- **Buy Product:** "Coffee Beans (1kg)" (Product ID #120)
- **Tiers:**
  - Tier 1: Min 1, Max 3 → 0% (regular price)
  - Tier 2: Min 4, Max 8 → FixedAmount $2.00 off per unit
  - Tier 3: Min 9, Max 15 → FixedAmount $3.50 off per unit
  - Tier 4: Min 16+ → FixedAmount $5.00 off per unit
- **Result:**
  - Buy 1-3 bags: Regular price
  - Buy 4-8 bags: $2 off each bag
  - Buy 9+ bags: $3.50 off each bag
  - Buy 16+ bags: $5 off each bag

#### Scenario C: Multi-Tier with Escalating Free Gifts
- **Goal:** Premium reward escalation for loyal bulk buyers
- **Rule Type:** Buy X Get Y
- **Buy Product:** "Premium Coffee" (Product ID #88)
- **Tiers:**
  - Tier 1: Min 2, Max 3 → FreeItem "Coffee Cup" (auto-add)
  - Tier 2: Min 4, Max 6 → FreeItem "Coffee Cup" + FreeItem "Stirring Spoon"
  - Tier 3: Min 7+ → FreeItem "Premium Gift Box" (with cup, spoon, chocolate)
- **Reward Behavior:** Auto-Add (gifts automatically added when tier qualifies)
- **Result:**
  - Buy 2 coffees → free cup
  - Buy 4 coffees → free cup + spoon
  - Buy 7 coffees → complete gift box set (premium presentation)

#### Scenario D: Back-to-School Bundle Tiers
- **Goal:** Encourage students to buy multiple supplies
- **Rule Type:** Buy X Get Y
- **Buy Products:** 
  - "Notebook Pack" (Product ID #201)
  - "Pencil Set" (Product ID #202)
  - Combined quantity counts toward tier
- **Tiers:**
  - Tier 1: Min 2 items, Max 4 → 5% off entire purchase
  - Tier 2: Min 5 items, Max 8 → 10% off + FreeItem "Backpack" (auto-add)
  - Tier 3: Min 9+ items → 15% off + FreeItem "Backpack" + FreeItem "Lunch Box"
- **Result:**
  - Buy 2+ items: 5% discount
  - Buy 5+ items: 10% discount + free backpack
  - Buy 9+ items: 15% discount + backpack + lunch box

#### Scenario E: Subscription-Style Tiering
- **Goal:** Reward repeat customer loyalty
- **Rule Type:** Buy X Get Y
- **Buy Product:** "Monthly Coffee Subscription Box"
- **Tiers:**
  - Tier 1: Min 1 box → No extra reward (regular price)
  - Tier 2: Min 3 boxes → 5% off per box + free specialty blend sample
  - Tier 3: Min 6 boxes → 10% off per box + free specialty blend + free shipping
  - Tier 4: Min 12 boxes → 15% off per box + free gift box every 3 months
- **Result:** Long-term subscribers get progressively better deals and gifts

### 4.7 Limitations and Conditions

| Limitation | Impact | Workaround |
|-----------|--------|-----------|
| Tiers must be sequential | Cannot have gaps in tiers | Plan tiers carefully; cover all quantity ranges |
| Cannot create "negative" tiers | Cannot penalize low quantities | Offer low/no discount at base tier instead |
| Free item reward same across tiers | Same free product for all tiers | Create separate BOGO rules with different products |
| Requires explicit tier configuration | Tiers don't auto-generate | Must manually create each tier level |
| Quantity-based only | Cannot tier by date/customer role | Use multiple rules for different customer segments |
| One buy product per rule | Cannot create "any 2 items" combos | Use multiple BOGO rules for different products |
| Max quantity limit must be set | Open-ended tiers are limited | Use very high Max (e.g., 999) for unlimited tier |

---

## 4.8 Complete BOGO Feature Guide: All Configurable Options

BOGO rules support **15+ configurable features** across tiers and reward settings. Here's the complete breakdown:

### Feature Category 1: Core Tier Configuration (Required)

These features must be configured for every tier:

| Feature | Description | Usage | Example |
|---------|-------------|-------|---------|
| **Min Quantity** | Minimum units to trigger tier | Required per tier | Tier 1: Min = 2 |
| **Max Quantity** | Maximum units for this tier | Required per tier | Tier 1: Max = 5 |
| **Discount Type** | Reward mechanism | Choose 1 per tier | Percentage, FixedAmount, or FreeItem |
| **Discount Value** | Amount or % to apply | Required per tier | 10% or $5.00 per unit |

**Feature Combinations for Core Configuration:**
- **2 Tiers:** Basic tiering (e.g., Buy 1-5 get 5%, Buy 6+ get 10%)
- **3 Tiers:** Standard escalation (e.g., 5%, 10%, 15%)
- **4+ Tiers:** Advanced volume pricing (e.g., $10, $8, $6, $5, $3 per unit)

### Feature Category 2: Free Product Rewards (Optional)

When you select **FreeItem** as the discount type:

| Feature | Description | Options | Impact |
|---------|-------------|---------|--------|
| **Reward Product** | Which product(s) customer gets free | Single product per tier | Specific free item |
| **Reward Quantity** | How many free units per tier | 1, 2, 3, or more | "Buy 5 get 2 free items" |
| **Reward Per Quantity** | How many free per X units purchased | Configure per tier | "For every 3 bought, 1 free" |

**Feature Combinations for Free Products:**
- Single tier with 1 free item (simplest)
- Multi-tier with 1 free + increasing discounts
- Escalating free quantities (Tier 1: 1 free, Tier 2: 2 free, Tier 3: 3 free)
- Mixed rewards (Tier 1: 1 free socks, Tier 2: 1 free socks + $2 off, Tier 3: 2 free socks)

### Feature Category 3: Reward Selection & Presentation (Optional)

Controls how customers interact with free rewards:

| Feature | Description | Setting | Behavior |
|---------|-------------|---------|----------|
| **Reward Behavior** | How reward is added | Auto-Add or Selection | Automatic vs. customer choice |
| **Selection Modal** | Popup for choosing reward | Enabled/Disabled | "Pick your free item" dialog |
| **Max Selections** | How many options customer can pick | 1, 2, 3, etc. | Limit choices per tier |
| **Reward Product Options** | Variants customer can choose | Multiple products | Choose socks, shirt, or hat |

**Feature Combinations for Selection:**
- **Auto-Add Only:** Reward automatically added, no choices
- **Selection Modal, 1 Choice:** Pick 1 free item from 3 options
- **Selection Modal, Multi-Choice:** Pick 2 free items (e.g., shirt AND socks)
- **Selection Modal with Attributes:** Choose product AND size/color

### Feature Category 4: Reward Attributes (Optional)

When selecting free products, customers can customize:

| Feature | Description | Example | Applicability |
|---------|-------------|---------|---|
| **Product Variant** | Different versions of same product | Red socks vs. Blue socks | All product types |
| **Size Selection** | Size option for reward | XS, S, M, L, XL | Apparel, shoes |
| **Color Selection** | Color choice for reward | Black, Blue, Red, Green | Most products |
| **Material Selection** | Material option | Cotton, Polyester, Wool | Apparel, textiles |
| **Custom Attributes** | Any product attribute | Volume, style, flavor | Depends on product setup |

**Feature Combinations for Attributes:**
- **No Attributes:** Simple free product (same version for all)
- **Single Attribute:** Choose size OR color
- **Multiple Attributes:** Choose size AND color for reward
- **Nested Modal:** Select product, then select variants

---

## 4.9 Advanced BOGO Configuration Patterns

### Pattern 1: Pure Discount Tiers (Percentage or Fixed)
No free products, just escalating discounts.

**Setup:**
```
Tier 1: Min 1, Max 3  → 0% (no discount)
Tier 2: Min 4, Max 6  → 10% off per unit
Tier 3: Min 7, Max 10 → 15% off per unit
Tier 4: Min 11+       → 20% off per unit
```

**Features Used:** 4 tiers, percentage discounts, no rewards  
**Use Case:** Volume-based bulk pricing without free items  
**Customer Experience:** Lower per-unit price as quantity increases

---

### Pattern 2: Simple Free Product (Auto-Add)
One free product automatically added at tier threshold.

**Setup:**
```
Tier 1: Min 1, Max 1  → No reward (regular price)
Tier 2: Min 2, Max 2  → FreeItem "Socks" (1 unit)
Tier 3: Min 3+        → FreeItem "Socks" (1 unit) + 10% off
```

**Features Used:** 3 tiers, free product rewards, auto-add behavior  
**Feature Count:** 4 features (tiers × 2, free product, auto-add)  
**Use Case:** "Buy 2 Get 1 Free" style promotions  
**Customer Experience:** Automatic gift at checkout (no choices)

---

### Pattern 3: Free Product Selection Modal
Customers choose which free product to receive.

**Setup:**
```
Tier 1: Min 3, Max 5
  → FreeItem with Selection Modal
  → Choose 1 from: [Socks, Hat, Gloves]
  
Tier 2: Min 6+
  → FreeItem with Selection Modal
  → Choose 1 from: [Socks, Hat, Gloves, Scarf]
```

**Features Used:** 2 tiers, free product, selection modal, max selections = 1  
**Feature Count:** 5 features (tiers × 2, free product, modal, selection limit)  
**Use Case:** Let customers pick reward they prefer  
**Customer Experience:** Modal pops up showing 3-4 free options to choose from

---

### Pattern 4: Multi-Choice Free Products
Customers can select multiple free products per tier.

**Setup:**
```
Tier 1: Min 5, Max 9
  → FreeItem with Selection Modal
  → Choose 2 from: [Socks, Hat, Gloves, Scarf, Headband]
  
Tier 2: Min 10+
  → FreeItem with Selection Modal
  → Choose 3 from: [Socks, Hat, Gloves, Scarf, Headband]
```

**Features Used:** 2 tiers, free products, selection modal, max selections = 2-3  
**Feature Count:** 6 features (tiers × 2, free products, modal, multi-select)  
**Use Case:** Premium rewards for high-volume buyers  
**Customer Experience:** "Choose any 2 free accessories with your purchase"

---

### Pattern 5: Free Product with Attributes
Customers choose product AND customize variants.

**Setup:**
```
Tier 1: Min 2, Max 4
  → FreeItem "T-Shirt" with Selection Modal + Attributes
  → Choose 1 shirt variant (Color: Red/Blue/Black) 
                           (Size: XS/S/M/L/XL)
  
Tier 2: Min 5+
  → FreeItem "T-Shirt" + "Socks" with Selection Modal + Attributes
  → Choose shirt color/size
  → Choose socks color
```

**Features Used:** 2 tiers, free products, selection modal, attributes  
**Feature Count:** 7 features (tiers × 2, products, modal, attributes × 2)  
**Use Case:** Personalized free rewards  
**Customer Experience:** "Pick your free shirt: [Color selector] [Size selector]"

---

### Pattern 6: Mixed Discount & Reward Tiers
Some tiers offer discounts, others offer free products.

**Setup:**
```
Tier 1: Min 1, Max 2
  → 5% off per unit (Percentage discount)
  
Tier 2: Min 3, Max 5
  → FreeItem "Socks" (Free product reward)
  
Tier 3: Min 6+
  → 10% off per unit (Percentage discount)
```

**Features Used:** 3 tiers, mixed discount types (percentage + free)  
**Feature Count:** 6 features (tiers × 2, discount types × 2, products)  
**Use Case:** Varied reward strategy  
**Customer Experience:** Different rewards at different quantities

---

### Pattern 7: Escalating Free Product Quantities
More free products as customer buys more.

**Setup:**
```
Tier 1: Min 2, Max 3
  → FreeItem "Socks" × 1 (1 unit)
  
Tier 2: Min 4, Max 6
  → FreeItem "Socks" × 2 (2 units)
  
Tier 3: Min 7, Max 10
  → FreeItem "Socks" × 3 (3 units)
  
Tier 4: Min 11+
  → FreeItem "Socks" × 5 (5 units)
```

**Features Used:** 4 tiers, free product with increasing quantities  
**Feature Count:** 8 features (tiers × 2, product, quantity escalation × 4)  
**Use Case:** Gift escalation programs  
**Customer Experience:** "Buy more, get more free items" (1 → 2 → 3 → 5)

---

### Pattern 8: Tiered Free Gift Boxes
Different "gift packages" at different quantity thresholds.

**Setup:**
```
Tier 1: Min 2, Max 3
  → FreeItem "Basic Gift Box" (contains 1 item)
  
Tier 2: Min 4, Max 7
  → FreeItem "Premium Gift Box" (contains 3 items)
  
Tier 3: Min 8+
  → FreeItem "Deluxe Gift Box" (contains 5 items + exclusive item)
```

**Features Used:** 3 tiers, free product tiers (different gift box levels)  
**Feature Count:** 6 features (tiers × 2, gift box levels)  
**Use Case:** Reward escalation with packaging appeal  
**Customer Experience:** Unboxing experience improves at higher tiers

---

### Pattern 9: Subscription-Style Tiering
Progressively better rewards for subscription/membership tiers.

**Setup:**
```
Tier 1: Min 1 purchase → Regular price + 0% off
Tier 2: Min 3 purchases → 5% off + free item A
Tier 3: Min 6 purchases → 10% off + free items A + B
Tier 4: Min 12 purchases → 15% off + free items A + B + exclusive item
```

**Features Used:** 4 tiers, escalating discounts, escalating free items  
**Feature Count:** 10+ features (tiers, discounts, products, escalation logic)  
**Use Case:** Loyalty/subscription programs  
**Customer Experience:** Rewards improve as commitment level increases

---

### Pattern 10: Conditional Free Products (Different Product per Tier)
Each tier unlocks a different free product.

**Setup:**
```
Tier 1: Min 2, Max 4
  → FreeItem "Socks" (auto-add)
  
Tier 2: Min 5, Max 8
  → FreeItem "T-Shirt" (auto-add) [different product]
  
Tier 3: Min 9+
  → FreeItem "Jacket" (auto-add) [different product]
```

**Features Used:** 3 tiers, different free products per tier  
**Feature Count:** 6 features (tiers × 2, product selection × 3)  
**Use Case:** "Unlock new gifts at each tier"  
**Customer Experience:** Progression through unlock system

---

## 4.10 Complete Feature Reference Table for BOGO

| Feature | Configuration | Options/Range | Per-Tier or Global | Complexity |
|---------|---|---|---|---|
| **Rule Name** | Text | 1-100 chars | Global | Low |
| **Rule Type** | Dropdown | "Buy X Get Y" (fixed) | Global | Low |
| **Priority** | Number | 1-999 | Global | Low |
| **Active Status** | Toggle | On/Off | Global | Low |
| **Exclusive** | Toggle | Yes/No | Global | Low |
| **Date Range Start** | Date picker | Any future date | Global | Low |
| **Date Range End** | Date picker | Any future date | Global | Low |
| **Store Scope** | Multi-select | All stores, or specific | Global | Low |
| **Buy Product** | Search & select | Any product ID | Per-rule | Medium |
| **Tier Min Qty** | Number | 1-9999 | Per-tier | Low |
| **Tier Max Qty** | Number | 1-9999 | Per-tier | Low |
| **Discount Type** | Dropdown | Percentage / FixedAmount / FreeItem | Per-tier | Medium |
| **Discount Value** | Number | 0.00-9999.99 | Per-tier | Medium |
| **Reward Product** | Search & select | Any product ID (if FreeItem) | Per-tier | Medium |
| **Reward Quantity** | Number | 1-100 units | Per-tier | Medium |
| **Reward Per Qty** | Number | 1-10 (per X units) | Per-tier | Medium |
| **Reward Behavior** | Dropdown | Auto-Add / Selection | Per-tier or global | Medium |
| **Selection Modal** | Toggle | On/Off | If reward = FreeItem | Medium |
| **Max Selections** | Number | 1-10 | Per-tier | Medium |
| **Reward Attributes** | Multi-select | Size, Color, Material, etc. | Per-tier | High |
| **Attribute Options** | List | Size: XS/S/M/L/XL | Per-tier | High |

**Total Possible Features: 22 configurable options**

---

## 4.11 Feature Capability Matrix: BOGO Use Case Coverage

| Use Case | Features Required | Tier Config | Reward Type | Complexity | Example |
|----------|---|---|---|---|---|
| **Simple Volume Discount** | 4 (rule + tier qty + discount type + value) | 1-4 tiers | Percentage/Fixed | Low | "Buy 5+ get 10% off" |
| **Buy X Get Y Free** | 6 (+ reward product + auto-add) | 2 tiers | FreeItem | Low | "Buy 2 get 1 free" |
| **Customer Selects Reward** | 8 (+ modal + max selections) | 2-3 tiers | FreeItem + Modal | Medium | "Buy 3, choose 1 free" |
| **Reward with Size/Color** | 10 (+ attributes) | 2-3 tiers | FreeItem + Attributes | Medium | "Buy 2, choose shirt (size + color)" |
| **Escalating Free Products** | 8 (+ qty escalation) | 3-4 tiers | FreeItem × quantities | Medium | "Buy 2 get 1, buy 5 get 3, buy 10 get 5" |
| **Multi-Product Selection** | 10 (+ multi-select) | 2-3 tiers | FreeItem + Multi-choice | High | "Buy 5, choose any 2 free items" |
| **Tiered Gift Boxes** | 9 (+ product levels) | 3 tiers | FreeItem (different products) | High | "Bronze/Silver/Gold gift boxes" |
| **Subscription Loyalty** | 12+ (+ escalation logic) | 4+ tiers | Mixed discounts + rewards | High | "Regular/Silver/Gold/Platinum tiers" |

---

## 4.12 BOGO Configuration Scenarios by Industry

### Retail / Apparel Industry
**Features Setup:** 4-6 tiers, mix of discounts and free items, selection modal with size/color
```
Example: "Buy 1 Shirt ($25) → Regular price
          Buy 2 Shirts → 10% off each
          Buy 3 Shirts → Free socks (choose color)
          Buy 5 Shirts → 20% off + free jacket (choose size/color)"
```
**Total Features Used:** ~12 (tiers, discounts, free items, attributes)

### Electronics / Tech Industry
**Features Setup:** 3-4 tiers, high-value free items, auto-add rewards
```
Example: "Buy 1 Laptop ($999) → Regular price
          Buy 2 Laptops → Free mouse pad
          Buy 3 Laptops → Free mouse pad + $50 off each
          Buy 5+ Laptops → Free laptop bag + $100 off each + free support"
```
**Total Features Used:** ~10 (tiers, discounts, escalating free items)

### Grocery / Food Industry
**Features Setup:** 5-6 tiers, per-unit pricing escalation, no free items
```
Example: "Buy 1-3 bags → $10/bag
          Buy 4-8 bags → $8.50/bag
          Buy 9-15 bags → $7.50/bag
          Buy 16+ bags → $6.50/bag"
```
**Total Features Used:** ~8 (tiers × 4, fixed amount discounts)

### Beauty / Cosmetics Industry
**Features Setup:** 3-4 tiers, selection modal, multiple free product options, attributes
```
Example: "Buy 2 Skincare → Free moisturizer (choose scent: lavender/rose/unscented)
          Buy 5 Skincare → Choose 2 free: [moisturizer, serum, mask] with scent selection
          Buy 10 Skincare → VIP gift box (exclusive items) + 20% off"
```
**Total Features Used:** ~14 (tiers, selection modal, multi-choice, attributes)

### Subscription Boxes
**Features Setup:** 4+ tiers, escalating rewards, tiered free items
```
Example: "Month 1-3 → Regular price ($39)
          Month 4-6 → 5% off + free bonus item
          Month 7-12 → 10% off + free bonus items + exclusive quarterly gift
          Month 12+ → 15% off + choose 2 gifts + VIP status"
```
**Total Features Used:** ~16 (tiers, escalating discounts, escalating rewards, selection)

---

## 4.13 Step-by-Step: Creating an Advanced BOGO (Multi-Feature)

**Goal:** "Buy 3+ Get Free Shirt (Customer Chooses Size/Color) + 10% off"

**Step-by-Step:**

1. **Create Rule**
   - Name: "T-Shirt Volume Tier"
   - Type: "Buy X Get Y"
   - Active: ✓

2. **Add Product (What to Buy)**
   - Product: "Classic T-Shirt"
   - Min Qty: 1

3. **Add Tier 1 (Basic)**
   - Min: 1, Max: 2
   - Discount Type: "Percentage"
   - Discount Value: 5%

4. **Add Tier 2 (With Free Item + Discount)**
   - Min: 3, Max: 5
   - Discount Type: "FreeItem"
   - Reward Product: "Premium T-Shirt"
   - Reward Qty: 1
   - Reward Behavior: **Selection Modal** ✓
   - Max Selections: 1

5. **Configure Reward Attributes**
   - Enable Size Selection: XS, S, M, L, XL, XXL
   - Enable Color Selection: Black, White, Navy, Gray, Red

6. **Add Tier 3 (Best Reward)**
   - Min: 6, Max: 999
   - Discount Type: "Percentage"
   - Discount Value: 10%
   - Note: Can add FreeItem here too if desired

7. **Save and Test**
   - Test with 1 shirt → 5% off
   - Test with 3 shirts → Modal appears to choose free shirt (size + color)
   - Test with 6 shirts → 10% off all (+ selection modal still available)

**Total Features Configured: 11**
- Rule name, type, active (3)
- Buy product (1)
- 3 tiers with min/max (6)
- Tier 1 discount type/value (2)
- Tier 2 free item + modal (3)
- Tier 2 attributes (2 - size + color)
- Tier 3 discount (1)

---

## Summary: BOGO Feature Capabilities

| Aspect | Minimum Features | Standard Features | Advanced Features | Maximum Features |
|--------|---|---|---|---|
| **Tiers** | 1 | 2-3 | 4-5 | 6+ |
| **Discount Types** | 1 | 2 | 3 | All (%, Fixed, Free) |
| **Free Products** | 0 | 1 | 2-3 | 5+ |
| **Selection Modal** | No | Optional | Yes, 1 choice | Yes, multi-choice |
| **Attributes** | None | None | 1-2 (Size/Color) | 3+ (Size/Color/Material) |
| **Reward Quantity** | Static | Static | Escalating | Fully escalating |
| **Configuration Time** | 5 mins | 15 mins | 30 mins | 45+ mins |
| **Admin Complexity** | Low | Medium | High | Very High |
| **Customer Experience** | Basic discount | "Buy More Save" | "Choose & Customize" | "Premium VIP" |
| **Revenue Impact** | +5-10% AOV | +15-25% AOV | +25-40% AOV | +40-60% AOV |

---

## Feature Matrix: Which Features Work With Which Rule Types

| Feature | Subtotal | Product | Combo | Cart Condition | BOGO | Notes |
|---------|----------|---------|-------|----------------|------|-------|
| **Percentage Discount** | ✓ | ✓ | ✓ | ✓ | ✓ | Available on all rule types |
| **Fixed Amount Discount** | ✓ | ✓ | ✓ | ✓ | ✓ | Available on all rule types |
| **Fixed Bundle Price** | ✗ | ✗ | ✓ | ✗ | ✗ | Combo pricing only |
| **Free Product (Auto-Add)** | ✗ | ✓ | ✗ | ✗ | ✓ | Product-based and BOGO |
| **Free Product (Selection Modal)** | ✗ | ✓ | ✗ | ✗ | ✓ | Product-based and BOGO |
| **Reward Attributes** | ✗ | ✓ | ✗ | ✗ | ✓ | Product-based and BOGO |
| **Tiered Escalation** | ✗ | ✗ | ✗ | ✗ | ✓ | BOGO only |
| **Condition Groups (AND/OR)** | ✗ | ✗ | ✗ | ✓ | ✗ | Cart condition and subtotal condition logic |
| **Product / Category / Vendor Conditions** | ✗ | ✗ | ✗ | ✓ | ✗ | Cart condition source rules |
| **Customer / Country / Payment Conditions** | ✗ | ✗ | ✗ | ✓ | ✗ | Cart condition context rules |
| **Coupon / Order Count / Quantity Conditions** | ✗ | ✗ | ✗ | ✓ | ✗ | Cart condition eligibility rules |
| **Same-Line Match** | ✗ | ✗ | ✗ | ✓ | ✗ | Cart condition line-scoping |
| **Multi-Store Support** | ✓ | ✓ | ✓ | ✓ | ✓ | All rule types |
| **Date Range Limits** | ✓ | ✓ | ✓ | ✓ | ✓ | All rule types |
| **Exclusivity Control** | ✓ | ✓ | ✓ | ✓ | ✓ | All rule types |
| **Priority Ordering** | ✓ | ✓ | ✓ | ✓ | ✓ | All rule types |

---

## Part 5: Cart Condition Rules

### 5.1 Purpose

Cart Condition Rules apply a discount only when the cart, customer, or request context matches the configured conditions. This rule type is the most flexible option when the promotion must depend on who is shopping, what is in the cart, or how the order is being placed.

**Common Use Cases:**
- VIP customer discount when cart contains eligible products
- Country-specific promotion for US and Canada customers
- Payment-method discount for prepaid or card orders
- First-order-only campaign for new customers
- Coupon-gated promotion with cart quantity requirements
- Device, channel, campaign, or referral-based campaigns

### 5.2 How It Works

When the cart is evaluated, the plugin:

1. Loads the rule conditions from the admin setup.
2. Evaluates each condition group using AND / OR logic.
3. Checks cart-scoped values such as subtotal, quantity, and qualifying products.
4. Checks customer-scoped values such as role, order count, and new-customer status.
5. Checks request-scoped values such as country, payment method, device type, channel, campaign source, and referral source.
6. Applies the discount only if the full condition tree is satisfied.

**Key Characteristics:**
- Requires at least one condition
- Supports cart-level percentage and fixed amount discounts
- Can combine multiple condition groups
- Can use both product-source checks and customer-context checks in the same rule
- Can scope matching to the same cart line when needed

### 5.3 Supported Discount Types

| Discount Type | Supported | Notes |
|---|---|---|
| **Percentage** | ✓ | Apply percentage to the cart or matched scope |
| **FixedAmount** | ✓ | Apply a fixed amount to the cart or matched scope |
| **FreeItem** | ✗ | CartCondition does not use reward product flow |
| **FixedBundlePrice** | ✗ | Bundle pricing belongs to Combo Pricing |

### 5.4 Condition Features

There are **15 condition features** in this rule family, plus **11 source types** used by source-based conditions.

| Condition Feature | Description | Example |
|---|---|---|
| **Product condition** | Match specific products in the cart | Product ID 124 must be present |
| **Category condition** | Match products from a category | Shoes category only |
| **Manufacturer condition** | Match products from a manufacturer | Nike products only |
| **Vendor condition** | Match products from a vendor | Vendor A products only |
| **Customer role condition** | Match customer role or group | VIP customers only |
| **Country condition** | Match billing or request country | US, CA, UK |
| **Payment method condition** | Match selected payment systems | Credit Card, PayPal |
| **Coupon code condition** | Require a valid coupon or code list | SAVE10, VIP20 |
| **Order count condition** | Match previous completed orders | First order only, or 3+ orders |
| **Cart quantity condition** | Match quantity range | 2 to 5 items |
| **Subtotal condition** | Match cart subtotal range | $50 to $150 |
| **Same-line match** | Require all checks to match one line item | Product + attribute on same row |
| **Condition groups** | Group conditions with AND / OR | Group A AND Group B, or either group |
| **Parent-child tree** | Nest conditions for advanced logic | Parent rule with multiple children |
| **Source-based condition** | Use source types for dynamic matching | Attribute values, expiry days, campaign source |

### 5.4.1 All Condition Rules at a Glance

#### Cart and Customer Condition Rules

1. Product condition
2. Category condition
3. Manufacturer condition
4. Vendor condition
5. Customer role condition
6. Country condition
7. Payment method condition
8. Coupon code condition
9. Order count condition
10. Cart quantity condition
11. Subtotal condition
12. Same-line match
13. Condition groups (AND/OR)
14. Parent-child tree
15. Source-based condition

#### Source Types Used by Conditions

1. Products
2. Categories
3. Manufacturers
4. Vendors
5. SpecificationAttributeOptions
6. ProductAttributeValues
7. ExpiryDays
8. DeviceType
9. SalesChannel
10. CampaignSource
11. ReferralSource

### 5.5 Condition Source Types

| Source Type | Use Case |
|---|---|
| **Products** | Exact product matching |
| **Categories** | Category-driven campaigns |
| **Manufacturers** | Brand-driven promotions |
| **Vendors** | Vendor-specific offers |
| **SpecificationAttributeOptions** | Filter by product specification options |
| **ProductAttributeValues** | Filter by selected product attributes |
| **ExpiryDays** | Time-sensitive or expiry-based logic |
| **DeviceType** | Desktop, mobile, or tablet targeting |
| **SalesChannel** | Storefront or channel targeting |
| **CampaignSource** | UTM or marketing campaign targeting |
| **ReferralSource** | Referrer-based targeting |

### 5.6 Step-by-Step Admin Configuration

**Navigate to:** `Admin > NopStation > DiscountManagerPlus > Promotion Rules > Create`

#### Step 1: Basic Information
1. Enter **Rule Name**: "VIP Cart Discount" or similar
2. Enter **System Name**: "vip-cart-discount"
3. Select **Rule Type**: Choose "CartCondition"
4. Select **Discount Type**: Choose "Percentage" or "FixedAmount"
5. Enter **Discount Value**: e.g., `10` or `25.00`

#### Step 2: Rule Settings
1. Set **Priority**: Unique number (lower = earlier execution)
2. Toggle **Active**: Enable the rule
3. Toggle **Exclusive** (optional): Prevent other rules from stacking
4. Set **Date Range** (optional): Campaign start/end dates
5. Select **Stores**: Which stores apply
6. Click **Save**

#### Step 3: Add Conditions
1. Scroll to **Conditions** section
2. Click **Add New Condition**
3. Choose the condition source:
  - Product, Category, Manufacturer, Vendor
  - Customer Role, Country, Payment Method
  - Coupon Code, Order Count, Cart Quantity, Subtotal
  - Device Type, Sales Channel, Campaign Source, Referral Source
4. Choose the operator or comparison mode:
  - Equals, Contains, GreaterThan, LessThan, Between, Any, All
5. Enter the required source data or range values
6. Save the condition

#### Step 4: Build Condition Logic
1. Add more conditions if needed
2. Group them with AND / OR logic
3. Use parent-child nesting for advanced trees
4. Enable same-line match when the rule must target one cart row

#### Step 5: Verify and Activate
1. Review the rule summary
2. Click **Save** again to finalize
3. Test the cart with matching and non-matching cases
4. Verify discount appears only when the full condition tree matches

### 5.7 Example Scenarios

#### Scenario A: VIP Country-Based Discount
- **Goal:** Give 15% off to VIP customers in approved countries
- **Rule Type:** CartCondition
- **Conditions:** VIP role, country is US or CA
- **Discount:** 15% off
- **Result:** Only VIP customers from those countries qualify

#### Scenario B: First Order Coupon Campaign
- **Goal:** Encourage first purchase with a coupon-gated reward
- **Rule Type:** CartCondition
- **Conditions:** First order only, coupon code `WELCOME10`
- **Discount:** $10 off
- **Result:** Only new customers using the coupon qualify

#### Scenario C: Payment Method Campaign
- **Goal:** Promote prepaid orders
- **Rule Type:** CartCondition
- **Conditions:** Payment method is Credit Card or PayPal
- **Discount:** 5% off
- **Result:** Rule applies only when the selected payment method matches

#### Scenario D: Campaign Source Promotion
- **Goal:** Target email campaign traffic
- **Rule Type:** CartCondition
- **Conditions:** Campaign source = `email-spring-sale`
- **Discount:** Fixed amount discount
- **Result:** Only traffic from the tracked campaign qualifies

### 5.8 Limitations and Conditions

| Limitation | Impact | Workaround |
|---|---|---|
| Requires at least one condition | Rule cannot activate without conditions | Add at least one qualifying condition |
| Free-item rewards are not used here | CartCondition is discount-focused | Use Product-Based or BOGO for rewards |
| Complex trees are harder to audit | Admin may need careful testing | Use clear group names and priority values |
| Some sources depend on request context | Not every condition is available in every browsing flow | Test with the actual storefront context |
| Same-line match can exclude valid carts | Conditions may be too strict | Disable same-line match if line scoping is not needed |

### 5.9 Practical Offer Scenarios

#### Scenario E: Offer On All Products Except Three Items
- **Goal:** Run one promotion across the whole catalog, but exclude 3 specific products.
- **Rule Type:** CartCondition
- **Conditions:**
  - Add the 3 products to the **Excluded Product** condition.
  - Leave the rule otherwise global so it can match all other products.
- **Discount:** Percentage or fixed amount
- **Result:** All products participate except the 3 excluded products.

**Best setup pattern:**
1. Create a `CartCondition` rule.
2. Add the 3 products you want to exclude.
3. Use `Percentage` or `FixedAmount` discount.
4. Keep `StopFurtherRulesForMatchedLines` enabled only if you want these items to remain protected from later rules.

#### Scenario F: Buy X, Get 50 Off on Another Item, Cumulative
- **Goal:** If the customer buys more qualifying quantity, the reward repeats in multiples.
- **Rule Type:** BuyXGetY or CartCondition depending on whether the trigger is product-based or cart-based.
- **Discount:** Fixed amount on the reward line, or percentage if the reward should scale by price.
- **Result:**
  - Buy 2 units → apply 1 reward block.
  - Buy 4 units → apply 2 reward blocks.
  - Buy 6 units → apply 3 reward blocks.

**Best setup pattern:**
1. Use `BuyXGetY` when the reward is tied to a product or set of products.
2. Use `CartCondition` when the reward depends on cart-wide conditions.
3. Create tier rows so each complete quantity set gets its own discount.
4. If the reward should stack, keep the rule non-exclusive and avoid blocking later matched lines.

#### Scenario G: Cart Has Quantity 5 Across Two Products at the Same Price Level
- **Goal:** Apply the same discount logic even when items are split across two products with equal pricing.
- **Rule Type:** BuyXGetY or CartCondition with quantity-based logic.
- **Example:**
  - Product A quantity: 2
  - Product B quantity: 3
  - Total quantity: 5
- **Result:** The rule evaluates the total qualifying quantity and applies the matching tier or cumulative reward.

**Best setup pattern:**
1. If both products should count together, use a shared buy-product rule group.
2. If the discount must apply to the cheapest or matched item only, use a matched-line scope.
3. If the two products are in the same price level, use a tier structure so the fifth item does not change the unit price unexpectedly.
4. If you want each pair of 2 items to trigger again, create tiers such as `2`, `4`, and `6` so the promotion repeats predictably.

### 5.10 Quick Recommendation Map

| Customer Goal | Best Rule Type | Why |
|---|---|---|
| Offer on all products except a few items | CartCondition | Supports excluded products and global eligibility |
| Buy X get fixed money off another item | BuyXGetY | Best for reward-line discounts |
| Discount repeats every 2 units | BuyXGetY | Tiered quantity logic supports cumulative rewards |
| Quantity from multiple products counts together | BuyXGetY or CartCondition | Both can evaluate total matched quantity |
| Apply one offer across most of the catalog | CartCondition | Easiest way to target all but a few products |

---

---

## Cross-Rule Strategies: Combining Multiple Rule Types

The plugin supports applying multiple rules simultaneously to the same cart. This enables sophisticated multi-layered promotions:

### Strategy 1: Subtotal + Product Rules
- **Subtotal Rule:** Spend $100+ → 5% off entire cart
- **Product Rule:** Buy Laptop → 10% off laptop only
- **Result:** Customer buys laptop for $999 + other items totaling $200:
  - Product rule applies: -$99.90 (10% off laptop)
  - Subtotal rule applies: -$60 (5% off cart total of $1,200)
  - **Total Savings:** -$159.90
- **Use When:** Combining universal thresholds with targeted product discounts

### Strategy 2: Combo + Subtotal Rules
- **Combo Rule:** Monitor + Keyboard + Mouse Bundle → $399 (saves $128)
- **Subtotal Rule:** Spend $500+ → 10% off
- **Result:** Customer buys combo ($399) + Monitor Stand ($150) = $549 total
  - Combo rule applies: -$128 (bundle discount)
  - Subtotal rule applies: $54.90 (10% off $549)
  - **Total Savings:** -$182.90
- **Use When:** Large orders with bundle components

### Strategy 3: Tiered Subtotal Rules (Highest Tier Wins)
- **Rule 1:** Spend $50-$99 → 5% off
- **Rule 2:** Spend $100-$199 → 10% off
- **Rule 3:** Spend $200+ → 15% off
- **Set Priorities:** 1, 2, 3 (ensures only highest tier applies)
- **Result:** Customer spends $250 → receives 15% off (not 5% or 10%)
- **Use When:** Creating tiered discount structures without rule stacking

### Strategy 4: Exclusive Category Promotions
- **Rule 1 (Exclusive):** Electronics → 15% off
- **Rule 2 (Exclusive):** Apparel → 20% off
- **Result:** If customer buys electronics, other rules blocked. If apparel, other rules blocked.
- **Use When:** Competing promotions must not overlap

### Strategy 5: Limited-Time Product + Base Subtotal
- **Subtotal Rule (Non-Exclusive):** Spend $75+ → 10% off (always active)
- **Product Rule (Non-Exclusive):** Flash sale item at -30% (limited time)
- **Result:** Customer buying flash item + normal items:
  - Flash product: -30%
  - Other items + cart: -10%
  - **Total:** Multiple savings combine
- **Use When:** Running flash sales within baseline promotions

### Strategy 6: BOGO Tier + Subtotal Bonus
- **BOGO Rule:** Buy 4+ Coffee → 10% off per unit
- **Subtotal Rule (Non-Exclusive):** Spend $100+ → additional 5% off entire cart
- **Result:** Customer buys 4 coffee ($150 total):
  - BOGO applies: -$15 (10% off $150)
  - Subtotal rule applies: -$7.50 (5% off adjusted total)
  - **Total Savings:** -$22.50
- **Use When:** Rewarding high-volume bulk buyers with layered savings

### Strategy 7: BOGO Escalation for Long-Term Loyalty
- **BOGO Rule (Non-Exclusive):** 
  - Buy 2 Coffee → Free Cup (Tier 1)
  - Buy 5 Coffee → Free Cup + 10% off (Tier 2)
  - Buy 10 Coffee → Free Cup + 15% off + Free Gift Box (Tier 3)
- **Product Rule (Non-Exclusive):** Flavor upgrade → Free specialty beans
- **Result:** Repeat customer buying 10 bags:
  - BOGO Tier 3 applies: -15% + free cup + free gift box
  - Product rule: free specialty beans if buying upgrade flavor
  - **Total Value:** Multiple tiers of appreciation rewards
- **Use When:** Building subscription/loyalty programs with escalating benefits

---

## Best Practices for Promotion Design

### 1. Priority and Exclusivity
- **Best Practice:** Keep priorities unique and clearly intentional
- **Example:**
  - Priority 1: VIP exclusive offers (exclusive = true)
  - Priority 2: Time-sensitive flash sales (exclusive = false)
  - Priority 3: Evergreen subtotal discounts (exclusive = false)
- **Why:** Prevents unpredictable behavior from rule ordering

### 2. Customer Communication
- **Website Messaging:** Display promotion rules on product pages and category pages
- **Cart Display:** Enable "Cart Savings Breakdown" so customers see what's applied
- **Header Link:** "View All Offers" helps customers discover active promotions
- **Why:** Transparency builds trust and encourages repeat purchases

### 3. Discount Value Psychology
- **Percentage Discounts:** Best for high-ticket items (feels more valuable)
- **Fixed Amount:** Best for low-ticket items ($5 off on $20 item is clear)
- **Bundle Pricing:** Best for educational communication (show original vs. bundle)
- **Why:** Different formats resonate with different customer segments

### 4. Date Range Management
- **Always Set Dates:** Even for "permanent" rules, use far-future end dates
- **Advance Planning:** Set promotion rules weeks in advance, activate via date range
- **Campaign Calendar:** Coordinate dates with marketing calendar
- **Why:** Prevents accidental permanent discounts; enables easy on/off

### 5. Testing Before Activation
- **Test Environment:** Create rules in staging first if possible
- **Edge Cases:** Test partial combos, exact threshold amounts, multiple rule interactions
- **Customer Roles:** Test with different customer segments (VIP vs. regular)
- **Why:** Prevents revenue leakage from unintended rule behavior

### 6. Monitoring and Analytics
- **Usage Dashboard:** Check "Analytics" page for rule effectiveness
- **Revenue Impact:** Compare orders with rule vs. without
- **Adjust:** If a rule isn't performing, adjust dates, values, or exclusivity
- **Why:** Data-driven decisions improve ROI on promotion budgets

---

## Troubleshooting Guide

### Promotion Not Appearing in Cart
1. **Check Active Status:** Is the rule marked "Active" in admin?
2. **Check Date Range:** Is today's date within the rule's start/end dates?
3. **Check Store Scope:** Is the rule configured for this store?
4. **Check Eligibility:** Does the cart meet all conditions (products, subtotal, etc.)?
5. **Check Priority:** Is an earlier exclusive rule blocking this one?

### Wrong Discount Amount
1. **Verify Discount Type:** Is it Percentage or FixedAmount?
2. **Check Discount Value:** Is the value correct (e.g., 10% vs. 10)?
3. **Verify Calculation Base:** 
   - Subtotal rules: Discount applied to entire cart
   - Product rules: Discount applied to target product only
   - Combo: Discount applied to complete set only
4. **Check Multi-Rule Interaction:** Multiple rules applying simultaneously?

### Free Product Not Appearing
1. **Check Reward Type:** Is Discount Type set to "FreeItem"?
2. **Verify Reward Product:** Is the reward product selected?
3. **Check Reward Mode:** Is it set to "Auto-Add" or "Selection Modal"?
4. **Check Eligibility:** Does cart meet minimum quantity requirements?

### Selection Modal Not Showing
1. **Check Rule Type:** Only Product-Based supports selection
2. **Check Reward Behavior:** Is it set to "Customer Selection Modal"?
3. **Check Eligibility:** Must meet minimum quantity to trigger modal
4. **Check Browser Cache:** Clear cache and refresh product page

---

## Summary Table: Quick Reference

| Rule Type | Best For | Key Setting | Discount Types | Special Features |
|-----------|----------|-------------|-----------------|------------------|
| **Subtotal-Based** | Spending thresholds | Operator (>, <, Between) | %, Fixed | Tiering support |
| **Product-Based** | Specific products | Min Quantity | %, Fixed, Free Item | Free item selection modal |
| **Combo Pricing** | Bundle offers | Product list | %, Fixed, Bundle Price | Multi-unit sets |
| **Cart Condition** | Cart, customer, and context filters | Conditions | %, Fixed | AND/OR logic, source-based rules |
| **BOGO** | Bulk purchases | Tiers (Min/Max Qty) | %, Fixed, Free Item | Escalating rewards, progressive discounts |

---

## Appendix: Additional Resources

- **Admin Manual:** See `ADMIN_USER_MANUAL.md` for detailed admin pages and workflows
- **Technical Documentation:** See `context.md` for system architecture
- **Feature Breakdown:** See `FEATURE_GROUPS.md` for feature-by-rule-type matrix
- **Product Backlog:** See `PRODUCT_BACKLOG_AND_USER_STORIES.md` for planned features

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Audience:** Business Users, Marketing Managers, E-Commerce Operators  
**Tone:** Professional, Business-Focused, Non-Technical
