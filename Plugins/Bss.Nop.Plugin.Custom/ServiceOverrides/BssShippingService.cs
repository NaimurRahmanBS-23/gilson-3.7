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

namespace Bss.Nop.Plugin.Custom.ServiceOverrides
{
    public class BssShippingService : ShippingService
    {

        private readonly ISettingService _settingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ILocalizationService _localizationService;

        public BssShippingService(IRepository<ShippingMethod> shippingMethodRepository, IRepository<DeliveryDate> deliveryDateRepository, IRepository<Warehouse> warehouseRepository, ILogger logger, IProductService productService, IProductAttributeParser productAttributeParser, ICheckoutAttributeParser checkoutAttributeParser, IGenericAttributeService genericAttributeService, ILocalizationService localizationService, IAddressService addressService, ShippingSettings shippingSettings, IPluginFinder pluginFinder, IStoreContext storeContext, IEventPublisher eventPublisher, ShoppingCartSettings shoppingCartSettings, ICacheManager cacheManager, ISettingService settingService) :
        base(shippingMethodRepository, deliveryDateRepository, warehouseRepository, logger, productService, productAttributeParser, checkoutAttributeParser, genericAttributeService, localizationService, addressService, shippingSettings, pluginFinder, storeContext, eventPublisher, shoppingCartSettings, cacheManager)
        {
            _settingService = settingService;
            _genericAttributeService = genericAttributeService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _localizationService = localizationService;
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

            var combinedShippingOptions = new GetShippingOptionResponse
            {
                ShippingOptions = new List<ShippingOption>()
            };

            
            //HACK: MSS - Check for cart items            
            if (!cart.Any())
            {
                combinedShippingOptions.Errors = new List<string> { "Your shoppping cart is empty.  Items are required to calculate shipping." };
                return combinedShippingOptions;
            }

            //HACK: MSS - International shipping
            if (!string.IsNullOrWhiteSpace(shippingAddress.Country?.TwoLetterIsoCode))
            {
                if (shippingAddress.Country.TwoLetterIsoCode != "US")
                {
                    combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                    {
                        ShippingRateComputationMethodSystemName = "International",
                        Rate = 0,
                        Name = "International",
                        Description = _localizationService.GetResource("international.shippingmethod.description", 1)
                    });
                    return combinedShippingOptions;
                }
            }
            else
            {
                combinedShippingOptions.Errors = new List<string> { "Please enter your shipping address information." };
                return combinedShippingOptions;
            }

            //HACK: MSS - Check for shipping address            
            if (string.IsNullOrWhiteSpace(shippingAddress.Address1) ||
                string.IsNullOrWhiteSpace(shippingAddress.City) ||
                string.IsNullOrWhiteSpace(shippingAddress.StateProvince?.Abbreviation) ||
                string.IsNullOrWhiteSpace(shippingAddress.ZipPostalCode))
            {
                combinedShippingOptions.Errors = new List<string> { "Please enter your shipping address information." };
                return combinedShippingOptions;
            }

            //HACK: MSS - Catalog only
            if (cart.All(i => i.Product.Sku == "GILSON CATALOG"))
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

            //HACK: MSS - Downloadable Product(s)
            if (cart.All(x => x.Product.IsDownload))
            {
                combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = "DownloadableProduct",
                    Rate = 0,
                    Name = _localizationService.GetResource("download.shipping.method", 1),
                    Description = _localizationService.GetResource("download.shipping.method.description", 1)
                    
                });
                return combinedShippingOptions;
            }


            //HACK: MSS - Free Shipping
            if (cart.All(x => x.Product.IsFreeShipping))
            {
                combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                {
                    ShippingRateComputationMethodSystemName = "FreeShipping",
                    Rate = 0,
                    Name = "Free Shipping",
                    Description = "All shopping cart items are eligible for free shipping."
                });
                return combinedShippingOptions;
            }


            //HACK: MSS - Set ShippingFromMultipleLocations
            combinedShippingOptions.ShippingFromMultipleLocations = cart.Where(c => c.Product.IsDownload == false).Select(c => c.Product.WarehouseId).Distinct().Count() > 1;

            //Eval Shipping Scenarios 
            if (!CanShipUSPSOrFedExMainWarehouse(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx))
            {
                if (!CanShipFedExMultipleWarehouses(cart, shippingAddress, storeId, ref shippingResponseFedEx))
                {
                    if (!CanShipFreight(cart, shippingAddress, storeId, ref shippingResponseFedExFreight))
                    {
                        //HACK: MSS - Added shippingResponseFedExFreight to return any freight errors.
                        if (!CanShipUSPSandFreight(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx, ref shippingResponseFedExFreight))
                        {
                            //HACK: MSS - Added shippingResponseFedExFreight to return any freight errors.
                            if (!CanShipFedExandFreight(cart, shippingAddress, storeId, ref shippingResponseUsps, ref shippingResponseFedEx, ref shippingResponseFedExFreight))
                            {

                            }
                        }
                    }
                }
            }
            
            //Add to combinedShippingOptions
            combinedShippingOptions.ShippingOptions = new List<ShippingOption>();
            if (shippingResponseUsps != null)
            {
                if (shippingResponseUsps.ShippingOptions != null)
                {
                    combinedShippingOptions.ShippingOptions.AddRange(shippingResponseUsps.ShippingOptions);
                }
                ////HACK: MSS - Return errors from shippingResponseUsps
                else if (shippingResponseUsps.Errors?.Count > 0)
                {
                    combinedShippingOptions.Errors = shippingResponseUsps.Errors;
                    //return combinedShippingOptions;
                }
            }

            if (shippingResponseFedEx != null)
            {
                if (shippingResponseFedEx.ShippingOptions?.Count > 0)
                {
                    combinedShippingOptions.ShippingOptions.AddRange(shippingResponseFedEx.ShippingOptions);
                }
                //HACK: MSS - Return errors from shippingResponseFedEx
                else if (shippingResponseFedEx.Errors?.Count > 0)
                {
                    combinedShippingOptions.Errors = shippingResponseFedEx.Errors;
                    //return combinedShippingOptions;
                }
            }

            if (shippingResponseFedExFreight != null)
            {
                if (shippingResponseFedExFreight.ShippingOptions != null)
                {
                    combinedShippingOptions.ShippingOptions.AddRange(shippingResponseFedExFreight.ShippingOptions);
                }
                //HACK: MSS - Return errors from shippingResponseFedEx
                else if (shippingResponseFedExFreight.Errors?.Count > 0)
                {
                    //HACK: MSS - Handle HI freight shipping error "Shipments to/from HI cannot be auto-rated"
                    if (shippingResponseFedExFreight.Errors.Any(e => e.Contains("Shipments to/from HI cannot be auto-rated")))
                    {
                        combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
                        {
                            ShippingRateComputationMethodSystemName = "HawaiiFreight",
                            Rate = 0,
                            Name = "Hawaii Freight",
                            Description = "Freight shipments to Hawaii cannot be auto-rated.  A customer service representative will contact you with a freight shipping quote."
                        });
                        //return combinedShippingOptions;
                    }
                    else { 
                        //return all other errors.
                        combinedShippingOptions.Errors = shippingResponseFedExFreight.Errors;
                        //return combinedShippingOptions;
                    }
                    
                }
            }

            //HACK: MSS: Return Errors
            if (!combinedShippingOptions.ShippingOptions.Any() && combinedShippingOptions.Errors.Any())
            {
                var ei = combinedShippingOptions.Errors.IndexOf("Destination Postal-State Mismatch.");
                if (ei != -1)
                {
                    combinedShippingOptions.Errors[ei] = "Shipping Zip / Postal Code is missing or invalid";
                }
                    

                return combinedShippingOptions;

            }

            //HACK: MSS - Something went wrong.  No shipping options were returned.  Return an error message with text to retry.
            if (!combinedShippingOptions.ShippingOptions.Any() && !combinedShippingOptions.Errors.Any())
            {
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

            //HACK: MSS - Set FedEx shipping options description
            var fedExStandardOvernight = combinedShippingOptions.ShippingOptions.FirstOrDefault(so => so.Name == "FedEx Standard Overnight");
            if (fedExStandardOvernight != null) fedExStandardOvernight.Description = "Delivery by 4:30 PM";
            var fedExPriorityOvernight = combinedShippingOptions.ShippingOptions.FirstOrDefault(so => so.Name == "FedEx Priority Overnight");
            if (fedExPriorityOvernight != null) fedExPriorityOvernight.Description = "Delivery by 12:00 PM";

            //HACK: MSS - Order shipping options by Rate (Asc)
            combinedShippingOptions.ShippingOptions = combinedShippingOptions.ShippingOptions.OrderBy(so => so.Rate).ToList();

            //HACK: MSS - Collect Shipping
            combinedShippingOptions.ShippingOptions.Add(new ShippingOption()
            {
                ShippingRateComputationMethodSystemName = "Collect",
                Rate = 0,
                Name = "Collect",
                Description = "Have us ship with your account number."
            });

            return combinedShippingOptions;
        }

        


        private bool CanShipUSPSOrFedExMainWarehouse(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx)
        {
            //SCENERIO ONE
            //1) All ship from main warehouse 
            //2) All order items have USPS designation
            //3) All fit in the two predefinied boxes
            //4) All items are under freightStartingWeight.
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var mainwarehouseid = settingService.GetSettingByKey<string>("warehousesetting.mainwarehouseid");
            var freightStartingWeight = settingService.GetSettingByKey<string>("warehousesetting.freightstartingweight");

            int primaryWarehouseId = 0;
            if (mainwarehouseid != null)
            {
                if (int.TryParse(mainwarehouseid, out primaryWarehouseId))
                {
                }
            }

            //HACK: MSS - Freight Starting Weight setting
            var intFreightStartingWeight = 0;
            if (freightStartingWeight != null)
            {
                if (int.TryParse(freightStartingWeight, out intFreightStartingWeight))
                {
                }
            }

            //bool IsAllFitInPreDefiniedBox = false;

            decimal totalProductLength = 0;
            decimal totalProductHeight = 0;
            decimal totalProductWidth = 0;

            //evaluate the cart

            var shippableCart = cart.Where(x => !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            foreach (var item in shippableCart)
            {
                if (item.Product.Weight >= intFreightStartingWeight)
                {
                    return false;
                }

                if (item.Product.WarehouseId != primaryWarehouseId)
                {
                    return false;
                }

                var keyValue = item.Product.GetAttribute<string>("CanUSPS");
                if (keyValue == null || keyValue == "False")
                {
                    return false;
                }
              
                //totalProductHeight = totalProductHeight + item.Product.Height;
                //totalProductLength = totalProductLength + item.Product.Length;
                //totalProductWidth = totalProductWidth + item.Product.Width;
                //totalProductWeight = totalProductWeight + item.Product.Weight;
                //totalProductHeight = totalProductHeight * item.Quantity;
                //totalProductLength = totalProductLength * item.Quantity;
                //totalProductWidth = totalProductWidth * item.Quantity;
                //totalProductWeight = totalProductWeight * item.Quantity;

                totalProductLength = totalProductLength + item.Product.Length * item.Quantity;
                totalProductHeight = totalProductHeight + item.Product.Height * item.Quantity;
                totalProductWidth = totalProductWidth + item.Product.Width * item.Quantity;

            }

            //check to see if all the products will fit in the first box
            //if (totalProductLength <= 6.88M && totalProductWidth <= 6.88M && totalProductHeight <= 1.18M)
            //{
            //    IsAllFitInPreDefiniedBox = true;
            //} //check the second box
            //else if (totalProductLength <= 8.46M && totalProductWidth <= 5.11M && totalProductHeight <= 1.57M)
            //{
            //    IsAllFitInPreDefiniedBox = true;
            //}

            if (!IsAllFitInPreDefiniedBox(totalProductLength, totalProductWidth, totalProductHeight))
            {
                return false;
            }

            //MSS - This means item(s) are able to be shipped BOTH USPS and FedEX.  User can choose between the two.  USPS is a cheaper option for the customer.

            //get shipping options for USPS
            shippingResponseUsps = base.GetShippingOptions(shippableCart, shippingAddress, "Shipping.USPS", storeId);
            
            //get shipping options for FedEx
            shippingResponseFedEx = base.GetShippingOptions(shippableCart, shippingAddress, "Shipping.Fedex", storeId);

            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            
            //HACK - MSS - OSHA / FedEx heavy weight charge
            customSettings.IsHeavyWeight = shippableCart.Select(x => x.Product.Weight).Sum() > customSettings.HeavyWeight;

            shippingResponseFedEx = AdjustFedExShipmentAmounts(shippingResponseFedEx, customSettings);
            
            return true;
        }

        private bool CanShipFedExMultipleWarehouses(IList<ShoppingCartItem> cart, Address shippingAddress,
            int storeId, ref GetShippingOptionResponse shippingResponseFedEx)
        {
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var freightStartingWeight = settingService.GetSettingByKey<string>("warehousesetting.freightstartingweight");

            //HACK: MSS - Freight Starting Weight setting
            var intFreightStartingWeight = 0;
            if (freightStartingWeight != null)
            {
                if (int.TryParse(freightStartingWeight, out intFreightStartingWeight))
                {
                }
            }

            foreach (var item in cart)
            {
                if (item.Product.Weight >= intFreightStartingWeight)
                {
                    return false;
                }

                //Does item have a frieght class specification?
                if (HasFreightClass(item))
                {
                    return false;
                }
            }

            var shippableCart = cart.Where(x => !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            shippingResponseFedEx = base.GetShippingOptions(shippableCart, shippingAddress, "Shipping.Fedex", storeId);

            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);

            //HACK - MSS - OSHA / FedEx heavy weight charge
            customSettings.IsHeavyWeight = shippableCart.Select(x => x.Product.Weight).Sum() > customSettings.HeavyWeight;

            shippingResponseFedEx = AdjustFedExShipmentAmounts(shippingResponseFedEx, customSettings);
            return true;
        }

        private bool CanShipFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
        ref GetShippingOptionResponse shippingResponseFedExFreight)
        {

            //Freight Only Scenerio
            //1) All items are over freightStartingWeight
            //2) At least One item is over freightStartingWeight and all items come from a single warehouse
            //3) At least One item is over freightStartingWeight and from the Main Warehouse and smaller items are from Main warehouse
            bool isAllItemsFrieghtItems = true;
            bool isAtLeastOneAFreightItem = false;
            bool isAllItemsFromSingleWarehouse = true;
            bool isAllWarehousesHaveAFreightItem = false;
            int i = 0;
            var previousWarehouseId = 0;
            cart = cart.OrderBy(x => x.Product.WarehouseId).ToList();
            List<int> warehousesThatHaveFreight = new List<int>();
            List<int> warehousesWithinCart = new List<int>();

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var freightStartingWeight = settingService.GetSettingByKey<string>("warehousesetting.freightstartingweight");

            //HACK: MSS - Freight Starting Weight setting
            var intFreightStartingWeight = 0;
            if (freightStartingWeight != null)
            {
                if (int.TryParse(freightStartingWeight, out intFreightStartingWeight))
                {
                }
            }

            var shippableCart = cart.Where(x => !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            foreach (var item in shippableCart)
            {
                if (!warehousesWithinCart.Contains(item.Product.WarehouseId))
                {
                    warehousesWithinCart.Add(item.Product.WarehouseId);
                }
                if (i == 0)
                {
                    previousWarehouseId = item.Product.WarehouseId;
                }

                var hasFreightClass = HasFreightClass(item);

                if (item.Product.Weight < intFreightStartingWeight && hasFreightClass == false)
                {
                    isAllItemsFrieghtItems = false;
                }

                if (item.Product.Weight >= intFreightStartingWeight || hasFreightClass)
                {
                    isAtLeastOneAFreightItem = true;
                    if (!warehousesThatHaveFreight.Contains(item.Product.WarehouseId))
                    {
                        warehousesThatHaveFreight.Add(item.Product.WarehouseId);
                    }
                }
                if (item.Product.WarehouseId != previousWarehouseId)
                {
                    isAllItemsFromSingleWarehouse = false;
                }
                previousWarehouseId = item.Product.WarehouseId;
                i++;
            }

            //if all warehouses have a freight item, everything is shipped freight
            if (warehousesWithinCart.Count == warehousesThatHaveFreight.Count)
            {
                isAllWarehousesHaveAFreightItem = true;
            }

            if (isAllItemsFrieghtItems || (isAtLeastOneAFreightItem && isAllItemsFromSingleWarehouse) || (isAllWarehousesHaveAFreightItem))
            {
                //get shipping options for Fed Ex Freight
                
                shippingResponseFedExFreight = base.GetShippingOptions(shippableCart, shippingAddress, "Shipping.FedExFreight", storeId);

                if (shippingResponseFedExFreight.Errors.Count > 0)
                {
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);
                    return false;
                }
                foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
                {
                    //if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                    //{
                    //    shippingOptions.Name = "Motor Freight";
                    //}

                    //HACK: MSS - FedEx Freight® Economy
                    if (shippingOptions.Name == "FEDEX_FREIGHT_ECONOMY")
                    {
                        shippingOptions.Name = "FedEx Freight® Economy";
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

        private bool CanShipUSPSandFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId, ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx, ref GetShippingOptionResponse shippingResponseFedExFreight)
        {

            //1) All items are < intFreightStartingWeight and are marked USPS and come from Main Warehouse
            //2) All OTHER items are greater than intFreightStartingWeight and require frieght regardless of the warehouse shipping from 
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

            var cartOfMainWareHouse =
                cart.Where(x => x.Product.WarehouseId == primaryWarehouseId && !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            var cartofOtherWarehouses =
                    cart.Where(x => x.Product.WarehouseId != primaryWarehouseId && !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            decimal totalProductLength = 0;
            decimal totalProductHeight = 0;
            decimal totalProductWidth = 0;

            var freightStartingWeight = settingService.GetSettingByKey<string>("warehousesetting.freightstartingweight");

            //HACK: MSS - Freight Starting Weight setting
            var intFreightStartingWeight = 0;
            if (freightStartingWeight != null)
            {
                if (int.TryParse(freightStartingWeight, out intFreightStartingWeight))
                {
                }
            }

            //make sure all of the main warehouse products are marked usps and under intFreightStartingWeight
            foreach (var item in cartOfMainWareHouse)
            {
                if (item.Product.Weight >= intFreightStartingWeight)
                {
                    return false;
                }

                string keyValue = item.Product.GetAttribute<string>("CanUSPS");
                if (keyValue == null || keyValue == "False")
                {
                    return false;
                }

                //totalProductHeight = totalProductHeight + item.Product.Height;
                //totalProductLength = totalProductLength + item.Product.Length;
                //totalProductWidth = totalProductWidth + item.Product.Width;
                //totalProductWeight = totalProductWeight + item.Product.Weight;

                //totalProductHeight = totalProductHeight * item.Quantity;
                //totalProductLength = totalProductLength * item.Quantity;
                //totalProductWidth = totalProductWidth * item.Quantity;
                //totalProductWeight = totalProductWeight * item.Quantity;

                totalProductHeight = totalProductHeight + item.Product.Height * item.Quantity; 
                totalProductLength = totalProductLength + item.Product.Length * item.Quantity;
                totalProductWidth = totalProductWidth + item.Product.Width * item.Quantity;
                
            }

            List<int> warehousesInCart = new List<int>();
            List<int> warehousesThatHaveFreight = new List<int>();

            foreach (var item in cartofOtherWarehouses)
            {
                if (!warehousesInCart.Contains(item.Product.WarehouseId))
                {
                    warehousesInCart.Add(item.Product.WarehouseId);
                }

                if (item.Product.Weight < intFreightStartingWeight && !HasFreightClass(item)) continue;

                if (!warehousesThatHaveFreight.Contains(item.Product.WarehouseId))
                {
                    warehousesThatHaveFreight.Add(item.Product.WarehouseId);
                }
            }

            //make sure all other warehouses have at least one freight item
            if (warehousesInCart.Count != warehousesThatHaveFreight.Count)
            {
                return false;
            }

            ////check to see if all the products will fit in the first box
            //if (totalProductLength <= 6.88M && totalProductWidth <= 6.88M && totalProductHeight <= 1.18M)
            //{
            //    IsAllFitInPreDefiniedBox = true;
            //} //check the second box
            //else if (totalProductLength <= 8.46M && totalProductWidth <= 5.11M && totalProductHeight <= 1.57M)
            //{
            //    IsAllFitInPreDefiniedBox = true;
            //}

            
            GetShippingOptionResponse shippingResponseUspsCart = new GetShippingOptionResponse();

            //get shipping options for USPS
            if (IsAllFitInPreDefiniedBox(totalProductLength, totalProductWidth, totalProductHeight))
            {
                shippingResponseUspsCart = base.GetShippingOptions(cartOfMainWareHouse, shippingAddress, "Shipping.USPS", storeId);

            }

            var shippingResponseFedExCart = base.GetShippingOptions(cartOfMainWareHouse, shippingAddress, "Shipping.Fedex", storeId);

            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);

            //HACK - MSS - OSHA / FedEx heavy weight charge
            customSettings.IsHeavyWeight = cartOfMainWareHouse.Select(x => x.Product.Weight).Sum() > customSettings.HeavyWeight;

            shippingResponseFedExCart = AdjustFedExShipmentAmounts(shippingResponseFedExCart, customSettings);

            shippingResponseFedExFreight = base.GetShippingOptions(cartofOtherWarehouses, shippingAddress, "Shipping.FedExFreight", storeId);

            if (shippingResponseFedExFreight.Errors.Count > 0)
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);

                return false;
            }
            foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
            {
                //if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                //{
                //    shippingOptions.Name = "Motor Freight";
                //}

                //HACK: MSS - FedEx Freight® Economy
                if (shippingOptions.Name == "FEDEX_FREIGHT_ECONOMY")
                {
                    shippingOptions.Name = "FedEx Freight® Economy";
                }
            }
            //shippingResponseFedExFreight.ShippingOptions.RemoveAt(0);
            //adjust freight based on fedex freight shipping adjustment
            customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExFreight = AdjustFreightAmounts(shippingResponseFedExFreight, customSettings);

            foreach (var shippingOption in shippingResponseUspsCart.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            foreach (var shippingOption in shippingResponseFedExCart.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }

            //HACK: MSS - If we made it this far there are no errors in shippingResponseFedExFreight, set it to null.  We no longer need it.
            shippingResponseFedExFreight = null;

            shippingResponseUsps = shippingResponseUspsCart;
            shippingResponseFedEx = shippingResponseFedExCart;
            return true;
        }

        private bool CanShipFedExandFreight(IList<ShoppingCartItem> cart, Address shippingAddress, int storeId,
            ref GetShippingOptionResponse shippingResponseUsps, ref GetShippingOptionResponse shippingResponseFedEx, ref GetShippingOptionResponse shippingResponseFedExFreight)
        {
            //start by simply dividing the cart between freight items and small package items

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var freightStartingWeight = settingService.GetSettingByKey<string>("warehousesetting.freightstartingweight");

            //HACK: MSS - Freight Starting Weight setting
            var intFreightStartingWeight = 0;
            if (freightStartingWeight != null)
            {
                if (int.TryParse(freightStartingWeight, out intFreightStartingWeight))
                {
                }
            }

            var cartSmallPackage = cart.Where(x => x.Product.Weight < intFreightStartingWeight && !HasFreightClass(x) && !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            var cartOfFedExQualified = new List<ShoppingCartItem>();

            var cartOfFreightQualified = cart.Where(x => x.Product.Weight >= intFreightStartingWeight || HasFreightClass(x) && !x.Product.IsFreeShipping && !x.Product.IsDownload).ToList();

            var warehousesThatHaveFreight = new List<int>();


            foreach (var item in cartOfFreightQualified)
            {
                if (!warehousesThatHaveFreight.Contains(item.Product.WarehouseId))
                {
                    warehousesThatHaveFreight.Add(item.Product.WarehouseId);
                }
            }

            //for warehouses that have freight, move the items OUT of the non-freight list and add to the freight list since these items will go out with the freight
            foreach (var item in cartSmallPackage)
            {
                if (warehousesThatHaveFreight.Contains(item.Product.WarehouseId))
                {
                    cartOfFreightQualified.Add(item);
                }
                else
                {
                    cartOfFedExQualified.Add(item);
                }
            }

            GetShippingOptionResponse shippingResponseUspsCart = new GetShippingOptionResponse();
            GetShippingOptionResponse shippingResponseFedExCart = new GetShippingOptionResponse();
            //GetShippingOptionResponse shippingResponseFedExFreight = new GetShippingOptionResponse();

            shippingResponseFedExCart = base.GetShippingOptions(cartOfFedExQualified, shippingAddress, "Shipping.Fedex", storeId);
            //adjust Fed Ex Shipping
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(0);

            //HACK - MSS - OSHA / FedEx heavy weight charge
            customSettings.IsHeavyWeight = cartOfFedExQualified.Select(x => x.Product.Weight).Sum() > customSettings.HeavyWeight;

            shippingResponseFedExCart = AdjustFedExShipmentAmounts(shippingResponseFedExCart, customSettings);

            shippingResponseFedExFreight = base.GetShippingOptions(cartOfFreightQualified, shippingAddress, "Shipping.FedExFreight", storeId);
            if (shippingResponseFedExFreight.Errors.Count > 0)
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Freight Error: " + shippingResponseFedExFreight.Errors[0]);
                return false;
            }
            foreach (var shippingOptions in shippingResponseFedExFreight.ShippingOptions)
            {
                //if (shippingOptions.Name == "FEDEX_FREIGHT_PRIORITY")
                //{
                //    shippingOptions.Name = "Motor Freight";
                //}

                //HACK: MSS - FedEx Freight® Economy
                if (shippingOptions.Name == "FEDEX_FREIGHT_ECONOMY")
                {
                    shippingOptions.Name = "FedEx Freight® Economy";
                }

            }
            //shippingResponseFedExFreight.ShippingOptions.RemoveAt(0);
            //adjust freight based on fedex freight shipping adjustment
            customSettings = _settingService.LoadSetting<BssCustomSettings>(0);
            shippingResponseFedExFreight = AdjustFreightAmounts(shippingResponseFedExFreight, customSettings);

            foreach (var shippingOption in shippingResponseUspsCart.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }
            foreach (var shippingOption in shippingResponseFedExCart.ShippingOptions)
            {
                shippingOption.Name = shippingOption.Name + " (" + shippingOption.Rate.ToString("C") + ") + " + shippingResponseFedExFreight.ShippingOptions[0].Name + " (" + shippingResponseFedExFreight.ShippingOptions[0].Rate.ToString("C") + ") = ";
                shippingOption.Rate = shippingOption.Rate + shippingResponseFedExFreight.ShippingOptions[0].Rate;
            }

            //HACK: MSS - If we made it this far there are no errors in shippingResponseFedExFreight, set it to null.  We no longer need it.
            shippingResponseFedExFreight = null;

            shippingResponseUsps = shippingResponseUspsCart;
            shippingResponseFedEx = shippingResponseFedExCart;
            return true;

        }



        private static GetShippingOptionResponse AdjustFedExShipmentAmounts(GetShippingOptionResponse shippingResponseFedEx,
            BssCustomSettings customSettings)
        {
            foreach (var FedExOption in shippingResponseFedEx.ShippingOptions)
            {
                //Additional Express Shipping Charges
                if (FedExOption.Name.Contains("2Day") || FedExOption.Name.Contains("Overnight"))
                {
                    if (customSettings.IsFedexExpressShippingAdjustmentPercent)
                    {
                        var percentage = customSettings.FedexExpressShippingAdjustment / 100;
                        var reductionAmount = FedExOption.Rate * percentage;
                        FedExOption.Rate = FedExOption.Rate + reductionAmount;
                        if (FedExOption.Rate < 0)
                        {
                            FedExOption.Rate = 0;
                        }
                    }
                    else
                    {
                        FedExOption.Rate = FedExOption.Rate + customSettings.FedexExpressShippingAdjustment;
                        if (FedExOption.Rate < 0)
                        {
                            FedExOption.Rate = 0;
                        }
                    }
                }
                else
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

                //HACK - MSS - OSHA / FedEx heavy weight charge
                if (customSettings.IsHeavyWeight)
                {
                    FedExOption.Rate = FedExOption.Rate + customSettings.FedexHeavyWeightCharge;
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


        private static bool HasFreightClass(ShoppingCartItem item)
        {
            //Does item have a [Freight Class] specification?
            if (item.Product.ProductSpecificationAttributes.All(psa => psa.SpecificationAttributeOption.SpecificationAttribute.Name != "Freight Class")) return false;
            
            //Does item have a [Freight Min Qty] specification?
            var psaMinQtyFreight = item.Product.ProductSpecificationAttributes.FirstOrDefault(psa => psa.SpecificationAttributeOption.SpecificationAttribute.Name == "Freight Min Qty");

            //If we made it this far, there is a [Freight Class] specification

            //Check for [Freight Min Qty]
            if (psaMinQtyFreight == null) return true;

            //Is item qty > [Freight Min Qty] specification value
            int freightMinQty;

            if (int.TryParse(psaMinQtyFreight.CustomValue, out freightMinQty))
            {
                return item.Quantity >= freightMinQty;
            }

            //If we made it this far, something went wrong

            var logger = EngineContext.Current.Resolve<ILogger>();
            logger.Warning("HasFreightClass: int.TryParse failed on [Freight Min Qty] specification value");

            return true;
        }

        private static bool IsAllFitInPreDefiniedBox(decimal totalProductLength, decimal totalProductWidth, decimal totalProductHeight)
        {
            //Small USPS Envelope Volume = Length * Width * Height
            const decimal box1Volume = 7.25M * 7.0M * 1.2M;

            //Large USPS Envelope Volume = Length * Width * Height
            const decimal box2Volume = 10.5M * 15M * 1.2M;

            //Small USPS Flate Rate Box Volume = Length * Width * Height 
            const decimal box3Volume = 8.625M * 5.375M * 1.625M;

            var totalProductvolume = totalProductLength * totalProductWidth * totalProductHeight;

            //check to see if all the products will fit 

            //check box 1
            if (totalProductvolume < box1Volume)
            {
                return true;
            } 
            
            //check box 2
            if (totalProductvolume < box2Volume)
            {
                return true;
            } 
            
            //check box 3
            return totalProductvolume < box3Volume;
        }

    }
}

