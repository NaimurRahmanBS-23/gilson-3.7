using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kendo.Mvc.Extensions;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Customers;
using Nop.Services.Topics;

namespace Bss.Nop.Plugin.Custom.ServiceOverrides
{
    public class BssShippingService : ShippingService
    {

        private readonly ISettingService _settingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        
        public BssShippingService(IRepository<ShippingMethod> shippingMethodRepository, IRepository<DeliveryDate> deliveryDateRepository, IRepository<Warehouse> warehouseRepository, ILogger logger, IProductService productService, IProductAttributeParser productAttributeParser, ICheckoutAttributeParser checkoutAttributeParser, IGenericAttributeService genericAttributeService, ILocalizationService localizationService, IAddressService addressService, ShippingSettings shippingSettings, IPluginFinder pluginFinder, IStoreContext storeContext, IEventPublisher eventPublisher, ShoppingCartSettings shoppingCartSettings, ICacheManager cacheManager, ISettingService settingService) :
        base(shippingMethodRepository, deliveryDateRepository, warehouseRepository, logger, productService, productAttributeParser, checkoutAttributeParser, genericAttributeService, localizationService, addressService, shippingSettings, pluginFinder, storeContext, eventPublisher, shoppingCartSettings, cacheManager)
        {
            _settingService = settingService;
            _genericAttributeService = genericAttributeService;
            _checkoutAttributeParser = checkoutAttributeParser;
        }
        
        public override GetShippingOptionResponse GetShippingOptions(IList<ShoppingCartItem> cart,
            Address shippingAddress, string allowedShippingRateComputationMethodSystemName = "",
            int storeId = 0)
        {
            var shippingRateComputationMethods = LoadActiveShippingRateComputationMethods(storeId);
            //container for USPS response
            GetShippingOptionResponse shippingResponseUsps = null;
            //container for FedEx response
            GetShippingOptionResponse shippingResponseFedEx = null;
            //container for FedExFreight response
            GetShippingOptionResponse shippingResponseFedExFreight = null;

            //var customer = cart.GetCustomer();

            GetShippingOptionResponse combinedShippingOptions = new GetShippingOptionResponse
            {
                ShippingOptions = new List<ShippingOption>()
            };


            //MSS- Check for cart items            
            if (!cart.Any())
            {
                combinedShippingOptions.Errors = new List<string> { "Your cart is empty." };
                return combinedShippingOptions;
            }

            //MSS- Check for shipping address            
            if (string.IsNullOrWhiteSpace(shippingAddress.Country?.TwoLetterIsoCode) ||
                string.IsNullOrWhiteSpace(shippingAddress.ZipPostalCode) || 
                string.IsNullOrWhiteSpace(shippingAddress.City) || 
                string.IsNullOrWhiteSpace(shippingAddress.Address1))
            {
                combinedShippingOptions.Errors = new List<string>{"Please enter your shipping address information."};
                return combinedShippingOptions;
            }


            //MSS - Catalog only
            if (cart.Count() == 1 && cart.Any(i => i.Product.Sku == "GILSON CATALOG"))
            {
                combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = "CatalogOnly",
                    Rate = 0,
                    Name = "USPS Bound Printed Material",
                    Description = "Please allow 2-3 weeks for delivery."
                });
                return combinedShippingOptions;
            }


            //MSS - Set ShippingFromMultipleLocations
            combinedShippingOptions.ShippingFromMultipleLocations = cart.Select(c => c.Product.WarehouseId).Distinct().Count() > 1;


            //MSS - International shipping
            if (!string.IsNullOrWhiteSpace(shippingAddress.Country?.TwoLetterIsoCode))
            {
                if (shippingAddress.Country.TwoLetterIsoCode != "US")
                {
                    combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                    {
                        ShippingRateComputationMethodSystemName = "International",
                        Rate = 0,
                        Name = "International",
                        Description = "A customer service representative will contact you with a shipping quote."
                    });
                    return combinedShippingOptions;
                }
            }

            //Eval Shipping Scenarios 
            if (!CanShipUSPSOrFedExMainWarehouse(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx))
            {
                if (!CanShipFedExMultipleWarehouses(cart, shippingAddress, storeId, ref shippingResponseFedEx))
                {
                    if (!CanShipFreight(cart, shippingAddress, storeId, ref shippingResponseFedExFreight))
                    {
                        if (!CanShipUSPSandFreight(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx))
                        {
                            if (!CanShipFedExandFreight(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx))
                            {

                            }
                        }
                    }
                }
            }
            
            //Add to combinedShippingOptions
            if (shippingResponseUsps?.ShippingOptions != null)
            {
                combinedShippingOptions.ShippingOptions.AddRange(shippingResponseUsps.ShippingOptions);
            }

            if (shippingResponseFedEx?.ShippingOptions != null)
            {
                combinedShippingOptions.ShippingOptions.AddRange(shippingResponseFedEx.ShippingOptions);
            }

            if (shippingResponseFedExFreight?.ShippingOptions != null)
            {
                combinedShippingOptions.ShippingOptions.AddRange(shippingResponseFedExFreight.ShippingOptions);
            }


            //HACK: MSS - Something went wrong.  No shipping options were returned.  Return an error message with text to retry.
            if (!combinedShippingOptions.ShippingOptions.Any())
            {
                //var topicService = EngineContext.Current.Resolve<ITopicService>();
                //var noShippingMethods = topicService.GetTopicBySystemName("Bss.Nop.Custom.NoShippingMethods");
                //combinedShippingOptions.Errors = !string.IsNullOrWhiteSpace(noShippingMethods?.Body) ? 
                //    new List<string> {noShippingMethods.Body} : 
                //    new List<string> { "The system encountered a problem retrieving shipping rates.  Please refresh the page to retry." };

                combinedShippingOptions.Errors = new List<string> { "The system encountered a problem retrieving shipping rates.  Please refresh to retry." };
                return combinedShippingOptions;
            }


            //add in additional handling charge
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            foreach (var shippingOption in combinedShippingOptions.ShippingOptions)
            {
                if (customSettings.IsAdditionalHandlingChargePercent)
                {
                    var percentage = customSettings.AdditionalHandlingCharge / 100;
                    var reductionAmount = shippingOption.Rate * percentage;
                    shippingOption.Rate = shippingOption.Rate + reductionAmount;
                    if (shippingOption.Rate < 0)
                    {
                        shippingOption.Rate = 0;
                    }
                }
                else
                {
                    shippingOption.Rate = shippingOption.Rate + customSettings.AdditionalHandlingCharge;
                    if (shippingOption.Rate < 0)
                    {
                        shippingOption.Rate = 0;
                    }
                }
            }

            //MSS - Set FedEx shipping options description
            var fedExStandardOvernight = combinedShippingOptions.ShippingOptions.FirstOrDefault(so => so.Name == "FedEx Standard Overnight");
            if (fedExStandardOvernight != null) fedExStandardOvernight.Description = "Delivery by 4:30 PM";
            var fedExPriorityOvernight = combinedShippingOptions.ShippingOptions.FirstOrDefault(so => so.Name == "FedEx Priority Overnight");
            if (fedExPriorityOvernight != null) fedExPriorityOvernight.Description = "Delivery by 12:00 PM";
            
            
            //MSS - Order shipping options by Rate (Asc)
            combinedShippingOptions.ShippingOptions = combinedShippingOptions.ShippingOptions.OrderBy(so => so.Rate).ToList();

            //MSS - Collect Shipping
            combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
            {
                ShippingRateComputationMethodSystemName = "Collect",
                Rate = 0,
                Name = "Collect",
                Description = "Have us ship with your account number."
            });

            return combinedShippingOptions;
        }

        private bool CanShipFedExandFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx)
        {
            var cartOfUnder70LBs =
                    cart.Where(x => x.Product.Weight < 70).ToList();

            var cartOver70LBs =
                cart.Where(x => x.Product.Weight >= 70).ToList();

            GetShippingOptionResponse shippingResponseUspsCartUnder70LBs = new GetShippingOptionResponse();
            GetShippingOptionResponse shippingResponseFedExCartUnder70LBs = new GetShippingOptionResponse();
            GetShippingOptionResponse shippingResponseFedExFreight = new GetShippingOptionResponse();

            shippingResponseFedExCartUnder70LBs = base.GetShippingOptions(cartOfUnder70LBs, shippingAddress, "Shipping.Fedex", storeId);
            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExCartUnder70LBs = AdjustFedExShipmentAmounts(shippingResponseFedExCartUnder70LBs, customSettings);
            
            shippingResponseFedExFreight = base.GetShippingOptions(cartOver70LBs, shippingAddress,
                "Shipping.FedExFreight", storeId);
            if (shippingResponseFedExFreight.Errors.Count > 0)
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);
                return false;
            }
            foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
            {
                if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                {
                    shippingOptions.Name = "Motor Freight";
                }
            }
            //shippingResponseFedExFreight.ShippingOptions.RemoveAt(0);
            //adjust freight based on fedex freight shipping adjustment
            customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExFreight = AdjustFreightAmounts(shippingResponseFedExFreight, customSettings);

            foreach (var shippingOption in shippingResponseUspsCartUnder70LBs.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            foreach (var shippingOption in shippingResponseFedExCartUnder70LBs.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            shippingResponseUsps = shippingResponseUspsCartUnder70LBs;
            shippingResponseFedEx = shippingResponseFedExCartUnder70LBs;
            return true;

        }

        private bool CanShipUSPSandFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx)
        {

            //1) All items are < 70lbs and are marked USPS and come from Main Warehouse
            //2) All OTHER items are greater than 70lbs and require frieght regardless of the warehouse shipping from 
            //3) Include Fed Ex for the USPS items only so customer can get expedited shipping

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var settingValue = settingService.GetSettingByKey<string>("warehousesetting.mainwarehouseid");
            int primaryWarehouseId = 0;
            if (settingValue != null)
            {
                if (int.TryParse(settingValue, out primaryWarehouseId))
                {
                }
            }

            bool IsAllFitInPreDefiniedBox = false;
            bool AllUSPSItemsComeFromMainWarehouse = true;
            bool AllOtherItemsAreFreight = false;

            var cartOfMainWareHouseUnder70LBs =
                cart.Where(x => x.Product.WarehouseId == primaryWarehouseId && x.Product.Weight < 70).ToList();

            var cartOver70LBs =
                cart.Where(x => x.Product.Weight >= 70).ToList();

            //make sure everything under 70 is marked USPS
            foreach (var under70 in cart.Where(x => x.Product.Weight < 70).ToList())
            {
                if (under70.Product.WarehouseId != primaryWarehouseId)
                {
                    return false;
                }
            }

            decimal totalProductLength = 0;
            decimal totalProductHeight = 0;
            decimal totalProductWidth = 0;
            decimal totalProductWeight = 0;
            //evaluate the cart
            foreach (var item in cartOfMainWareHouseUnder70LBs)
            {

                totalProductHeight = totalProductHeight + item.Product.Height;
                totalProductLength = totalProductLength + item.Product.Length;
                totalProductWidth = totalProductWidth + item.Product.Width;
                totalProductWeight = totalProductWeight + item.Product.Weight;
                totalProductHeight = totalProductHeight * item.Quantity;
                totalProductLength = totalProductLength * item.Quantity;
                totalProductWidth = totalProductWidth * item.Quantity;
                totalProductWeight = totalProductWeight * item.Quantity;
            }
            //check to see if all the products will fit in the first box
            if (totalProductLength <= 6.88M && totalProductWidth <= 6.88M && totalProductHeight <= 1.18M)
            {
                IsAllFitInPreDefiniedBox = true;
            } //check the second box
            else if (totalProductLength <= 8.46M && totalProductWidth <= 5.11M && totalProductHeight <= 1.57M)
            {
                IsAllFitInPreDefiniedBox = true;
            }

            GetShippingOptionResponse shippingResponseUspsCartUnder70LBs = new GetShippingOptionResponse();
            GetShippingOptionResponse shippingResponseFedExCartUnder70LBs = new GetShippingOptionResponse();
            GetShippingOptionResponse shippingResponseFedExFreight = new GetShippingOptionResponse();
            //get shipping options for USPS
            if (IsAllFitInPreDefiniedBox)
            {
                shippingResponseUspsCartUnder70LBs = base.GetShippingOptions(cartOfMainWareHouseUnder70LBs, shippingAddress,
                       "Shipping.USPS", storeId);

            }
            shippingResponseFedExCartUnder70LBs = base.GetShippingOptions(cartOfMainWareHouseUnder70LBs, shippingAddress,
                "Shipping.Fedex", storeId);
            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExCartUnder70LBs = AdjustFedExShipmentAmounts(shippingResponseFedExCartUnder70LBs, customSettings);

            shippingResponseFedExFreight = base.GetShippingOptions(cartOver70LBs, shippingAddress,
                "Shipping.FedExFreight", storeId);
            if (shippingResponseFedExFreight.Errors.Count > 0)
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);
                return false;
            }
            foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
            {
                if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                {
                    shippingOptions.Name = "Motor Freight";
                }
            }
            //shippingResponseFedExFreight.ShippingOptions.RemoveAt(0);
            //adjust freight based on fedex freight shipping adjustment
            customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExFreight = AdjustFreightAmounts(shippingResponseFedExFreight, customSettings);

            foreach (var shippingOption in shippingResponseUspsCartUnder70LBs.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            foreach (var shippingOption in shippingResponseFedExCartUnder70LBs.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            shippingResponseUsps = shippingResponseUspsCartUnder70LBs;
            shippingResponseFedEx = shippingResponseFedExCartUnder70LBs;
            return true;
        }
        private bool CanShipFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseFedExFreight)
        {
            var FreightOnly = false;
            //Freight Only Scenerio
            //1) All items are over 70lbs
            //2) At least One item is over 70lb and all items come from a single warehouse

            bool IsAllItemsOver70Pounds = true;
            bool IsAtLeastOneOver70 = false;
            bool IsAllFromSingleWarehouse = true;

            int i = 0;
            var previousWarehouseId = 0;
            foreach (var item in cart)
            {
                if (i == 0)
                {
                    previousWarehouseId = item.Product.WarehouseId;
                }
                if (item.Product.Weight < 70)
                {
                    IsAllItemsOver70Pounds = false;
                }
                if (item.Product.Weight >= 70)
                {
                    IsAtLeastOneOver70 = true;
                }
                if (item.Product.WarehouseId != previousWarehouseId)
                {
                    IsAllFromSingleWarehouse = false;
                }
                previousWarehouseId = item.Product.WarehouseId;
                i++;
            }

            if (IsAllItemsOver70Pounds || (IsAtLeastOneOver70 && IsAllFromSingleWarehouse))
            {
                //get shipping options for Fed Ex Freight
                shippingResponseFedExFreight = base.GetShippingOptions(cart, shippingAddress,
                    "Shipping.FedExFreight", storeId);
                if (shippingResponseFedExFreight.Errors.Count > 0)
                {
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);
                    return false;
                }
                foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
                {
                    if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                    {
                        shippingOptions.Name = "Motor Freight";
                    }
                }
                //shippingResponseFedExFreight.ShippingOptions.RemoveAt(0);
                //adjust freight based on fedex freight shipping adjustment
                var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
                shippingResponseFedExFreight = AdjustFreightAmounts(shippingResponseFedExFreight, customSettings);

            }
            else
            {
                return false;
            }
            //massage FedEx
            return true;
        }


        private bool CanShipFedExMultipleWarehouses(IList<ShoppingCartItem> cart, Address shippingAddress,
            int storeId, ref GetShippingOptionResponse shippingResponseFedEx)
        {

            foreach (var item in cart)
            {
                if (item.Product.Weight >= 70)
                {
                    return false;
                }

            }
            shippingResponseFedEx = base.GetShippingOptions(cart, shippingAddress,
    "Shipping.Fedex", storeId);
            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedEx = AdjustFedExShipmentAmounts(shippingResponseFedEx, customSettings);
            return true;
        }

        private bool CanShipUSPSOrFedExMainWarehouse(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx)
        {
            //SCENERIO ONE
            //1) All ship from main warehouse 
            //2) All order items have USPS designation
            //3) All fit in the two predefinied boxes
            //4) All items are under 70 lbs.
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var settingValue = settingService.GetSettingByKey<string>("warehousesetting.mainwarehouseid");
            int primaryWarehouseId = 0;
            if (settingValue != null)
            {
                if(int.TryParse(settingValue, out primaryWarehouseId))
                {
                }
            }
            
            bool IsAllFitInPreDefiniedBox = false;

            decimal totalProductLength = 0;
            decimal totalProductHeight = 0;
            decimal totalProductWidth = 0;
            decimal totalProductWeight = 0;
            //evaluate the cart
            foreach (var item in cart)
            {
                if (item.Product.Weight >= 70)
                {
                    return false;
                }
                if (item.Product.WarehouseId != primaryWarehouseId)
                {
                    return false;
                }
                string keyValue = item.Product.GetAttribute<string>("CanUSPS");
                if (keyValue == null || keyValue == "False")
                {
                    return false;
                }

                totalProductHeight = totalProductHeight + item.Product.Height;
                totalProductLength = totalProductLength + item.Product.Length;
                totalProductWidth = totalProductWidth + item.Product.Width;
                totalProductWeight = totalProductWeight + item.Product.Weight;
                totalProductHeight = totalProductHeight * item.Quantity;
                totalProductLength = totalProductLength * item.Quantity;
                totalProductWidth = totalProductWidth * item.Quantity;
                totalProductWeight = totalProductWeight * item.Quantity;
            }
            //check to see if all the products will fit in the first box
            if (totalProductLength <= 6.88M && totalProductWidth <= 6.88M && totalProductHeight <= 1.18M)
            {
                IsAllFitInPreDefiniedBox = true;
            } //check the second box
            else if (totalProductLength <= 8.46M && totalProductWidth <= 5.11M && totalProductHeight <= 1.57M)
            {
                IsAllFitInPreDefiniedBox = true;
            }
            if (IsAllFitInPreDefiniedBox == false)
            {
                return false;
            }
            //get shipping options for USPS
            shippingResponseUsps = base.GetShippingOptions(cart, shippingAddress,
                   "Shipping.USPS", storeId);
            shippingResponseFedEx = base.GetShippingOptions(cart, shippingAddress,
                "Shipping.Fedex", storeId);
            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedEx = AdjustFedExShipmentAmounts(shippingResponseFedEx, customSettings);
            return true;
        }

        private static GetShippingOptionResponse AdjustFedExShipmentAmounts(GetShippingOptionResponse shippingResponseFedEx,
            BssCustomSettings customSettings)
        {
            foreach (var FedExOption in shippingResponseFedEx.ShippingOptions)
            {
                if (customSettings.IsFedexShippingAdjustmentPercent)
                {
                    var percentage = customSettings.FedexShippingAdjustment / 100;
                    var reductionAmount = FedExOption.Rate * percentage;
                    FedExOption.Rate = FedExOption.Rate + reductionAmount;
                    if (FedExOption.Rate < 0)
                    {
                        FedExOption.Rate = 0;
                    }
                }
                else
                {
                    FedExOption.Rate = FedExOption.Rate + customSettings.FedexShippingAdjustment;
                    if (FedExOption.Rate < 0)
                    {
                        FedExOption.Rate = 0;
                    }
                }
            }
            return shippingResponseFedEx;
        }

        private static GetShippingOptionResponse AdjustFreightAmounts(GetShippingOptionResponse shippingResponseFedExFreight,
            BssCustomSettings customSettings)
        {
            string freightChargesDetail = string.Empty;
            if (System.Web.HttpContext.Current.Session["freightChargesDetail"] != null)
            {
                freightChargesDetail = System.Web.HttpContext.Current.Session["freightChargesDetail"].ToString();
                var settingService = EngineContext.Current.Resolve<ISettingService>();
                var settingValue = settingService.GetSettingByKey<string>("freight.logdetails");
                if (settingValue == "True")
                {
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Information(freightChargesDetail);
                }
            }
            
            foreach (var FreightOption in shippingResponseFedExFreight.ShippingOptions)
            {
                //If it contains a liftgate charges, these must be removed from the total before the discount is applied and then added back in.
                decimal totalLiftGateCharges = 0;

                if (freightChargesDetail.Contains("LIFTGATE_DELIVERY"))
                {
                    string[] chargesDetailArray = freightChargesDetail.Split('|');
                    foreach (var chargesDetailitem in chargesDetailArray)
                    {
                        if (chargesDetailitem.Contains("LIFTGATE_DELIVERY"))
                        {
                            string[] liftGateLine = chargesDetailitem.Split(':');
                            if (liftGateLine.Length == 2)
                            {
                                decimal liftGateCharge = 0;
                                if (decimal.TryParse(liftGateLine[1], out liftGateCharge))
                                {
                                    FreightOption.Rate = FreightOption.Rate - liftGateCharge;
                                    totalLiftGateCharges = totalLiftGateCharges + liftGateCharge;
                                }
                            }
                        }

                    }
                }

                if (customSettings.IsFedexFreightShippingAdjustmentPercent)
                {
                    var percentage = customSettings.FedexFreightShippingAdjustment / 100;
                    var reductionAmount = FreightOption.Rate * percentage;
                    FreightOption.Rate = FreightOption.Rate + reductionAmount;
                    if (FreightOption.Rate < 0)
                    {
                        FreightOption.Rate = 0;
                    }
                }
                else
                {
                    FreightOption.Rate = FreightOption.Rate + customSettings.FedexFreightShippingAdjustment;
                    if (FreightOption.Rate < 0)
                    {
                        FreightOption.Rate = 0;
                    }
                }
                //add liftgate fee back in
                if (totalLiftGateCharges > 0)
                {
                    FreightOption.Rate = FreightOption.Rate + totalLiftGateCharges;
                }

                //clear the description since it would show to the customer otherwise.
                FreightOption.Description = "";
            }
            System.Web.HttpContext.Current.Session["freightChargesDetail"] = null;
            return shippingResponseFedExFreight;
        }
    }
}

