# 🔧 **Complete Debugging Guide - Cart Display Issue**

## 🚨 **Your Current Issue**

**Cart Shows (WRONG):**
```
HTC One Mini Blue: $0.00 × 2 = $0.00 ← Both units FREE!
Total: $3,245.00
You save: $200.00
```

**Should Show (CORRECT):**
```
HTC One Mini Blue: $50.00 × 2 = $100.00 ← 1 FREE + 1 at 50% off
• 1st unit: $0.00 (FREE from Buy 2 Get 1 Free)
• 2nd unit: $50.00 (50% off from Buy 1 Get 50% Off)

Total: $3,295.00
You save: $150.00
```

---

## ✅ **Fixes Applied**

### **Fix 1: `PromotionDiscountAllocator.cs` (Line 501)** ✅
```csharp
// FIXED: Use correct overload with discount type and value
var rewardDiscountResult = await CalculateRewardDiscountsAsync(
    rewardItems,
    appliedPromotion.RewardQuantity,
    (DiscountType)appliedPromotion.DiscountTypeId,
    appliedPromotion.DiscountValue);
```

### **Fix 2: `DiscountManagerPlusService.cs` (Line 472)** ✅
```csharp
// FIXED: Use correct overload with discount type and value
var rewardDiscountResult = await _promotionDiscountAllocator.CalculateRewardDiscountsAsync(
    rewardItems,
    appliedPromotion.RewardQuantity,
    appliedPromotion.DiscountedQuantitiesByLineId,
    (DiscountType)appliedPromotion.DiscountTypeId,
    appliedPromotion.DiscountValue);
```

---

## 🔍 **Step-by-Step Debugging**

### **Step 1: Verify Fixes Are Deployed**

#### **Check Code Files:**
1. Open `PromotionDiscountAllocator.cs`
2. Go to line **501-506**
3. You should see:
```csharp
// FIXED: Use correct overload with discount type and value
var rewardDiscountResult = await CalculateRewardDiscountsAsync(
    rewardItems,
    appliedPromotion.RewardQuantity,
    (DiscountType)appliedPromotion.DiscountTypeId,
    appliedPromotion.DiscountValue);
```

4. Open `DiscountManagerPlusService.cs`
5. Go to line **472-476**
6. You should see:
```csharp
var rewardDiscountResult = await _promotionDiscountAllocator.CalculateRewardDiscountsAsync(
    rewardItems,
    appliedPromotion.RewardQuantity,
    appliedPromotion.DiscountedQuantitiesByLineId,
    (DiscountType)appliedPromotion.DiscountTypeId,
    appliedPromotion.DiscountValue);
```

If you DON'T see these fixes, they weren't applied!

---

### **Step 2: Rebuild and Deploy**

#### **Build Solution:**
1. Open Visual Studio
2. Open `Themes.sln` solution
3. Right-click solution → **Clean Solution**
4. Right-click solution → **Rebuild Solution**
5. Look for **Build Succeeded** message

#### **Deploy Plugin:**
1. Find the compiled DLL:
   ```
   src/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/bin/Debug/
   or
   src/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/bin/Release/
   ```
2. **Copy the DLL** to your nopCommerce plugins folder:
   ```
   wwwroot/plugins/NopStation.Plugin.Misc.DiscountManagerPlus/
   ```
3. **Overwrite existing file** (confirm if prompted)

#### **Restart Application:**
1. **Restart IIS** (if using IIS)
   - Open IIS Manager
   - Find your website application pool
   - Right-click → **Recycle**
   
2. **OR restart Kestrel/NopCommerce**

---

### **Step 3: Clear ALL Caches**

#### **Clear nopCommerce Cache:**
1. Log into nopCommerce Admin
2. Go to **System → Configuration**
3. Click **"Clear Cache"** button

#### **Clear Browser Cache:**
1. **Chrome/Edge**: Press `Ctrl+Shift+Delete`
2. Select **"Cached images and files"**
3. Click **"Clear data"**

#### **Restart Application (if needed):**
1. Stop the website
2. Clear server cache
3. Start the website again

---

### **Step 4: Verify Plugin is Active**

#### **Check Plugin Status:**
1. Go to **nopCommerce Admin → Configuration → Local Plugins**
2. Find **"DiscountManagerPlus"**
3. Status should be **"Installed"** ✅
4. **Active** checkbox should be **checked** ✅

#### **Reinstall Plugin (if needed):**
1. Click **"Uninstall"** if needed
2. Click **"Install"** again
3. **Activate** the plugin

---

### **Step 5: Test with Fresh Cart**

#### **Clear Existing Cart:**
1. Go to your **Shopping Cart** page
2. **Empty the cart completely**
3. Close browser tab

#### **Add Test Products:**
1. Open new browser tab
2. Add these products:
   - **Asus Laptop**: Quantity 2 ($1500 each)
   - **HTC One Mini Blue**: Quantity 2 ($100 each)
   - **HTC Smartphone**: Quantity 1 ($245 each)

#### **Check Cart Display:**
1. Go to **Shopping Cart** page
2. Look for **"Cart Savings"** section
3. Expected display:

```
✨ 2 simultaneous discounts applied!

Cart Savings:
├─ Buy 2 Get 1 Free - Best Offer: $100.00
│  └─ (Applied to HTC One Mini Blue - Free Item)
├─ Buy 1 Get 50% Off Any Item: $50.00
│  └─ (Applied to HTC One Mini Blue - 50% Off)
└─ Total Savings: $150.00
```

---

## 🐛 **If Issue Still Persists**

### **Debug Mode 1: Check Plugin Logs**

#### **Enable Debug Logging:**
1. Open `appsettings.json`
2. Find or add:
```json
{
  "DetailedErrorReport": {
    "EnableDebug": true
  }
}
```

3. Restart application
4. Check logs in:
   ```
   /Logs/DiscountManagerPlus.log
   ```

#### **Look for Errors:**
```
ERROR - Could not calculate reward discount
ERROR - Invalid discount type
ERROR - Discount calculation failed
```

---

### **Debug Mode 2: Test Simple Scenario**

#### **Test Case: Single Offer First**
1. **Deactivate** "Buy 1 Get 50% Off" offer
2. **Keep active** only "Buy 2 Get 1 Free" offer
3. Test with your cart
4. **Expected**: Only 1 unit should be FREE

#### **Test Case: Then Activate Second Offer**
1. **Activate** "Buy 1 Get 50% Off" offer
2. **Test with same cart**
3. **Expected**: 1 unit FREE + 1 unit at 50% off

---

### **Debug Mode 3: Verify Rule Configuration**

#### **Check "Buy 2 Get 1 Free" Offer:**
1. Go to **Promotion Rules**
2. Edit "Buy 2 Get 1 Free" offer
3. Check **Tiers** tab:
   - **Buy Quantity**: 2
   - **Reward Quantity**: 1
   - **Discount Type**: **Free Item**
   - **Discount Value**: **100**

#### **Check "Buy 1 Get 50% Off" Offer:**
1. Go to **Promotion Rules**
2. Edit "Buy 1 Get 50% Off" offer
3. Check **Tiers** tab:
   - **Buy Quantity**: 1
   - **Reward Quantity**: 1
   - **Discount Type**: **Percentage**
   - **Discount Value**: **50**

---

## 🔍 **Advanced Debugging**

### **Check Discount Calculation Flow:**

#### **Enable Logging:**
Add temporary logging to `PromotionDiscountAllocator.cs`:

```csharp
public async Task<(Dictionary<int, decimal> LineDiscountMap, Dictionary<int, decimal> RuleDiscountMap)> BuildDiscountMapsAsync(...)
{
    var lineDiscountMap = new Dictionary<int, decimal>();
    var ruleDiscountMap = new Dictionary<int, decimal>();
    
    // ADD THIS:
    Console.WriteLine($"=== BuildDiscountMapsAsync START ===");
    
    foreach (var rule in activeRules)
    {
        // ADD THIS:
        Console.WriteLine($"Processing rule: {rule.Name}, Type: {rule.RuleType}");
        
        var evaluationCart = rule.RuleType == PromotionRuleType.BuyXGetY
            ? BuildCartWithRemainingQuantities(cart, consumedBuyXGetYQuantitiesByLineId)
            : cart;
        
        // ADD THIS:
        Console.WriteLine($"Evaluation cart items: {evaluationCart.Count}");
        foreach (var item in evaluationCart)
            Console.WriteLine($"  - Product {item.ProductId}, Qty: {item.Quantity}");
    }
    
    Console.WriteLine($"=== Final Line Discounts ===");
    foreach (var lineDiscount in lineDiscountMap)
        Console.WriteLine($"Line {lineDiscount.Key}: ${lineDiscount.Value}");
    
    Console.WriteLine($"=== Final Rule Discounts ===");
    foreach (var ruleDiscount in ruleDiscountMap)
        Console.WriteLine($"Rule {ruleDiscount.Key}: ${ruleDiscount.Value}");
}
```

---

## 🚨 **Common Issues**

### **Issue 1: Plugin Not Rebuilt**
**Solution**: Rebuild solution in Visual Studio and redeploy DLL

### **Issue 2: Cache Not Cleared**
**Solution**: Clear nopCommerce cache + browser cache + restart IIS

### **Issue 3: Wrong Rule Configuration**
**Solution**: Verify both offers have correct tier configurations

### **Issue 4: Discount Type Wrong**
**Solution**: Ensure Offer 1 has **Percentage (50%)** and Offer 2 has **Free Item (100%)**

### **Issue 5: Priority Wrong**
**Solution**: Offer 2 should have **Priority 1** (higher), Offer 1 should have **Priority 2**

---

## 📋 **Verification Checklist**

Before testing, verify:

- [ ] Both code fixes are present in files
- [ ] Solution has been rebuilt
- [ ] DLL has been redeployed
- [ ] Application has been restarted
- [ ] nopCommerce cache cleared
- ] Browser cache cleared
- [ ] Plugin shows "Installed" status
- [ ] Plugin is "Active"
- [ ] Both offers are active
- [ ] Priority: Offer 2 = 1, Offer 1 = 2
- [ ] Offer 1: Discount Type = Percentage, Value = 50
- [ ] Offer 2: Discount Type = Free Item, Value = 100
- [ ] Cart has been cleared and retested

---

## 🎯 **Expected Final Result**

**After All Fixes Applied:**

```
Shopping Cart Contents:
┌─────────────────────────────────────────┐
│ Asus Laptop (2×)     $3,000.00         │
│ HTC One Mini Blue (2×) $100.00 ← FIXED!   │
│ HTC Smartphone (1×)   $245.00          │
├─────────────────────────────────────────┤
│ Subtotal:            $3,345.00         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│ Total Savings:       $150.00          │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│ Total:               $3,195.00         │
└─────────────────────────────────────────┘

Cart Savings Breakdown:
┌─────────────────────────────────────────┐
│ ✨ 2 simultaneous discounts applied!        │
├─────────────────────────────────────────┤
│ Buy 2 Get 1 Free: $100.00              │
│ • Applied to HTC One Mini Blue (FREE)     │
│                                         │
│ Buy 1 Get 50% Off: $50.00              │
│ • Applied to HTC One Mini Blue (50% off)  │
├─────────────────────────────────────────┤
│ Total Savings: $150.00                  │
│                                         │
│ 💰 Cheapest items automatically selected    │
└─────────────────────────────────────────┘

HTC One Mini Blue Breakdown:
• Unit 1: $0.00 (FREE from Buy 2 Get 1 Free)
• Unit 2: $50.00 (50% off from Buy 1 Get 50% Off)
• Line Total: $100.00 (not $0.00!)
```

---

## 🎉 **Success Indicators**

When the fix is working correctly:

✅ **Cart Display**:
- HTC One Mini Blue shows: **$50.00 × 2 = $100.00** (not $0.00)
- Total cart shows: **$3,295.00** (not $3,245.00)
- Total savings: **$150.00** (not $200.00)

✅ **Cart Savings Section**:
- Shows both discounts separately
- Breakdown shows which product got which discount
- Total savings amount is correct

✅ **Attention Messages**:
- Multiple discount notice appears
- Cheapest item selection messages show correctly
- All amounts are accurate

---

## 📞 **If Still Not Working**

### **Collect Diagnostic Information:**

1. **Plugin Version**: Check `plugin.json` for version number
2. **Error Logs**: Check `/Logs/` folder for errors
3. **Rule Configuration**: Screenshot of both offers' settings
4. **Cart Screenshot**: Current cart display
5. **Browser Console**: Open DevTools (F12) → Console tab

### **Share This Information:**

Provide the above information so I can:
- Verify fixes were applied correctly
- Check for additional code paths I missed
- Identify other integration issues
- Create more targeted fixes

---

**🚀 Follow this guide step by step and your multiple offer scenarios should work correctly!**