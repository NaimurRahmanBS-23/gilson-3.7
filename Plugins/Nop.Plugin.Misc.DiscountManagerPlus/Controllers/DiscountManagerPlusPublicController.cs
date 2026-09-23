using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Controllers
{
    public class DiscountManagerPlusPublicController : BasePluginController
    {
        private const string RenderedRequestItemKey = "NopStation.DiscountManagerPlus.CartSavings.Rendered";
        private const string ViewRoot = "~/Plugins/Misc.DiscountManagerPlus/Views/DiscountManagerPlusPublic/";

        private readonly ICategoryService _categoryService;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IDiscountManagerPlusService _discountManagerPlusService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IRewardSynchronizationService _rewardSynchronizationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IPictureService _pictureService;
        private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
        private readonly ILogger _logger;
        private readonly IPromotionAttentionMessageService _promotionAttentionMessageService;
        private readonly ICurrencyService _currencyService;
        private readonly MediaSettings _mediaSettings;

        public DiscountManagerPlusPublicController(
            ICategoryService categoryService,
            IPromotionRuleService promotionRuleService,
            IDiscountManagerPlusService discountManagerPlusService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IShoppingCartService shoppingCartService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            IRewardSynchronizationService rewardSynchronizationService,
            IPriceFormatter priceFormatter,
            IPriceCalculationService priceCalculationService,
            IPictureService pictureService,
            IPromotionConditionEvaluator promotionConditionEvaluator,
            ILogger logger,
            IPromotionAttentionMessageService promotionAttentionMessageService,
            ICurrencyService currencyService,
            MediaSettings mediaSettings)
        {
            _categoryService = categoryService;
            _promotionRuleService = promotionRuleService;
            _discountManagerPlusService = discountManagerPlusService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _workContext = workContext;
            _storeContext = storeContext;
            _shoppingCartService = shoppingCartService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _settingService = settingService;
            _localizationService = localizationService;
            _rewardSynchronizationService = rewardSynchronizationService;
            _priceFormatter = priceFormatter;
            _priceCalculationService = priceCalculationService;
            _pictureService = pictureService;
            _promotionConditionEvaluator = promotionConditionEvaluator;
            _logger = logger;
            _promotionAttentionMessageService = promotionAttentionMessageService;
            _currencyService = currencyService;
            _mediaSettings = mediaSettings;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRewardToCart(int promotionRuleId, int rewardProductId, int rewardQuantity, FormCollection form)
        {
            var customer = _workContext.CurrentCustomer;
            var store = _storeContext.CurrentStore;

            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return Json(new { success = false, message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null || !rule.IsActive)
                return Json(new { success = false, message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

            var product = _productService.GetProductById(rewardProductId);
            if (product == null || product.Deleted)
                return Json(new { success = false, message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

            if (!IsRewardProductAllowed(rule, rewardProductId))
                return Json(new { success = false, message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

            var cart = GetCart();
            var appliedPromotion = _discountManagerPlusService.EvaluateRule(rule, cart);
            if (appliedPromotion == null)
                return Json(new { success = false, message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

            var quantityToAdd = appliedPromotion.RewardQuantity > 0 ? appliedPromotion.RewardQuantity : rewardQuantity;
            if (quantityToAdd <= 0)
                quantityToAdd = 1;

            var customerEnteredPriceConverted = ParseCustomerEnteredPrice(product, form);
            var attributes = ParseProductAttributes(product, form);
            DateTime? rentalStartDate;
            DateTime? rentalEndDate;
            ParseRentalDates(product, form, out rentalStartDate, out rentalEndDate);

            var addWarnings = _shoppingCartService.AddToCart(
                customer,
                product,
                ShoppingCartType.ShoppingCart,
                store.Id,
                attributes,
                customerEnteredPriceConverted,
                rentalStartDate,
                rentalEndDate,
                quantityToAdd,
                false);

            if (addWarnings.Any())
            {
                var message = string.Join(" ", addWarnings.Distinct());
                if (string.IsNullOrWhiteSpace(message))
                    message = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.AddFailed");
                return Json(new { success = false, message });
            }

            _rewardSynchronizationService.TrackManualReward(
                customer,
                store.Id,
                rule.Id,
                product.Id,
                attributes,
                quantityToAdd);

            return Json(new { success = true });
        }

        [ChildActionOnly]
        public ActionResult CartSavings(string widgetZone, object additionalData)
        {
            if (HasAlreadyRenderedForRequest())
                return Content("");

            var store = _storeContext.CurrentStore;
            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return Content("");

            var cart = GetCart();
            if (!cart.Any())
                return Content("");

            var appliedPromotions = _discountManagerPlusService.EvaluateCart(cart, store.Id);
            var pendingRewards = appliedPromotions
                .Where(x => x.RequiresRewardSelection)
                .GroupBy(x => x.PromotionRuleId)
                .Select(g => new
                {
                    PromotionRuleId = g.Key,
                    RewardProductId = g.Select(x => x.RewardProductId).FirstOrDefault(x => x.HasValue && x.Value > 0),
                    RewardQuantity = g.Max(x => x.RewardQuantity)
                })
                .ToList();

            var reminderMessages = BuildTierUpgradeReminders(cart, store.Id, appliedPromotions);
            var model = new CartSavingsModel();

            if (settings.EnableCartSavingsBreakdown)
            {
                var ruleDiscountMap = _discountManagerPlusService.BuildRuleDiscountMap(cart, store.Id);
                var groupedSavings = appliedPromotions
                    .Select(x => new
                    {
                        Promotion = x,
                        EffectiveAmount = ruleDiscountMap.ContainsKey(x.PromotionRuleId) ? ruleDiscountMap[x.PromotionRuleId] : 0m
                    })
                    .Where(x => x.EffectiveAmount > 0)
                    .GroupBy(x => new { x.Promotion.PromotionRuleId, x.Promotion.RuleName })
                    .Select(g => new
                    {
                        g.Key.PromotionRuleId,
                        g.Key.RuleName,
                        DiscountAmount = g.Sum(x => x.EffectiveAmount),
                        RepresentativePromotion = g.Select(x => x.Promotion).FirstOrDefault()
                    })
                    .OrderByDescending(x => x.DiscountAmount)
                    .ToList();

                if (groupedSavings.Any())
                {
                    foreach (var groupedSaving in groupedSavings)
                    {
                        var representative = groupedSaving.RepresentativePromotion;
                        var targetProductName = representative != null ? representative.TargetProductName ?? string.Empty : string.Empty;
                        var appliedToText = !string.IsNullOrEmpty(targetProductName)
                            ? string.Format("(Applied to {0})", targetProductName)
                            : string.Empty;
                        var isCoordinatedDiscount = groupedSavings.Count >= 2 && representative != null &&
                            representative.RuleTypeId == (int)PromotionRuleType.BuyXGetY;
                        var discountPriority = isCoordinatedDiscount
                            ? groupedSavings.Count - groupedSavings.IndexOf(groupedSaving)
                            : 0;

                        model.Items.Add(new CartSavingsItemModel
                        {
                            PromotionRuleId = groupedSaving.PromotionRuleId,
                            RuleName = groupedSaving.RuleName,
                            BadgeText = GetSavingsBadge(representative),
                            DetailText = GetSavingsDetail(representative),
                            IsBogoStyle = representative != null && representative.RuleTypeId == (int)PromotionRuleType.BuyXGetY,
                            DiscountAmount = groupedSaving.DiscountAmount,
                            DiscountAmountFormatted = _priceFormatter.FormatPrice(groupedSaving.DiscountAmount, true, false),
                            TargetProductName = targetProductName,
                            AppliedToText = appliedToText,
                            DiscountType = GetDiscountTypeLabel(representative),
                            TargetSelectionText = GetCheapestItemSelectionText(representative),
                            IsCoordinatedDiscount = isCoordinatedDiscount,
                            DiscountPriority = discountPriority
                        });
                    }

                    model.Title = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Title");
                    model.TotalLabel = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Total");
                    model.TotalSavings = model.Items.Sum(x => x.DiscountAmount);
                    model.TotalSavingsFormatted = _priceFormatter.FormatPrice(model.TotalSavings, true, false);
                    var savingsSummaryTemplate = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings");
                    model.SummaryText = string.Format(savingsSummaryTemplate, model.TotalSavingsFormatted);
                }
            }

            if (pendingRewards.Any())
            {
                model.PendingRewardsTitle = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Title");
                model.PendingRewardsDescription = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Description");
                model.PendingRewardsSelectText = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.SelectReward");
                model.PendingRewardsAddText = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.AddToCart");

                var addToCartUrl = Url.Action("AddRewardToCart", "DiscountManagerPlusPublic",
                    new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Misc.DiscountManagerPlus.Controllers" }, { "area", "" } }) ?? string.Empty;

                var index = 0;
                foreach (var pending in pendingRewards)
                {
                    var rule = _promotionRuleService.GetPromotionRuleById(pending.PromotionRuleId);
                    if (rule == null)
                        continue;

                    var rewardOptionIds = GetPendingRewardOptionProductIds(rule, pending.RewardProductId, store.Id);
                    if (!rewardOptionIds.Any())
                        continue;

                    var applied = appliedPromotions.FirstOrDefault(x => x.PromotionRuleId == pending.PromotionRuleId);
                    var rewardSelection = new CartRewardSelectionModel
                    {
                        PromotionRuleId = pending.PromotionRuleId,
                        RuleName = applied != null ? applied.RuleName : string.Empty,
                        RewardQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1,
                        MaxSelectableQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1,
                        FormId = string.Format("promotion-reward-form-{0}-{1}", pending.PromotionRuleId, index),
                        PopupId = string.Format("promotion-reward-popup-{0}-{1}", pending.PromotionRuleId, index),
                        AddToCartUrl = addToCartUrl
                    };

                    foreach (var rewardOptionId in rewardOptionIds)
                    {
                        var product = _productService.GetProductById(rewardOptionId);
                        if (product == null || product.Deleted || product.ProductType != ProductType.SimpleProduct)
                            continue;

                        var productModel = PrepareRewardProductDetailsModel(product);
                        productModel.AddToCart.EnteredQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1;

                        rewardSelection.Options.Add(new CartRewardOptionModel
                        {
                            RewardProductId = rewardOptionId,
                            RewardProductName = productModel.Name,
                            RewardProductOldPrice = productModel.ProductPrice != null ? productModel.ProductPrice.OldPrice ?? string.Empty : string.Empty,
                            RewardProductPrice = productModel.ProductPrice != null ? productModel.ProductPrice.Price ?? string.Empty : string.Empty,
                            ImageUrl = productModel.PictureModels != null && productModel.PictureModels.Any()
                                ? productModel.PictureModels.First().ImageUrl ?? string.Empty
                                : string.Empty,
                            IsSelected = rewardSelection.Options.Count == 0,
                            Product = productModel
                        });
                    }

                    if (rewardSelection.Options.Any())
                    {
                        model.PendingRewards.Add(rewardSelection);
                        index++;
                    }
                }
            }

            if (reminderMessages.Any())
            {
                model.ReminderTitle = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Title");
                foreach (var reminder in reminderMessages)
                    model.Reminders.Add(reminder);
            }

            foreach (var warning in BuildExcludedProductWarnings(cart, store.Id, appliedPromotions))
                model.ExcludedProductWarnings.Add(warning);

            foreach (var notice in BuildMultipleDiscountNotices(appliedPromotions))
                model.MultipleDiscountNotices.Add(notice);

            foreach (var detail in BuildCheapestItemSelectionDetails(cart, appliedPromotions))
                model.CheapestItemSelectionDetails.Add(detail);

            GenerateDualOfferMessaging(cart, appliedPromotions, model);

            if (!model.Items.Any() && !model.PendingRewards.Any() && !model.Reminders.Any() &&
                !model.ExcludedProductWarnings.Any() && !model.MultipleDiscountNotices.Any() &&
                !model.CheapestItemSelectionDetails.Any() && !model.AttentionMessages.Any())
                return Content("");

            MarkRenderedForRequest();
            return View(ViewRoot + "CartSavings.cshtml", model);
        }

        [ChildActionOnly]
        public ActionResult PromotionBadge(string widgetZone, object additionalData)
        {
            var store = _storeContext.CurrentStore;
            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled || !settings.EnablePromotionBadge)
                return Content("");

            var productId = ResolveProductId(additionalData);
            if (productId <= 0)
                return Content("");

            var customer = _workContext.CurrentCustomer;
            var cart = GetCart().ToList();
            if (!cart.Any(x => x.ProductId == productId))
            {
                cart.Add(new ShoppingCartItem
                {
                    CustomerId = customer.Id,
                    StoreId = store.Id,
                    ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
                    ProductId = productId,
                    Quantity = 1,
                    AttributesXml = string.Empty,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                });
            }

            var appliedPromotions = _discountManagerPlusService.EvaluateCart(cart, store.Id);
            if (!appliedPromotions.Any(x => x.DiscountAmount > 0))
                return Content("");

            var model = new PromotionBadgeModel
            {
                Text = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.PromotionBadge"),
                CssClass = "ns-discount-manager-plus-badge"
            };

            return View(ViewRoot + "PromotionBadge.cshtml", model);
        }

        [ChildActionOnly]
        public ActionResult OffersLink(string widgetZone, object additionalData)
        {
            var store = _storeContext.CurrentStore;
            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return Content("");

            var activeRules = _promotionRuleService.GetActiveRules(store.Id);
            if (!activeRules.Any())
                return Content("");

            var model = new OfferPageLinkModel
            {
                Text = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.LinkText"),
                Url = Url.RouteUrl(DiscountManagerPlusDefaults.OffersRouteName) ?? "/promotions/offers"
            };

            return View(ViewRoot + "OffersLink.cshtml", model);
        }

        private IList<ShoppingCartItem> GetCart()
        {
            return _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart && sci.StoreId == _storeContext.CurrentStore.Id)
                .ToList();
        }

        private int ResolveProductId(object additionalData)
        {
            var overview = additionalData as ProductOverviewModel;
            if (overview != null)
                return overview.Id;

            var details = additionalData as ProductDetailsModel;
            if (details != null)
                return details.Id;

            if (additionalData is int)
                return (int)additionalData;

            return 0;
        }

        private bool IsRewardProductAllowed(PromotionRule rule, int rewardProductId)
        {
            if (rewardProductId <= 0)
                return false;

            if (rule.RuleType == PromotionRuleType.BuyXGetY)
            {
                var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                foreach (var rewardRuleProduct in ruleProducts.Where(x => x.IsRewardProduct))
                {
                    if (MatchesRewardRuleProductSource(rewardRuleProduct, rewardProductId))
                        return true;
                }

                var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
                return tiers.Any(x => x.RewardProductId.HasValue && x.RewardProductId.Value == rewardProductId);
            }

            if (rule.RuleType == PromotionRuleType.ProductBased && rule.DiscountType == DiscountType.FreeItem)
            {
                var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                foreach (var rewardRuleProduct in ruleProducts.Where(x => x.IsRewardProduct))
                {
                    if (MatchesRewardRuleProductSource(rewardRuleProduct, rewardProductId))
                        return true;
                }

                return false;
            }

            return false;
        }

        private bool MatchesRewardRuleProductSource(PromotionRuleProduct rewardRuleProduct, int rewardProductId)
        {
            if (rewardRuleProduct == null || rewardProductId <= 0)
                return false;

            if (rewardRuleProduct.ProductId > 0)
                return rewardRuleProduct.ProductId == rewardProductId;

            if (rewardRuleProduct.IsAllProducts)
                return true;

            var product = _productService.GetProductById(rewardProductId);
            if (product == null || product.Deleted)
                return false;

            if (rewardRuleProduct.VendorId.HasValue && rewardRuleProduct.VendorId.Value > 0)
                return product.VendorId == rewardRuleProduct.VendorId.Value;

            if (rewardRuleProduct.ManufacturerId.HasValue && rewardRuleProduct.ManufacturerId.Value > 0)
            {
                var manufacturerMappings = _manufacturerService.GetProductManufacturersByProductId(rewardProductId, true);
                return manufacturerMappings.Any(x => x.ManufacturerId == rewardRuleProduct.ManufacturerId.Value);
            }

            if (rewardRuleProduct.CategoryId.HasValue && rewardRuleProduct.CategoryId.Value > 0)
            {
                var productCategoryIds = new HashSet<int>(_categoryService.GetProductCategoriesByProductId(rewardProductId, true).Select(x => x.CategoryId));
                if (!productCategoryIds.Any())
                    return false;

                var rewardCategoryIds = new HashSet<int>(_categoryService.GetAllCategoriesByParentCategoryId(rewardRuleProduct.CategoryId.Value, true, true).Select(x => x.Id));
                rewardCategoryIds.Add(rewardRuleProduct.CategoryId.Value);
                return productCategoryIds.Overlaps(rewardCategoryIds);
            }

            return false;
        }

        private bool HasAlreadyRenderedForRequest()
        {
            return HttpContext != null && HttpContext.Items.Contains(RenderedRequestItemKey);
        }

        private void MarkRenderedForRequest()
        {
            if (HttpContext != null)
                HttpContext.Items[RenderedRequestItemKey] = true;
        }

        private IList<int> GetPendingRewardOptionProductIds(PromotionRule rule, int? appliedRewardProductId, int storeId)
        {
            var productIds = new List<int>();

            if (appliedRewardProductId.HasValue && appliedRewardProductId.Value > 0)
                productIds.Add(appliedRewardProductId.Value);

            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            foreach (var rewardProductId in ruleProducts.Where(x => x.IsRewardProduct && x.ProductId > 0).Select(x => x.ProductId).Distinct())
            {
                if (!productIds.Contains(rewardProductId))
                    productIds.Add(rewardProductId);
            }

            foreach (var rewardRuleProduct in ruleProducts.Where(x => x.IsRewardProduct))
            {
                foreach (var sourceProductId in GetRewardOptionProductIdsByRuleProduct(rewardRuleProduct, storeId))
                {
                    if (!productIds.Contains(sourceProductId))
                        productIds.Add(sourceProductId);
                }
            }

            if (productIds.Any())
                return productIds;

            var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            foreach (var rewardProductId in tiers.Where(x => x.RewardProductId.HasValue && x.RewardProductId.Value > 0).Select(x => x.RewardProductId.Value).Distinct())
            {
                if (!productIds.Contains(rewardProductId))
                    productIds.Add(rewardProductId);
            }

            return productIds;
        }

        private IList<int> GetRewardOptionProductIdsByRuleProduct(PromotionRuleProduct rewardRuleProduct, int storeId)
        {
            if (rewardRuleProduct == null)
                return new List<int>();

            if (rewardRuleProduct.ProductId > 0)
                return new List<int> { rewardRuleProduct.ProductId };

            if (rewardRuleProduct.IsAllProducts)
            {
                var products = _productService.SearchProducts(
                    pageIndex: 0,
                    pageSize: 2000,
                    storeId: storeId,
                    visibleIndividuallyOnly: true,
                    overridePublished: true);
                return products.Select(x => x.Id).Distinct().ToList();
            }

            if (rewardRuleProduct.CategoryId.HasValue && rewardRuleProduct.CategoryId.Value > 0)
            {
                var products = _productService.SearchProducts(
                    pageIndex: 0,
                    pageSize: 2000,
                    categoryIds: new List<int> { rewardRuleProduct.CategoryId.Value },
                    storeId: storeId,
                    visibleIndividuallyOnly: true,
                    overridePublished: true);
                return products.Select(x => x.Id).Distinct().ToList();
            }

            if (rewardRuleProduct.ManufacturerId.HasValue && rewardRuleProduct.ManufacturerId.Value > 0)
            {
                var products = _productService.SearchProducts(
                    pageIndex: 0,
                    pageSize: 2000,
                    manufacturerId: rewardRuleProduct.ManufacturerId.Value,
                    storeId: storeId,
                    visibleIndividuallyOnly: true,
                    overridePublished: true);
                return products.Select(x => x.Id).Distinct().ToList();
            }

            if (rewardRuleProduct.VendorId.HasValue && rewardRuleProduct.VendorId.Value > 0)
            {
                var products = _productService.SearchProducts(
                    pageIndex: 0,
                    pageSize: 2000,
                    vendorId: rewardRuleProduct.VendorId.Value,
                    storeId: storeId,
                    visibleIndividuallyOnly: true,
                    overridePublished: true);
                return products.Select(x => x.Id).Distinct().ToList();
            }

            return new List<int>();
        }

        private ProductDetailsModel PrepareRewardProductDetailsModel(Product product)
        {
            var model = new ProductDetailsModel
            {
                Id = product.Id,
                Name = product.GetLocalized(x => x.Name),
                IsRental = product.IsRental
            };

            model.ProductPrice.Price = _priceFormatter.FormatPrice(_priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer, includeDiscounts: true));
            if (product.OldPrice > 0)
                model.ProductPrice.OldPrice = _priceFormatter.FormatPrice(product.OldPrice);

            model.AddToCart.ProductId = product.Id;
            model.AddToCart.EnteredQuantity = product.OrderMinimumQuantity;
            model.AddToCart.CustomerEntersPrice = product.CustomerEntersPrice;
            model.AddToCart.IsRental = product.IsRental;
            if (product.CustomerEntersPrice)
            {
                var minimumCustomerEnteredPrice = _currencyService.ConvertFromPrimaryStoreCurrency(product.MinimumCustomerEnteredPrice, _workContext.WorkingCurrency);
                var maximumCustomerEnteredPrice = _currencyService.ConvertFromPrimaryStoreCurrency(product.MaximumCustomerEnteredPrice, _workContext.WorkingCurrency);
                model.AddToCart.CustomerEnteredPrice = minimumCustomerEnteredPrice;
                model.AddToCart.CustomerEnteredPriceRange = string.Format(
                    _localizationService.GetResource("Products.EnterProductPrice.Range"),
                    _priceFormatter.FormatPrice(minimumCustomerEnteredPrice, false, false),
                    _priceFormatter.FormatPrice(maximumCustomerEnteredPrice, false, false));
            }

            model.GiftCard.IsGiftCard = product.IsGiftCard;
            if (product.IsGiftCard)
            {
                model.GiftCard.GiftCardType = product.GiftCardType;
                model.GiftCard.SenderName = _workContext.CurrentCustomer.GetFullName();
                model.GiftCard.SenderEmail = _workContext.CurrentCustomer.Email;
            }

            var pictures = _pictureService.GetPicturesByProductId(product.Id);
            foreach (var picture in pictures)
            {
                model.PictureModels.Add(new PictureModel
                {
                    ImageUrl = _pictureService.GetPictureUrl(picture, _mediaSettings.ProductThumbPictureSize),
                    FullSizeImageUrl = _pictureService.GetPictureUrl(picture)
                });
            }

            var productAttributeMapping = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in productAttributeMapping)
            {
                var attributeModel = new ProductDetailsModel.ProductAttributeModel
                {
                    Id = attribute.Id,
                    ProductId = product.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = attribute.ProductAttribute.GetLocalized(x => x.Name),
                    Description = attribute.ProductAttribute.GetLocalized(x => x.Description),
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    DefaultValue = attribute.DefaultValue,
                    HasCondition = !string.IsNullOrEmpty(attribute.ConditionAttributeXml)
                };
                if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                if (attribute.ShouldHaveValues())
                {
                    foreach (var attributeValue in _productAttributeService.GetProductAttributeValues(attribute.Id))
                    {
                        attributeModel.Values.Add(new ProductDetailsModel.ProductAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetLocalized(x => x.Name),
                            ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                            IsPreSelected = attributeValue.IsPreSelected
                        });
                    }
                }

                model.ProductAttributes.Add(attributeModel);
            }

            return model;
        }

        private string GetSavingsBadge(AppliedPromotion promotion)
        {
            if (promotion == null)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");

            if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo");

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Percentage");

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount ||
                promotion.DiscountTypeId == (int)DiscountType.FixedBundlePrice)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Fixed");

            return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
        }

        private string GetSavingsDetail(AppliedPromotion promotion)
        {
            if (promotion == null)
                return string.Empty;

            if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
            {
                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                {
                    return string.Format(
                        _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoFree"),
                        promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1);
                }

                return string.Format(
                    _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoDiscounted"),
                    promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1);
            }

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Percentage");

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount ||
                promotion.DiscountTypeId == (int)DiscountType.FixedBundlePrice)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Fixed");

            return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Default");
        }

        private IList<string> BuildTierUpgradeReminders(IList<ShoppingCartItem> cart, int storeId, IList<AppliedPromotion> appliedPromotions)
        {
            var reminders = new List<string>();
            if (cart == null || !cart.Any())
                return reminders;

            appliedPromotions = appliedPromotions ?? new List<AppliedPromotion>();
            var mandatorySelectionRuleIds = new HashSet<int>(appliedPromotions.Where(x => x.RequiresRewardSelection).Select(x => x.PromotionRuleId).Distinct());

            var activeRules = _promotionRuleService.GetActiveRules(storeId);
            foreach (var rule in activeRules.Where(x => x.RuleType == PromotionRuleType.BuyXGetY))
            {
                if (mandatorySelectionRuleIds.Contains(rule.Id))
                    continue;

                var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id).Where(x => x.MinQuantity > 0).OrderBy(x => x.MinQuantity).ToList();
                if (tiers.Count < 2)
                    continue;

                var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
                var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
                if (!buyProducts.Any())
                    continue;

                var eligibleItems = buyProducts.All(x => x.IsAllProducts)
                    ? cart.Where(x => x.Quantity > 0).ToList()
                    : _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);

                var eligibleQuantity = eligibleItems.Sum(x => Math.Max(0, x.Quantity));
                if (eligibleQuantity <= 0)
                    continue;

                var reminder = BuildTierUpgradeReminder(rule, tiers, ruleProducts, eligibleQuantity);
                if (!string.IsNullOrWhiteSpace(reminder))
                    reminders.Add(reminder);
            }

            return reminders.Distinct().ToList();
        }

        private string BuildTierUpgradeReminder(PromotionRule rule, IList<PromotionRuleTier> tiers, IList<PromotionRuleProduct> ruleProducts, int eligibleQuantity)
        {
            if (rule == null || tiers == null || !tiers.Any() || eligibleQuantity <= 0)
                return string.Empty;

            ruleProducts = ruleProducts ?? new List<PromotionRuleProduct>();
            var hasExplicitRewardProducts = ruleProducts.Any(x => x.IsRewardProduct && x.ProductId > 0);
            var hasTierSpecificRewardProducts = tiers.Any(x => x.RewardProductId.HasValue && x.RewardProductId.Value > 0);
            var hasExplicitRewardScope = hasExplicitRewardProducts || hasTierSpecificRewardProducts;

            var tierSnapshots = tiers
                .Select(x => new
                {
                    Tier = x,
                    RewardQuantity = x.RewardQuantity > 0 ? x.RewardQuantity : 1,
                    RequiredGroupSize = hasExplicitRewardScope
                        ? x.MinQuantity
                        : x.MinQuantity + (x.RewardQuantity > 0 ? x.RewardQuantity : 1)
                })
                .Where(x => x.RequiredGroupSize > 0)
                .OrderBy(x => x.RequiredGroupSize)
                .ToList();
            if (tierSnapshots.Count < 2)
                return string.Empty;

            var nextTierTarget = tierSnapshots.FirstOrDefault(x => eligibleQuantity < x.RequiredGroupSize);
            if (nextTierTarget == null)
                return string.Empty;

            var addQuantity = nextTierTarget.RequiredGroupSize - eligibleQuantity;
            if (addQuantity <= 0)
                return string.Empty;

            var offerText = BuildTierOfferText(nextTierTarget.Tier);
            var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Template");
            return string.Format(template, addQuantity, offerText, rule.Name);
        }

        private string BuildTierOfferText(PromotionRuleTier tier)
        {
            if (tier == null)
                return string.Empty;

            if (tier.DiscountType == DiscountType.FreeItem)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Free");

            if (tier.DiscountType == DiscountType.Percentage)
            {
                var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Percentage");
                return string.Format(template, tier.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture));
            }

            if (tier.DiscountType == DiscountType.FixedAmount)
            {
                var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Fixed");
                return string.Format(template, tier.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture));
            }

            return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Discount");
        }

        private IList<string> BuildExcludedProductWarnings(IList<ShoppingCartItem> cart, int storeId, IList<AppliedPromotion> appliedPromotions)
        {
            var warnings = new List<string>();
            if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
                return warnings;

            try
            {
                var activeRules = _promotionRuleService.GetActiveRules(storeId);
                foreach (var rule in activeRules.Where(x => x.IsActive))
                {
                    var ruleConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
                    var excludedProductIds = ruleConditions
                        .Where(x => x.ExcludedProductId.HasValue && x.ExcludedProductId.Value > 0)
                        .Select(x => x.ExcludedProductId.Value)
                        .Distinct()
                        .ToList();

                    if (!excludedProductIds.Any())
                        continue;

                    var cartExcludedProducts = cart.Where(item => excludedProductIds.Contains(item.ProductId)).Select(item => item.ProductId).Distinct().ToList();
                    if (!cartExcludedProducts.Any())
                        continue;

                    var excludedProductNames = new List<string>();
                    foreach (var productId in cartExcludedProducts)
                    {
                        var product = _productService.GetProductById(productId);
                        if (product != null && !string.IsNullOrEmpty(product.Name))
                            excludedProductNames.Add(product.Name);
                    }

                    if (excludedProductNames.Any())
                    {
                        var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.ExcludedProducts");
                        warnings.Add(string.Format(template, string.Join(", ", excludedProductNames), rule.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error building excluded product warnings", ex);
            }

            return warnings;
        }

        private IList<string> BuildMultipleDiscountNotices(IList<AppliedPromotion> appliedPromotions)
        {
            var notices = new List<string>();
            if (appliedPromotions == null || !appliedPromotions.Any())
                return notices;

            var appliedBuyXGetYRules = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && x.DiscountAmount > 0)
                .ToList();

            if (appliedBuyXGetYRules.Count >= 2)
            {
                var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleDiscounts");
                notices.Add(string.Format(template, appliedBuyXGetYRules.Count));

                var coordinationTemplate = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.CoordinatedDiscounts");
                notices.Add(string.Format(coordinationTemplate, appliedBuyXGetYRules.Count));

                foreach (var promotion in appliedBuyXGetYRules.OrderByDescending(x => x.DiscountAmount))
                {
                    string discountTypeText;
                    if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                        discountTypeText = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo");
                    else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                        discountTypeText = string.Format("{0}% {1}", promotion.DiscountValue.ToString("0.##"), _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount"));
                    else
                        discountTypeText = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");

                    var targetProductText = !string.IsNullOrEmpty(promotion.TargetProductName)
                        ? string.Format("{0} {1}", _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedTo"), promotion.TargetProductName)
                        : _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedToCheapest");

                    notices.Add(string.Format("{0}: {1} - {2}", promotion.RuleName, discountTypeText, targetProductText));
                }
            }

            return notices;
        }

        private IList<string> BuildCheapestItemSelectionDetails(IList<ShoppingCartItem> cart, IList<AppliedPromotion> appliedPromotions)
        {
            var details = new List<string>();
            if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
                return details;

            try
            {
                var cheapestItemPromotions = appliedPromotions
                    .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && x.LineDiscounts != null && x.LineDiscounts.Any(d => d.Value > 0))
                    .ToList();

                var isDualOfferScenario = cheapestItemPromotions.Count >= 2;

                foreach (var promotion in cheapestItemPromotions)
                {
                    if (promotion.DiscountAmount <= 0)
                        continue;

                    if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
                    {
                        foreach (var lineDiscount in promotion.LineDiscounts.Where(x => x.Value > 0))
                        {
                            var discountedItem = cart.FirstOrDefault(x => x.Id == lineDiscount.Key);
                            if (discountedItem == null)
                                continue;

                            var product = _productService.GetProductById(discountedItem.ProductId);
                            if (product == null)
                                continue;

                            details.Add(BuildCheapestItemMessage(promotion, product.Name, lineDiscount.Value));
                        }
                    }
                    else if (!string.IsNullOrEmpty(promotion.TargetProductName))
                    {
                        details.Add(BuildCheapestItemMessage(promotion, promotion.TargetProductName, promotion.DiscountAmount));
                    }
                }

                if (isDualOfferScenario && details.Any())
                {
                    var coordinationSummary = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.DualOfferSummary");
                    details.Insert(0, coordinationSummary);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error building cheapest item selection details", ex);
            }

            return details;
        }

        private string BuildCheapestItemMessage(AppliedPromotion promotion, string productName, decimal amount)
        {
            var discountAmount = _priceFormatter.FormatPrice(amount, true, false);
            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                var freeItemTemplate = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied");
                return string.Format(freeItemTemplate, productName, discountAmount);
            }

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
            {
                var percentageTemplate = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied");
                var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
                return string.Format(percentageTemplate, productName, percentage, discountAmount);
            }

            var discountType = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
            var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemDetail");
            return string.Format(template, discountType, productName, discountAmount);
        }

        private string GetDiscountTypeLabel(AppliedPromotion promotion)
        {
            if (promotion == null)
                return string.Empty;

            if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
            {
                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo");

                var discountPercent = promotion.DiscountValue > 0 ? promotion.DiscountValue : 50;
                return string.Format(
                    _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoDiscounted"),
                    promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1, discountPercent);
            }

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return string.Format(_localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Percentage"), promotion.DiscountValue.ToString("0.##"));

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Fixed");

            return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Default");
        }

        private string GetCheapestItemSelectionText(AppliedPromotion promotion)
        {
            if (promotion == null)
                return string.Empty;

            if (promotion.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
                return string.Empty;

            var targetProduct = promotion.TargetProductName;
            if (string.IsNullOrEmpty(targetProduct) && promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleOfferApplied");

            if (string.IsNullOrEmpty(targetProduct))
                return string.Empty;

            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                var template = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemSelected");
                return string.Format(template, targetProduct);
            }

            var discountTemplate = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemSelectedDiscount");
            var discountPercent = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
            return string.Format(discountTemplate, targetProduct, discountPercent);
        }

        private void GenerateDualOfferMessaging(IList<ShoppingCartItem> cart, IList<AppliedPromotion> appliedPromotions, CartSavingsModel model)
        {
            if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
                return;

            try
            {
                var buyXGetYPromotions = appliedPromotions
                    .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                    .ToList();

                if (buyXGetYPromotions.Count < 2)
                    return;

                var emptyAllocations = new Dictionary<int, List<DiscountAllocation>>();
                var attentionMessages = _promotionAttentionMessageService.GenerateDualOfferMessages(appliedPromotions, cart, emptyAllocations);
                if (attentionMessages != null)
                {
                    foreach (var message in attentionMessages)
                        model.AttentionMessages.Add(message);
                }

                var dualOfferExplanation = _promotionAttentionMessageService.GenerateDualOfferExplanation(appliedPromotions, emptyAllocations, cart);
                if (dualOfferExplanation != null && dualOfferExplanation.IsDualOfferScenario)
                    model.DualOfferExplanation = dualOfferExplanation;

                var eligibleProductIds = cart.Select(x => x.ProductId).ToList();
                var maximizationTips = _promotionAttentionMessageService.GenerateMaximizationTips(appliedPromotions, cart, eligibleProductIds);
                if (maximizationTips != null)
                {
                    foreach (var tip in maximizationTips.OrderByDescending(t => t.Priority).Take(5))
                        model.MaximizationTips.Add(tip);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error generating dual-offer messaging", ex);
            }
        }

        private decimal ParseCustomerEnteredPrice(Product product, FormCollection form)
        {
            if (product == null || !product.CustomerEntersPrice || form == null)
                return decimal.Zero;

            foreach (var formKey in form.AllKeys)
            {
                if (formKey.Equals(string.Format("addtocart_{0}.CustomerEnteredPrice", product.Id), StringComparison.InvariantCultureIgnoreCase))
                {
                    decimal customerEnteredPrice;
                    if (decimal.TryParse(form[formKey], out customerEnteredPrice))
                        return _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice, _workContext.WorkingCurrency);
                }
            }

            return decimal.Zero;
        }

        private string ParseProductAttributes(Product product, FormCollection form)
        {
            var attributesXml = "";
            var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in productAttributes)
            {
                var controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                int selectedAttributeId;
                                if (int.TryParse(ctrlAttributes, out selectedAttributeId) && selectedAttributeId > 0)
                                    attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                foreach (var item in ctrlAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    int selectedAttributeId;
                                    if (int.TryParse(item, out selectedAttributeId) && selectedAttributeId > 0)
                                        attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues.Where(v => v.IsPreSelected).Select(v => v.Id).ToList())
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, ctrlAttributes.Trim());
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            var day = form[controlId + "_day"];
                            var month = form[controlId + "_month"];
                            var year = form[controlId + "_year"];
                            try
                            {
                                var selectedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedDate.ToString("D"));
                            }
                            catch
                            {
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            Guid downloadGuid;
                            Guid.TryParse(form[controlId], out downloadGuid);
                            attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, downloadGuid.ToString());
                        }
                        break;
                }
            }

            foreach (var attribute in productAttributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(attribute, attributesXml);
                if (conditionMet.HasValue && !conditionMet.Value)
                    attributesXml = _productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
            }

            if (product.IsGiftCard)
            {
                string recipientName = "";
                string recipientEmail = "";
                string senderName = "";
                string senderEmail = "";
                string giftCardMessage = "";
                foreach (var formKey in form.AllKeys)
                {
                    if (formKey.Equals(string.Format("giftcard_{0}.RecipientName", product.Id), StringComparison.InvariantCultureIgnoreCase))
                        recipientName = form[formKey];
                    else if (formKey.Equals(string.Format("giftcard_{0}.RecipientEmail", product.Id), StringComparison.InvariantCultureIgnoreCase))
                        recipientEmail = form[formKey];
                    else if (formKey.Equals(string.Format("giftcard_{0}.SenderName", product.Id), StringComparison.InvariantCultureIgnoreCase))
                        senderName = form[formKey];
                    else if (formKey.Equals(string.Format("giftcard_{0}.SenderEmail", product.Id), StringComparison.InvariantCultureIgnoreCase))
                        senderEmail = form[formKey];
                    else if (formKey.Equals(string.Format("giftcard_{0}.Message", product.Id), StringComparison.InvariantCultureIgnoreCase))
                        giftCardMessage = form[formKey];
                }

                attributesXml = _productAttributeParser.AddGiftCardAttribute(attributesXml, recipientName, recipientEmail, senderName, senderEmail, giftCardMessage);
            }

            return attributesXml;
        }

        private void ParseRentalDates(Product product, FormCollection form, out DateTime? startDate, out DateTime? endDate)
        {
            startDate = null;
            endDate = null;
            if (product == null || !product.IsRental || form == null)
                return;

            var startControlId = string.Format("rental_start_date_{0}", product.Id);
            var endControlId = string.Format("rental_end_date_{0}", product.Id);
            try
            {
                const string datePickerFormat = "MM/dd/yyyy";
                startDate = DateTime.ParseExact(form[startControlId], datePickerFormat, CultureInfo.InvariantCulture);
                endDate = DateTime.ParseExact(form[endControlId], datePickerFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
            }
        }
    }
}
