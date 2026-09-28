# Product-Based Setup (Independent DiscountManagerPlus)

## 1) What changed
- DiscountManagerPlus rules are now managed directly from:
  - `Admin > Nop-Station > DiscountManagerPlus > Promotion Rules`
- No sync with default `Discount` records is required.
- Product attribute filtering is supported for product-source rule items.
- New rule flag:
  - `Cumulative with other discounts`
  - `Checked`: DiscountManagerPlus can stack with other discounts.
  - `Unchecked`: DiscountManagerPlus skips this rule when conflicting discounts are found (line-level or cart-level default discounts).

## 2) Product-based rule setup
1. Create a rule with `Rule type = ProductBased`.
2. Select `Discount type` (`Percentage`, `FixedAmount`, or `FreeItem`).
3. Save rule.
4. Open `Products` card and add source rows:
   - `Source type = Product/Category/Manufacturer/Vendor`
5. For attribute-based product targeting:
   - Use `Source type = Product`
   - Set `Attribute filter = Specific attribute values`
   - Click `Choose attributes` and select values
6. Activate the rule.

## 3) Difference: DiscountManagerPlus Product vs Default Applied Product
- **Default nop product discount**:
  - Defined in `Admin > Discount`
  - Applied by nop default discount pipeline
- **DiscountManagerPlus product rule**:
  - Defined in Promotion Rule `Products` section
  - Applied by DiscountManagerPlus runtime logic
  - Supports advanced product/category/manufacturer/vendor + attribute matching

## 4) Two setup examples (cumulative ON/OFF)

### Example A: Non-cumulative
- Default nop: Category `Shoes` has 10% discount
- Promotion rule: ProductBased 15% on `Shoes`
- Rule option: `Cumulative with other discounts = false`
- Result: Promotion rule is skipped for `Shoes` lines already receiving default category/product/manufacturer discount

### Example B: Cumulative
- Same setup as Example A
- Rule option: `Cumulative with other discounts = true`
- Result: Promotion discount can stack on top of default discounts
