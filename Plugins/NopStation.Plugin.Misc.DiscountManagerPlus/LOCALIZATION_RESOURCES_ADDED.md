# Localization Resources Added

## Overview
All localization resources for the Simplified Excluded Products Section have been successfully added to the DiscountManagerPlus plugin.

## Resource Location
The resources have been added to the `GetPluginResources()` method in:
**File:** `DiscountManagerPlusPlugin.cs`

## Localization Resources Added

### Main Tab & Section
```csharp
["Admin.NopStation.DiscountManagerPlus.PromotionRules.ExcludedProductsWithCount"] = "Excluded Products ({0})"
```

### Section Description & Actions
```csharp
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Description"] = "Products in this list will not receive the promotion discount, even if they match the promotion criteria."
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.AddExcludedProducts"] = "Add Excluded Products"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectProducts"] = "Select products to exclude from this promotion"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectExcludedProducts"] = "Please select at least one product to exclude"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.BulkDelete"] = "Bulk Delete"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added"] = "{0} product(s) have been added to the exclusion list."
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Deleted"] = "The excluded products have been removed."
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.AddPopup.Description"] = "Select one or more products to exclude from this promotion rule. Excluded products will not receive the discount even if they match the promotion criteria."
```

### Field Labels
```csharp
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductId"] = "Product ID"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName"] = "Product name"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Sku"] = "SKU"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Price"] = "Price"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductPictureUrl"] = "Product picture"
["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.CreatedOn"] = "Added on"
```

## Total Resources Added
**14 new localization strings** have been added to support the excluded products feature.

## How Localization Works in NopStation Plugins

### Resource Storage
- Resources are defined in the plugin's `GetPluginResources()` method
- They are automatically stored in the nopCommerce database `LocaleStringResource` table
- The plugin's `UpdateAsync()` method handles adding/updating resources

### Multi-Language Support
- All resources can be translated to other languages through the nopCommerce admin panel
- Navigate to: **Administration → Configuration → Languages**
- Edit each language and translate the resource strings

### Resource Naming Convention
Resources follow this pattern:
```
Admin.NopStation.DiscountManagerPlus.{Feature}.{Category}.{PropertyName}
```

Examples:
- `Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Description` - Feature description
- `Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName` - Field label
- `Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added` - Success message

## Usage in Views

These resources are used in the views with the `T()` helper:

```cshtml
@T("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Description")
@T("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName")
@T("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added")
```

## Testing Localization

### Verify Resources are Loaded
1. Build and run the application
2. Navigate to: **Administration → Promotions → Discounts**
3. Edit a discount and open a promotion rule
4. Verify all text displays correctly in the "Excluded Products" section

### Test Multi-Language
1. Install additional languages in nopCommerce
2. Navigate to: **Administration → Configuration → Languages**
3. Edit a language and search for "DiscountManagerPlus"
4. Translate the new excluded products resources
5. Switch to that language and verify translations display

## Adding Translations

To add translations for other languages:

1. **Navigate to Language Resources**
   - Go to: **Administration → Configuration → Languages**
   - Click "Edit" next to the desired language

2. **Search for Resources**
   - Filter by: "DiscountManagerPlus"
   - Find the excluded products resources (they start with `Admin.NopStation.DiscountManagerPlus.ExcludedProducts`)

3. **Add Translations**
   - Click "Add new resource" or edit existing resources
   - Enter the translated text for each resource key
   - Save the changes

4. **Verify**
   - Switch to the translated language in the admin panel
   - Navigate to the excluded products section
   - Verify all text displays in the translated language

## Resource Keys Summary

### UI Elements
- `ExcludedProductsWithCount` - Tab title with product count
- `Description` - Section description
- `AddExcludedProducts` - Button text
- `SelectProducts` - Placeholder text
- `SelectExcludedProducts` - Validation message
- `BulkDelete` - Button text

### User Messages
- `Added` - Success message when products are added
- `Deleted` - Success message when products are removed
- `AddPopup.Description` - Help text in popup

### Data Fields
- `Fields.ProductId` - Product ID column header
- `Fields.ProductName` - Product name column header
- `Fields.Sku` - SKU column header
- `Fields.Price` - Price column header
- `Fields.ProductPictureUrl` - Product picture (internal use)
- `Fields.CreatedOn` - Date added column header

## Maintenance

### When Adding New Features
Always add localization resources for any new:
- UI elements (buttons, labels, headers)
- User messages (success, error, info)
- Help text and descriptions
- Field labels and hints

### Consistency
- Follow existing naming conventions
- Use clear, descriptive English text
- Include context in descriptions when helpful
- Support parameter substitution (e.g., `{0}` for counts)

## Benefits of Proper Localization

✅ **Multi-Language Support** - Easy translation to any language
✅ **Consistent UI** - Centralized resource management
✅ **Easy Updates** - Change text in one place
✅ **Professional Appearance** - No hardcoded text in views
✅ **Accessibility** - Screen readers can properly announce UI elements
✅ **Maintenance** - Easier to update and manage text changes

## Next Steps

1. ✅ **Build the solution** - Compile the plugin
2. ✅ **Run the application** - Start nopCommerce
3. ✅ **Install/Update plugin** - The UpdateAsync method will add resources automatically
4. ✅ **Test UI** - Verify all text displays correctly
5. 📝 **Add translations** - Translate to other languages as needed

The localization system is now complete and ready for use! 🌍
