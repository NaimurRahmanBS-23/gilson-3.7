using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Web.Extensions;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.Models.Common;
using Nop.Web.Models.Order;

namespace Nop.Web.Controllers
{
    public partial class OrderController : BasePublicController
    {
		#region Fields

        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IPaymentService _paymentService;
        private readonly ILocalizationService _localizationService;
        private readonly IPdfService _pdfService;
        private readonly IShippingService _shippingService;
        private readonly ICountryService _countryService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IWebHelper _webHelper;
        private readonly IDownloadService _downloadService;
        private readonly IAddressAttributeFormatter _addressAttributeFormatter;
        private readonly IStoreContext _storeContext;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IRewardPointService _rewardPointService;

        private readonly OrderSettings _orderSettings;
        private readonly TaxSettings _taxSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly AddressSettings _addressSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PdfSettings _pdfSettings;

        #endregion

		#region Constructors

        public OrderController(IOrderService orderService, 
            IShipmentService shipmentService, 
            IWorkContext workContext,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IOrderProcessingService orderProcessingService, 
            IDateTimeHelper dateTimeHelper,
            IPaymentService paymentService, 
            ILocalizationService localizationService,
            IPdfService pdfService, 
            IShippingService shippingService,
            ICountryService countryService, 
            IProductAttributeParser productAttributeParser,
            IWebHelper webHelper,
            IDownloadService downloadService,
            IAddressAttributeFormatter addressAttributeFormatter,
            IStoreContext storeContext,
            IOrderTotalCalculationService orderTotalCalculationService,
            IRewardPointService rewardPointService,
            CatalogSettings catalogSettings,
            OrderSettings orderSettings,
            TaxSettings taxSettings,
            ShippingSettings shippingSettings, 
            AddressSettings addressSettings,
            RewardPointsSettings rewardPointsSettings,
            PdfSettings pdfSettings)
        {
            this._orderService = orderService;
            this._shipmentService = shipmentService;
            this._workContext = workContext;
            this._currencyService = currencyService;
            this._priceFormatter = priceFormatter;
            this._orderProcessingService = orderProcessingService;
            this._dateTimeHelper = dateTimeHelper;
            this._paymentService = paymentService;
            this._localizationService = localizationService;
            this._pdfService = pdfService;
            this._shippingService = shippingService;
            this._countryService = countryService;
            this._productAttributeParser = productAttributeParser;
            this._webHelper = webHelper;
            this._downloadService = downloadService;
            this._addressAttributeFormatter = addressAttributeFormatter;
            this._storeContext = storeContext;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._rewardPointService = rewardPointService;

            this._catalogSettings = catalogSettings;
            this._orderSettings = orderSettings;
            this._taxSettings = taxSettings;
            this._shippingSettings = shippingSettings;
            this._addressSettings = addressSettings;
            this._rewardPointsSettings = rewardPointsSettings;
            this._pdfSettings = pdfSettings;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual CustomerOrderListModel PrepareCustomerOrderListModel()
        {
            //HACK: Customization - MSS - Only show Pending nopCommerce orders.  Add GP orders (Open, Completed)

            CustomerOrderListModel model;
            IPagedList<Order> orders;
            switch (Request.QueryString["status"])
            {
                case null:
                case "1":

                    model = new CustomerOrderListModel();
                    orders = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id, 
                                                        customerId: _workContext.CurrentCustomer.Id,
                                                        os: OrderStatus.Pending);

                    foreach (var order in orders)
                    {
                        var orderModel = new CustomerOrderListModel.OrderDetailsModel
                        {
                            Id = order.Id,
                            CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                            OrderStatusEnum = order.OrderStatus,
                            OrderStatus = order.OrderStatus.GetLocalizedEnum(_localizationService, _workContext),
                            IsReturnRequestAllowed = false
                            //IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(order)
                        };
                        var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
                        orderModel.OrderTotal = _priceFormatter.FormatPrice(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);

                        model.Orders.Add(orderModel);
                    }
                    return model;
                    
                case "2":
                case "3":

                    model = new CustomerOrderListModel();

                    //No GilsonCustomerNumber?  GTFO!!!
                    if (string.IsNullOrWhiteSpace(_workContext.CurrentCustomer.GilsonCustomerNumber)) return model;

                    var dataSettingsManager = new Core.Data.DataSettingsManager();
                    var dataProviderSettings = dataSettingsManager.LoadSettings();
                                        
                    using (var conn = new System.Data.SqlClient.SqlConnection(dataProviderSettings.DataConnectionString))
                    {
                        using (var cmd = new System.Data.SqlClient.SqlCommand{Connection = conn, CommandText = "GetGPOrders", CommandType = CommandType.StoredProcedure})
                        {
                            cmd.Parameters.Add("@Type", SqlDbType.Text).Value = Request.QueryString["status"] == "2" ? "Open" : "Completed";
                            cmd.Parameters.Add("@GilsonCustomerNumber", SqlDbType.Text).Value = _workContext.CurrentCustomer.GilsonCustomerNumber;
                            
                            cmd.Connection.Open();
                            var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                            while (dr.Read())
                            {
                                var orderModel = new CustomerOrderListModel.OrderDetailsModel
                                {
                                    GPOrderNumber = (string) dr["OrderNumber"],
                                    CreatedOn = (DateTime) dr["OrderDate"],
                                    OrderStatus = Request.QueryString["status"] == "2" ? "Open" : "Completed",
                                    //IsReturnRequestAllowed = _orderSettings.ReturnRequestsEnabled,
                                    IsReturnRequestAllowed = false,
                                    OrderTotal = (string) dr["OrderTotal"]
                                };

                                model.Orders.Add(orderModel);
                            }
                        }
                    }
                    return model;

                default:
                    return null;
            }
        }

        [NonAction]
        protected virtual OrderDetailsModel PrepareOrderDetailsModel(Order order, bool isPartial = false, bool catalogOnly = false, bool trialDownloadOnly = false)
        {
            if (order == null) throw new ArgumentNullException("order");

            var model = new OrderDetailsModel {IsPartial = isPartial};

            if (isPartial)
            {
                model.CatalogOnly = catalogOnly;
                model.TrialDownloadOnly = trialDownloadOnly;
            }
            else
            {
                //HACK: Customization - MSS - Check for catalog only orders
                if (order.OrderItems.Count == 1)
                {
                    var item = order.OrderItems.FirstOrDefault();
                    if (item != null && item.Product.Sku == "GILSON CATALOG")
                    {
                        model.CatalogOnly = true;
                    }
                }
            }
            
            model.Id = order.Id;
            model.CreatedOn = order.CreatedOnUtc;
            model.OrderStatus = order.OrderStatus.GetLocalizedEnum(_localizationService, _workContext);
            //model.IsReOrderAllowed = _orderSettings.IsReOrderAllowed;
            model.IsReOrderAllowed = false;
            //model.IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(order);
            model.IsReturnRequestAllowed = false;
            model.PdfInvoiceDisabled = _pdfSettings.DisablePdfInvoicesForPendingOrders && order.OrderStatus == OrderStatus.Pending;

            //shipping info
            model.ShippingStatus = order.ShippingStatus.GetLocalizedEnum(_localizationService, _workContext);
            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;
                model.PickUpInStore = order.PickUpInStore;
                if (!order.PickUpInStore)
                {
                    model.ShippingAddress.PrepareModel(
                        address: order.ShippingAddress,
                        excludeProperties: false,
                        addressSettings: _addressSettings,
                        addressAttributeFormatter: _addressAttributeFormatter);
                }
                model.ShippingMethod = order.ShippingMethod;
   

                //shipments (only already shipped)
                var shipments = order.Shipments.Where(x => x.ShippedDateUtc.HasValue).OrderBy(x => x.CreatedOnUtc).ToList();
                foreach (var shipment in shipments)
                {
                    var shipmentModel = new OrderDetailsModel.ShipmentBriefModel
                    {
                        Id = shipment.Id,
                        TrackingNumber = shipment.TrackingNumber,
                    };
                    if (shipment.ShippedDateUtc.HasValue)
                        shipmentModel.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                    if (shipment.DeliveryDateUtc.HasValue)
                        shipmentModel.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                    model.Shipments.Add(shipmentModel);
                }
            }


            //billing info
            model.BillingAddress.PrepareModel(
                address: order.BillingAddress,
                excludeProperties: false,
                addressSettings: _addressSettings,
                addressAttributeFormatter: _addressAttributeFormatter);

            //VAT number
            model.VatNumber = order.VatNumber;

            //payment method
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
            model.PaymentMethod = paymentMethod != null ? paymentMethod.GetLocalizedFriendlyName(_localizationService, _workContext.WorkingLanguage.Id) : order.PaymentMethodSystemName;
            model.PaymentMethodStatus = order.PaymentStatus.GetLocalizedEnum(_localizationService, _workContext);
            model.CanRePostProcessPayment = _paymentService.CanRePostProcessPayment(order);
            //custom values
            model.CustomValues = order.DeserializeCustomValues();

            //order subtotal
            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //including tax

                //order subtotal
                var orderSubtotalInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalInclTax, order.CurrencyRate);
                model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
                //discount (applied to order subtotal)
                var orderSubTotalDiscountInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountInclTax, order.CurrencyRate);
                if (orderSubTotalDiscountInclTaxInCustomerCurrency > decimal.Zero)
                    model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
            }
            else
            {
                //excluding tax

                //order subtotal
                var orderSubtotalExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubtotalExclTax, order.CurrencyRate);
                model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
                //discount (applied to order subtotal)
                var orderSubTotalDiscountExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderSubTotalDiscountExclTax, order.CurrencyRate);
                if (orderSubTotalDiscountExclTaxInCustomerCurrency > decimal.Zero)
                    model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
            }

            if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                //including tax

                //order shipping
                var orderShippingInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingInclTax, order.CurrencyRate);
                model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
                //payment method additional fee
                var paymentMethodAdditionalFeeInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate);
                if (paymentMethodAdditionalFeeInclTaxInCustomerCurrency > decimal.Zero)
                    model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
            }
            else
            {
                //excluding tax

                //order shipping
                var orderShippingExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderShippingExclTax, order.CurrencyRate);
                model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
                //payment method additional fee
                var paymentMethodAdditionalFeeExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate);
                if (paymentMethodAdditionalFeeExclTaxInCustomerCurrency > decimal.Zero)
                    model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
            }

            //tax
            bool displayTax = true;
            bool displayTaxRates = true;
            if (_taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                if (order.OrderTax == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = _taxSettings.DisplayTaxRates && order.TaxRatesDictionary.Count > 0;
                    displayTax = !displayTaxRates;

                    var orderTaxInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTax, order.CurrencyRate);
                    //TODO pass languageId to _priceFormatter.FormatPrice
                    model.Tax = _priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);

                    foreach (var tr in order.TaxRatesDictionary)
                    {
                        model.TaxRates.Add(new OrderDetailsModel.TaxRate
                        {
                            Rate = _priceFormatter.FormatTaxRate(tr.Key),
                            //TODO pass languageId to _priceFormatter.FormatPrice
                            Value = _priceFormatter.FormatPrice(_currencyService.ConvertCurrency(tr.Value, order.CurrencyRate), true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage),
                        });
                    }
                }
            }
            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;
            model.DisplayTaxShippingInfo = _catalogSettings.DisplayTaxShippingInfoOrderDetailsPage;
            model.PricesIncludeTax = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax;

            //discount (applied to order total)
            var orderDiscountInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderDiscount, order.CurrencyRate);
            if (orderDiscountInCustomerCurrency > decimal.Zero)
                model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);


            //gift cards
            foreach (var gcuh in order.GiftCardUsageHistory)
            {
                model.GiftCards.Add(new OrderDetailsModel.GiftCard
                {
                    CouponCode = gcuh.GiftCard.GiftCardCouponCode,
                    Amount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage),
                });
            }

            //reward points           
            if (order.RedeemedRewardPointsEntry != null)
            {
                model.RedeemedRewardPoints = -order.RedeemedRewardPointsEntry.Points;
                model.RedeemedRewardPointsAmount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
            }

            //total
            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
            model.OrderTotal = _priceFormatter.FormatPrice(orderTotalInCustomerCurrency, true, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);

            //checkout attributes
            model.CheckoutAttributeInfo = order.CheckoutAttributeDescription;

            //order notes
            foreach (var orderNote in order.OrderNotes
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList())
            {
                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Id = orderNote.Id,
                    HasDownload = orderNote.DownloadId > 0,
                    Note = orderNote.FormatOrderNoteText(),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }


            //purchased products
            model.ShowSku = _catalogSettings.ShowProductSku;
            var orderItems = _orderService.GetAllOrderItems(order.Id, null, null, null, null, null, null);
            foreach (var orderItem in orderItems)
            {
                var orderItemModel = new OrderDetailsModel.OrderItemModel
                {
                    Id = orderItem.Id,
                    OrderItemGuid = orderItem.OrderItemGuid,
                    Sku = orderItem.Product.FormatSku(orderItem.AttributesXml, _productAttributeParser),
                    ProductId = orderItem.Product.Id,
                    ProductName = orderItem.Product.GetLocalized(x => x.Name),
                    ProductSeName = orderItem.Product.GetSeName(),
                    Quantity = orderItem.Quantity,
                    AttributeInfo = orderItem.AttributeDescription,
                };

                //HACK: MSS - Use parent product SeName for variants
                if (orderItem.Product.ParentGroupedProductId > 0)
                {
                    var _productService =  Nop.Core.Infrastructure.EngineContext.Current.Resolve<IProductService>();
                    var parentProduct = _productService.GetProductById(orderItem.Product.ParentGroupedProductId);
                    if (parentProduct != null)
                    {
                        orderItemModel.ProductSeName = parentProduct.GetSeName();
                    }
                }
                
                //rental info
                if (orderItem.Product.IsRental)
                {
                    var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                    var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                    orderItemModel.RentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);
                }
                model.Items.Add(orderItemModel);

                //unit price, subtotal
                if (order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceInclTax, order.CurrencyRate);
                    orderItemModel.UnitPrice = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);

                    var priceInclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceInclTax, order.CurrencyRate);
                    orderItemModel.SubTotal = _priceFormatter.FormatPrice(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, true);
                }
                else
                {
                    //excluding tax
                    var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.UnitPriceExclTax, order.CurrencyRate);
                    orderItemModel.UnitPrice = _priceFormatter.FormatPrice(unitPriceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);

                    var priceExclTaxInCustomerCurrency = _currencyService.ConvertCurrency(orderItem.PriceExclTax, order.CurrencyRate);
                    orderItemModel.SubTotal = _priceFormatter.FormatPrice(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, _workContext.WorkingLanguage, false);
                }

                //downloadable products
                if (_downloadService.IsDownloadAllowed(orderItem))
                    orderItemModel.DownloadId = orderItem.Product.DownloadId;
                if (_downloadService.IsLicenseDownloadAllowed(orderItem))
                    orderItemModel.LicenseId = orderItem.LicenseDownloadId.HasValue ? orderItem.LicenseDownloadId.Value : 0;
            }

            return model;
        }

        [NonAction]
        protected virtual OrderDetailsModel PrepareGpOrderDetailsModel(string orderNumber)
        {
            //HACK: Customization - MSS - PrepareGPOrderDetailsModel

            //No GilsonCustomerNumber?  GTFO!!!
            if (string.IsNullOrWhiteSpace(_workContext.CurrentCustomer.GilsonCustomerNumber)) return null;
            
            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();

            OrderDetailsModel model;
            
            using (var conn = new SqlConnection(dataProviderSettings.DataConnectionString))
            {
                #region "Order Header"

                using (var cmdHeader = new SqlCommand{Connection = conn, CommandText = "GetGPOrderHeader", CommandType = CommandType.StoredProcedure})
                {
                    cmdHeader.Parameters.Add("@Status", SqlDbType.VarChar).Value = Request.QueryString["status"] == "2" ? "Open" : "Completed";
                    cmdHeader.Parameters.Add("@OrderNumber", SqlDbType.VarChar).Value = orderNumber;

                    cmdHeader.Connection.Open();
                    using (var dr = cmdHeader.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (dr.Read())
                        {
                            //return model for Current Customer only
                            if (_workContext.CurrentCustomer.GilsonCustomerNumber != (string)dr["GilsonCustomerNumber"]) return null;

                            model = new OrderDetailsModel
                            {
                                IsPartial = false,
                                GilsonOrderNumber = (string)dr["OrderNumber"],
                                GilsonCustomerNumber = (string)dr["GilsonCustomerNumber"],
                                CreatedOn = (DateTime) dr["OrderDate"],
                                OrderStatus = (string) dr["Status"],
                                IsReOrderAllowed = false,
                                IsReturnRequestAllowed = _orderSettings.ReturnRequestsEnabled,
                                PdfInvoiceDisabled = _pdfSettings.DisablePdfInvoicesForPendingOrders,
                                BillingAddress = new AddressModel
                                {
                                    FirstName = (string) dr["BillingAddress3"],
                                    Company = (string)dr["BillingName"],
                                    StreetAddressEnabled = true,
                                    Address1 = (string)dr["BillingAddress1"],
                                    Address2 = (string)dr["BillingAddress2"],
                                    City = (string)dr["BillingCity"],
                                    StateProvinceName  = (string)dr["BillingState"],
                                    ZipPostalCode = (string)dr["BillingZip"],
                                    CountryName = (string)dr["BillingCountry"]
                                },
                                IsShippable = true,
                                PickUpInStore = false,
                                ShippingAddress = new AddressModel
                                {
                                    FirstName = (string)dr["ShippingAddress3"],
                                    CompanyEnabled = true,
                                    Company = (string)dr["ShippingName"],
                                    StreetAddressEnabled = true,
                                    Address1 = (string)dr["ShippingAddress1"],
                                    StreetAddress2Enabled = true,
                                    Address2 = (string)dr["ShippingAddress2"],
                                    City = (string)dr["ShippingCity"],
                                    StateProvinceName = (string)dr["ShippingState"],
                                    ZipPostalCode = (string)dr["ShippingZip"],
                                    CountryName = (string)dr["ShippingCountry"]
                                },
                                PaymentMethod = (string)dr["PaymentType"],
                                CustomValues = (string)dr["PaymentType"] == "Purchase Order" ? new Dictionary<string, object> { { "PO Number", (string) dr["CustomerPO"] } } : null,
                                OrderSubtotal = (string)dr["SubTotal"],
                                DisplayTaxRates = false,
                                DisplayTax = true,
                                DisplayTaxShippingInfo = _catalogSettings.DisplayTaxShippingInfoOrderDetailsPage,
                                OrderShipping = (string)dr["Shipping"],
                                Tax = (string) dr["Tax"],
                                OrderTotal = (string)dr["OrderTotal"]
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                #endregion


                #region "Order Detail"

                //purchased products
                model.ShowSku = _catalogSettings.ShowProductSku;
                using (var cmdItems = new SqlCommand{Connection = conn, CommandText = "GetGPOrderDetails", CommandType = CommandType.StoredProcedure})
                {
                    cmdItems.Parameters.Add("@Status", SqlDbType.VarChar).Value = Request.QueryString["status"] == "2" ? "Open" : "Completed";
                    cmdItems.Parameters.Add("@OrderNumber", SqlDbType.VarChar).Value = orderNumber;

                    cmdItems.Connection.Open();
                    using (var dr = cmdItems.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            var orderItemModel = new OrderDetailsModel.OrderItemModel
                            {
                                Sku =  dr.IsDBNull(dr.GetOrdinal("ItemNumber")) ? null : (string) dr["ItemNumber"],
                                ProductName = dr.IsDBNull(dr.GetOrdinal("ProductName")) ? null : (string)dr["ProductName"],
                                ProductSeName = dr.IsDBNull(dr.GetOrdinal("ProductSeName")) ? null : (string)dr["ProductSeName"],
                                Quantity = dr.IsDBNull(dr.GetOrdinal("Quantity")) ? 0 : (int)dr["Quantity"],
                                UnitPrice = dr.IsDBNull(dr.GetOrdinal("UnitPrice")) ? null : (string)dr["UnitPrice"],
                                SubTotal = dr.IsDBNull(dr.GetOrdinal("ExtendedPrice")) ? null : (string)dr["ExtendedPrice"],
                                InvoiceNumber = dr.IsDBNull(dr.GetOrdinal("InvoiceNumber")) ? null : (string)dr["InvoiceNumber"],
                            };
                            model.Items.Add(orderItemModel);
                        }
                    }
                }

                #endregion

            }
            
            return model;
        }


        [NonAction]
        protected virtual ShipmentDetailsModel PrepareShipmentDetailsModel(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = shipment.Order;
            if (order == null)
                throw new Exception("order cannot be loaded");
            var model = new ShipmentDetailsModel();
            
            model.Id = shipment.Id;
            if (shipment.ShippedDateUtc.HasValue)
                model.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
            if (shipment.DeliveryDateUtc.HasValue)
                model.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
            
            //tracking number and shipment information
            if (!String.IsNullOrEmpty(shipment.TrackingNumber))
            {
                model.TrackingNumber = shipment.TrackingNumber;
                var srcm = _shippingService.LoadShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);
                if (srcm != null &&
                    srcm.PluginDescriptor.Installed &&
                    srcm.IsShippingRateComputationMethodActive(_shippingSettings))
                {
                    var shipmentTracker = srcm.ShipmentTracker;
                    if (shipmentTracker != null)
                    {
                        model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
                        if (_shippingSettings.DisplayShipmentEventsToCustomers)
                        {
                            var shipmentEvents = shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                            if (shipmentEvents != null)
                            {
                                foreach (var shipmentEvent in shipmentEvents)
                                {
                                    var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel();
                                    var shipmentEventCountry = _countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                    shipmentStatusEventModel.Country = shipmentEventCountry != null
                                                                           ? shipmentEventCountry.GetLocalized(x => x.Name)
                                                                           : shipmentEvent.CountryCode;
                                    shipmentStatusEventModel.Date = shipmentEvent.Date;
                                    shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                    shipmentStatusEventModel.Location = shipmentEvent.Location;
                                    model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                                }
                            }
                        }
                    }
                }
            }

            //products in this shipment
            model.ShowSku = _catalogSettings.ShowProductSku;
            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = _orderService.GetOrderItemById(shipmentItem.OrderItemId);
                if (orderItem == null)
                    continue;

                var shipmentItemModel = new ShipmentDetailsModel.ShipmentItemModel
                {
                    Id = shipmentItem.Id,
                    Sku = orderItem.Product.FormatSku(orderItem.AttributesXml, _productAttributeParser),
                    ProductId = orderItem.Product.Id,
                    ProductName = orderItem.Product.GetLocalized(x => x.Name),
                    ProductSeName = orderItem.Product.GetSeName(),
                    AttributeInfo = orderItem.AttributeDescription,
                    QuantityOrdered = orderItem.Quantity,
                    QuantityShipped = shipmentItem.Quantity,
                };
                //rental info
                if (orderItem.Product.IsRental)
                {
                    var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                    var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                    shipmentItemModel.RentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);
                }
                model.Items.Add(shipmentItemModel);
            }

            //order details model
            model.Order = PrepareOrderDetailsModel(order);
            
            return model;
        }

        #endregion

        #region Methods

        //My account / Orders
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult CustomerOrders()
        {
            //Not a registered customer?  GTFO!!!
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            var model = PrepareCustomerOrderListModel();

            if (model == null)
            {
                return RedirectToRoute("CustomerOrders");
            }

            return View(model);
        }

        //My account / Orders / Cancel recurring order
        [HttpPost, ActionName("CustomerOrders")]
        [PublicAntiForgery]
        [FormValueRequired(FormValueRequirement.StartsWith, "cancelRecurringPayment")]
        public ActionResult CancelRecurringPayment(FormCollection form)
        {
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            //get recurring payment identifier
            int recurringPaymentId = 0;
            foreach (var formValue in form.AllKeys)
                if (formValue.StartsWith("cancelRecurringPayment", StringComparison.InvariantCultureIgnoreCase))
                    recurringPaymentId = Convert.ToInt32(formValue.Substring("cancelRecurringPayment".Length));

            var recurringPayment = _orderService.GetRecurringPaymentById(recurringPaymentId);
            if (recurringPayment == null)
            {
                return RedirectToRoute("CustomerOrders");
            }

            if (_orderProcessingService.CanCancelRecurringPayment(_workContext.CurrentCustomer, recurringPayment))
            {
                var errors = _orderProcessingService.CancelRecurringPayment(recurringPayment);

                var model = PrepareCustomerOrderListModel();
                model.CancelRecurringPaymentErrors = errors;

                return View(model);
            }
            else
            {
                return RedirectToRoute("CustomerOrders");
            }
        }

        //My account / Reward points
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult CustomerRewardPoints()
        {
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            if (!_rewardPointsSettings.Enabled)
                return RedirectToRoute("CustomerInfo");

            var customer = _workContext.CurrentCustomer;

            var model = new CustomerRewardPointsModel();
            foreach (var rph in _rewardPointService.GetRewardPointsHistory(customer.Id))
            {
                model.RewardPoints.Add(new CustomerRewardPointsModel.RewardPointsHistoryModel
                {
                    Points = rph.Points,
                    PointsBalance = rph.PointsBalance,
                    Message = rph.Message,
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            //current amount/balance
            int rewardPointsBalance = _rewardPointService.GetRewardPointsBalance(customer.Id, _storeContext.CurrentStore.Id);
            decimal rewardPointsAmountBase = _orderTotalCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
            decimal rewardPointsAmount = _currencyService.ConvertFromPrimaryStoreCurrency(rewardPointsAmountBase, _workContext.WorkingCurrency);
            model.RewardPointsBalance = rewardPointsBalance;
            model.RewardPointsAmount = _priceFormatter.FormatPrice(rewardPointsAmount, true, false);
            //minimum amount/balance
            int minimumRewardPointsBalance = _rewardPointsSettings.MinimumRewardPointsToUse;
            decimal minimumRewardPointsAmountBase = _orderTotalCalculationService.ConvertRewardPointsToAmount(minimumRewardPointsBalance);
            decimal minimumRewardPointsAmount = _currencyService.ConvertFromPrimaryStoreCurrency(minimumRewardPointsAmountBase, _workContext.WorkingCurrency);
            model.MinimumRewardPointsBalance = minimumRewardPointsBalance;
            model.MinimumRewardPointsAmount = _priceFormatter.FormatPrice(minimumRewardPointsAmount, true, false);
            return View(model);
        }

        //My account / Order details page
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult Details(string orderId, bool isPartial = false, bool catalogOnly = false, bool trialDownloadOnly = false)
        {
            //HACK: Customization - MSS - Check for Gilson Order Number

            //Not a registered customer?  GTFO!!!
            if (!isPartial && !_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            if (string.IsNullOrWhiteSpace(orderId)) return RedirectToRoute("CustomerOrders");

            if (orderId.StartsWith("O"))
            {
                //status must be 2 (open) or 3 (Completed)
                if (Request.QueryString["status"] == null || (Request.QueryString["status"] != "2" && Request.QueryString["status"] != "3"))
                {
                    return RedirectToRoute("CustomerOrders");
                }

                var model = PrepareGpOrderDetailsModel(orderId);

                if (model == null)
                {
                    return RedirectToRoute("CustomerOrders");
                }

                return View(model);
            }
            else
            {
                int id;
                if (!int.TryParse(orderId, out id)) return RedirectToRoute("CustomerOrders");

                var order = _orderService.GetOrderById(id);

                if (order == null) return RedirectToRoute("CustomerOrders");
                
                if (order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId) return new HttpUnauthorizedResult();

                var model = PrepareOrderDetailsModel(order, isPartial, catalogOnly, trialDownloadOnly);

                if (model == null)
                {
                    return RedirectToRoute("CustomerOrders");
                }

                return View(model);
            }
        }

        //My account / Order details page / Print
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult PrintOrderDetails(string orderId)
        {
            //HACK: Customization - MSS - Check for Gilson Order Number
            if (string.IsNullOrWhiteSpace(orderId)) return RedirectToRoute("CustomerOrders");

            if (orderId.StartsWith("O"))
            {
                var model = PrepareGpOrderDetailsModel(orderId);
                if (model == null)
                {
                    return RedirectToRoute("CustomerOrders");
                }
                model.PrintMode = true;
                return View("Details", model);
            }
            else
            {
                int id;
                if (!int.TryParse(orderId, out id)) return RedirectToRoute("CustomerOrders");

                var order = _orderService.GetOrderById(id);
                if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                    return new HttpUnauthorizedResult();

                var model = PrepareOrderDetailsModel(order);
                model.PrintMode = true;
                return View("Details", model);
            }
        }

        //My account / Order details page / PDF invoice
        public ActionResult GetPdfInvoice(int orderId)
        {
            var order = _orderService.GetOrderById(orderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            var orders = new List<Order>();
            orders.Add(order);
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", string.Format("order_{0}.pdf", order.Id));
        }

        //My account / Order details page / re-order
        public ActionResult ReOrder(int orderId)
        {
            var order = _orderService.GetOrderById(orderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            _orderProcessingService.ReOrder(order);
            return RedirectToRoute("ShoppingCart");
        }

        //My account / Order details page / Complete payment
        [HttpPost, ActionName("Details")]
        [PublicAntiForgery]
        [FormValueRequired("repost-payment")]
        public ActionResult RePostPayment(int orderId)
        {
            var order = _orderService.GetOrderById(orderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            if (!_paymentService.CanRePostProcessPayment(order))
                return RedirectToRoute("OrderDetails", new { orderId = orderId });

            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = order
            };
            _paymentService.PostProcessPayment(postProcessPaymentRequest);

            if (_webHelper.IsRequestBeingRedirected || _webHelper.IsPostBeingDone)
            {
                //redirection or POST has been done in PostProcessPayment
                return Content("Redirected");
            }

            //if no redirection has been done (to a third-party payment page)
            //theoretically it's not possible
            return RedirectToRoute("OrderDetails", new { orderId = orderId });
        }

        //My account / Order details page / Shipment details page
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult ShipmentDetails(int shipmentId)
        {
            var shipment = _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                return new HttpUnauthorizedResult();

            var order = shipment.Order;
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new HttpUnauthorizedResult();

            var model = PrepareShipmentDetailsModel(shipment);

            return View(model);
        }

        #endregion
    }
}
