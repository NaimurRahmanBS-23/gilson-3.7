# Nop.Plugin.Fix.SubtotalCoupon

A zero-core-change plugin for **nopCommerce 3.7** that fixes the coupon code being
silently dropped from the shopping cart line-item subtotal calculation.

---

## The Bug

In nopCommerce 3.7, `ShoppingCartService` calculates the cart subtotal by calling
`GetSubTotal` → `GetUnitPrice` → `GetFinalPrice` on `PriceCalculationService`.
The unit-price display path correctly forwards the customer's coupon code, so the
price shown per item is discounted (e.g. **$66.50**).

However, the subtotal accumulation path calls `GetFinalPrice` **without** the coupon
code argument. `GetFinalPrice` therefore never finds a matching discount, returns the
base price (**$70.00**), and the row total (`unit price × quantity`) is wrong.

### Symptom

| Column        | Displayed value | Expected value |
|---------------|-----------------|----------------|
| Unit Price    | $66.50 ✔        | $66.50         |
| Subtotal (×1) | **$70.00 ✘**    | $66.50         |

---

## The Fix

This plugin replaces `IPriceCalculationService` in nopCommerce's IoC container with
`FixedPriceCalculationService`, which overrides two methods:

| Method         | What changes |
|----------------|--------------|
| `GetSubTotal`  | Reads `SystemCustomerAttributeNames.DiscountCouponCode` from the current customer and passes it explicitly to `GetFinalPrice`. |
| `GetUnitPrice` | Same fix, keeping the unit-price display and the row-total calculation in sync. |

Because Autofac resolves the **last** registration for a given service interface, and
nopCommerce always loads plugin registrars after core registrars, no core file needs
to be modified.

---

## Project Structure

```
Nop.Plugin.Fix.SubtotalCoupon/
├── Description.txt                          ← plugin manifest (required by nopCommerce)
├── Nop.Plugin.Fix.SubtotalCoupon.csproj
├── SubtotalCouponFixPlugin.cs               ← IPlugin entry point (install/uninstall hooks)
├── Properties/
│   └── AssemblyInfo.cs
├── Infrastructure/
│   └── DependencyRegistrar.cs              ← replaces IPriceCalculationService in IoC
└── Services/
    └── FixedPriceCalculationService.cs     ← the patched implementation
```

---

## Installation

### 1. Add the project to your solution

Open the nopCommerce 3.7 solution in Visual Studio, right-click **Plugins** in Solution
Explorer, choose **Add → Existing Project**, and select
`Nop.Plugin.Fix.SubtotalCoupon.csproj`.

### 2. Verify project references

The `.csproj` includes `ProjectReference` entries for `Nop.Core` and `Nop.Services`.
Confirm that the `<Project>` GUIDs match those in your local solution (visible in each
project's `.csproj` file) and update them if they differ.

### 3. Check the output path

Both `Debug` and `Release` output paths in the `.csproj` point to:

```
..\..\..\Presentation\Nop.Web\Plugins\Fix.SubtotalCoupon\
```

Adjust this relative path if your folder layout is different. nopCommerce requires the
plugin DLL and `Description.txt` to be inside a dedicated sub-folder under
`Nop.Web/Plugins/`.

### 4. Build

Build the solution in Visual Studio. The output path setting will copy the compiled DLL
and `Description.txt` into the correct plugin folder automatically.

### 5. Activate in the admin panel

1. Start (or restart) the nopCommerce site.
2. Go to **Admin → Configuration → Plugins → Local plugins**.
3. Click **Reload list of plugins** if the plugin does not appear.
4. Find **Subtotal Coupon Fix** and click **Install**.
5. Restart the application when prompted.

The fix is now live — no further configuration is required.

---

## Uninstallation

1. Go to **Admin → Configuration → Plugins → Local plugins**.
2. Find **Subtotal Coupon Fix** and click **Uninstall**.
3. Restart the application when prompted.
4. Delete the `Plugins/Fix.SubtotalCoupon/` folder from `Nop.Web` if you want to
   remove it entirely.

---

## Compatibility Notes

- Tested against the **nopCommerce 3.7** public release API surface.
- The plugin inherits from `PriceCalculationService` (concrete class), so any
  customisations already applied to that class in your production binary are preserved
  for all methods **except** `GetSubTotal` and `GetUnitPrice`, which are overridden.
- Tier prices continue to work correctly because `GetFinalPrice` handles coupon codes
  and tier prices independently; passing the coupon code does not suppress tier pricing.
- If your production binary has already overridden `GetFinalPrice`, this plugin will
  still call your override — it only changes what arguments are passed in.

---

## How the IoC Override Works

`DependencyRegistrar` implements `IDependencyRegistrar` with `Order = 10`.
nopCommerce scans all assemblies for this interface on startup and calls each registrar
in order. Core registrars use order `0`, so `Order = 10` guarantees this registration
runs last. Autofac's last-registration-wins rule means every component that depends on
`IPriceCalculationService` — including `ShoppingCartService` — will receive
`FixedPriceCalculationService` transparently.
