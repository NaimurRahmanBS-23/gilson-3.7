//------------------------------------------------------------------------------
// Contributor(s): mb, New York. 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Shipping.FedexFreight.Domain;
using Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using Address = Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Address;
using ContactAndAddress = Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.ContactAndAddress;
using Weight = Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Weight;

namespace Nop.Plugin.Shipping.FedexFreight
{
    /// <summary>
    /// Fedex computation method
    /// </summary>
    public class FedexFreightComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        #region Constants

        private const int MAXPACKAGEWEIGHT = 150;
        private const string MEASUREWEIGHTSYSTEMKEYWORD = "lb";
        private const string MEASUREDIMENSIONSYSTEMKEYWORD = "inches";

        #endregion

        #region Fields

        private readonly IMeasureService _measureService;
        private readonly IShippingService _shippingService;
        private readonly ISettingService _settingService;
        private readonly FedexFreightSettings _fedexFreightSettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ILogger _logger;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        #endregion

        #region Ctor
        public FedexFreightComputationMethod(IMeasureService measureService,
            IShippingService shippingService, ISettingService settingService,
            FedexFreightSettings fedexFreightSettings, IOrderTotalCalculationService orderTotalCalculationService,
            ICurrencyService currencyService, CurrencySettings currencySettings,
            ILogger logger, ICheckoutAttributeParser checkoutAttributeParser)
        {
            this._measureService = measureService;
            this._shippingService = shippingService;
            this._settingService = settingService;
            this._fedexFreightSettings = fedexFreightSettings;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._logger = logger;
            this._checkoutAttributeParser = checkoutAttributeParser;
        }
        #endregion

        #region Utilities

        private RateRequest CreateRateRequest(GetShippingOptionRequest getShippingOptionRequest, out Currency requestedShipmentCurrency)
        {
            // Build the RateRequest
            var request = new RateRequest();

            request.WebAuthenticationDetail = new RateServiceWebReference.WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new RateServiceWebReference.WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = _fedexFreightSettings.Key;
            request.WebAuthenticationDetail.UserCredential.Password = _fedexFreightSettings.Password;

            request.ClientDetail = new RateServiceWebReference.ClientDetail();
            request.ClientDetail.AccountNumber = _fedexFreightSettings.AccountNumber;
            request.ClientDetail.MeterNumber = _fedexFreightSettings.MeterNumber;

            request.TransactionDetail = new RateServiceWebReference.TransactionDetail();
            request.TransactionDetail.CustomerTransactionId = "***Rate Available Services v16 Request - nopCommerce***"; // This is a reference field for the customer.  Any value can be used and will be provided in the response.

            request.Version = new RateServiceWebReference.VersionId(); // WSDL version information, value is automatically set from wsdl            

            request.ReturnTransitAndCommit = true;
            request.ReturnTransitAndCommitSpecified = true;
            request.CarrierCodes = new RateServiceWebReference.CarrierCodeType[1];
            // Insert the Carriers you would like to see the rates for
            request.CarrierCodes[0] = RateServiceWebReference.CarrierCodeType.FXFR;

            decimal orderSubTotalDiscountAmount;
            Discount orderSubTotalAppliedDiscount;
            decimal subTotalWithoutDiscountBase;
            decimal subTotalWithDiscountBase;
            //TODO we should use getShippingOptionRequest.Items.GetQuantity() method to get subtotal
            _orderTotalCalculationService.GetShoppingCartSubTotal(getShippingOptionRequest.Items.Select(x=>x.ShoppingCartItem).ToList(),
                false, out orderSubTotalDiscountAmount, out orderSubTotalAppliedDiscount,
                out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);
            decimal subTotalBase = subTotalWithDiscountBase;

            //HACK: MSS - Set ServiceType = FEDEX_FREIGHT_ECONOMY
            request.RequestedShipment = new RequestedShipment() {ServiceType = RateServiceWebReference.ServiceType.FEDEX_FREIGHT_ECONOMY};
            var freightShipmentDetail = new FreightShipmentDetail();
            var customer = getShippingOptionRequest.Customer;
            
            //Liftgate code
            var checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, 1);
            var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributesXml);
            if (attributeValues.Count > 0)
            {
                if (attributeValues[0].CheckoutAttribute.Name.ToUpper().Contains("LIFTGATE"))
                {
                    if (attributeValues[0].Name.ToUpper() == "YES")
                    {
                        ShipmentSpecialServiceType lift = ShipmentSpecialServiceType.LIFTGATE_DELIVERY;
                        List<ShipmentSpecialServiceType> listOfSpecial = new List<ShipmentSpecialServiceType>();
                        listOfSpecial.Add(lift);
                        request.RequestedShipment.SpecialServicesRequested = new ShipmentSpecialServicesRequested();
                        request.RequestedShipment.SpecialServicesRequested.SpecialServiceTypes = listOfSpecial.ToArray();

                    }
                }
            }

            freightShipmentDetail.Role = FreightShipmentRoleType.SHIPPER;
            freightShipmentDetail.RoleSpecified = true;


            freightShipmentDetail.AlternateBilling = new Party();
            freightShipmentDetail.AlternateBilling.Address = new Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Address();
            freightShipmentDetail.AlternateBilling.AccountNumber = _fedexFreightSettings.FreightAltAccountNumber;
            freightShipmentDetail.AlternateBilling.Address.StateOrProvinceCode = _fedexFreightSettings.FreightAltBillingState;
            freightShipmentDetail.AlternateBilling.Address.City = _fedexFreightSettings.FreightAltBillingCity;
            freightShipmentDetail.AlternateBilling.Address.CountryCode = _fedexFreightSettings.FreightAltBillingCountry;
            freightShipmentDetail.AlternateBilling.Address.PostalCode = _fedexFreightSettings.FreightAltBillingPostalCode;
            List<string> streetlinesalternatelist = new List<string>();
            streetlinesalternatelist.Add(_fedexFreightSettings.FreightAltBillingAddress1);
            freightShipmentDetail.AlternateBilling.Address.StreetLines = streetlinesalternatelist.ToArray();
            freightShipmentDetail.AlternateBilling.Contact = new Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Contact();

            request.RequestedShipment.FreightShipmentDetail = freightShipmentDetail;
            //request.RequestedShipment.ServiceType = RateServiceWebReference.ServiceType.FEDEX_FREIGHT_PRIORITY;

            List<FreightShipmentLineItem> fsliList = new List<FreightShipmentLineItem>();
            foreach (var item in getShippingOptionRequest.Items)
            {
                var fsli = new FreightShipmentLineItem {Description = "shipment"};

                //HACK: MSS - Set freight class.
                //***********************************************************************************************************************************
                var psaFreightClass = item.ShoppingCartItem.Product.ProductSpecificationAttributes.Where(psa => psa.SpecificationAttributeOption.SpecificationAttribute.Name == "Freight Class");



                switch (psaFreightClass.FirstOrDefault()?.CustomValue)
                {
                    case "50":
                        fsli.FreightClass = FreightClassType.CLASS_050;
                        break;
                    case "55":
                        fsli.FreightClass = FreightClassType.CLASS_055;
                        break;
                    case "60":
                        fsli.FreightClass = FreightClassType.CLASS_060;
                        break;
                    case "65":
                        fsli.FreightClass = FreightClassType.CLASS_065;
                        break;
                    case "70":
                        fsli.FreightClass = FreightClassType.CLASS_070;
                        break;
                    case "77.5":
                        fsli.FreightClass = FreightClassType.CLASS_077_5;
                        break;
                    case "92.5":
                        fsli.FreightClass = FreightClassType.CLASS_092_5;
                        break;
                    case "100":
                        fsli.FreightClass = FreightClassType.CLASS_100;
                        break;
                    case "110":
                        fsli.FreightClass = FreightClassType.CLASS_110;
                        break;
                    case "125":
                        fsli.FreightClass = FreightClassType.CLASS_125;
                        break;
                    case "150":
                        fsli.FreightClass = FreightClassType.CLASS_150;
                        break;
                    case "175":
                        fsli.FreightClass = FreightClassType.CLASS_175;
                        break;
                    case "200":
                        fsli.FreightClass = FreightClassType.CLASS_200;
                        break;
                    case "250":
                        fsli.FreightClass = FreightClassType.CLASS_250;
                        break;
                    case "300":
                        fsli.FreightClass = FreightClassType.CLASS_300;
                        break;
                    case "400":
                        fsli.FreightClass = FreightClassType.CLASS_400;
                        break;
                    case "500":
                        fsli.FreightClass = FreightClassType.CLASS_500;
                        break;
                    default:
                        fsli.FreightClass = FreightClassType.CLASS_085;
                        break;
                }
                //***********************************************************************************************************************************

                fsli.FreightClassSpecified = true;
                Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Weight weight = new Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Weight();
                weight.Units = RateServiceWebReference.WeightUnits.LB;
                weight.UnitsSpecified = true;
                weight.ValueSpecified = true;
                //hack, it will not accept a weight under a pound
                if ((item.ShoppingCartItem.Product.Weight * item.ShoppingCartItem.Quantity )< 1)
                {
                    weight.Value = 1;
                }
                else
                {
                    weight.Value = item.ShoppingCartItem.Product.Weight * item.ShoppingCartItem.Quantity;
                }
                fsli.Weight = weight;
                fsliList.Add(fsli);
            }

            request.RequestedShipment.FreightShipmentDetail.LineItems = fsliList.ToArray();

            SetOrigin(request, getShippingOptionRequest);
            SetDestination(request, getShippingOptionRequest);

            requestedShipmentCurrency = GetRequestedShipmentCurrency(
                request.RequestedShipment.Shipper.Address.CountryCode,    // origin
                request.RequestedShipment.Recipient.Address.CountryCode); // destination

            decimal subTotalShipmentCurrency;
            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (requestedShipmentCurrency.CurrencyCode == primaryStoreCurrency.CurrencyCode)
                subTotalShipmentCurrency = subTotalBase;
            else
                subTotalShipmentCurrency = _currencyService.ConvertFromPrimaryStoreCurrency(subTotalBase, requestedShipmentCurrency);

            Debug.WriteLine("SubTotal (Primary Currency) : {0} ({1})", subTotalBase, primaryStoreCurrency.CurrencyCode);
            Debug.WriteLine("SubTotal (Shipment Currency): {0} ({1})", subTotalShipmentCurrency, requestedShipmentCurrency.CurrencyCode);

            SetShipmentDetails(request, subTotalShipmentCurrency, requestedShipmentCurrency.CurrencyCode);
            SetPayment(request);
            //view xml for debug purposes
            var requestXML = CreateXML(request);
            return request;
        }

        private void SetShipmentDetails(RateRequest request, decimal orderSubTotal, string currencyCode)
        {
            //set drop off type
            switch (_fedexFreightSettings.DropoffType)
            {
                case DropoffType.BusinessServiceCenter:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.BUSINESS_SERVICE_CENTER;
                    break;
                case DropoffType.DropBox:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.DROP_BOX;
                    break;
                case DropoffType.RegularPickup:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.REGULAR_PICKUP;
                    break;
                case DropoffType.RequestCourier:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.REQUEST_COURIER;
                    break;
                case DropoffType.Station:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.STATION;
                    break;
                default:
                    request.RequestedShipment.DropoffType = RateServiceWebReference.DropoffType.BUSINESS_SERVICE_CENTER;
                    break;
            }


            //Saturday pickup is available for certain FedEx Express U.S. service types:
            //http://www.fedex.com/us/developer/product/WebServices/MyWebHelp/Services/Options/c_SaturdayShipAndDeliveryServiceDetails.html
            //If the customer orders on a Saturday, the rate calculation will use Saturday as the shipping date, and the rates will include a Saturday pickup surcharge
            //More info: http://www.nopcommerce.com/boards/t/27348/fedex-rate-can-be-excessive-for-express-methods-if-calculated-on-a-saturday.aspx
            var shipTimestamp = DateTime.Now;
            if (shipTimestamp.DayOfWeek == DayOfWeek.Saturday)
                shipTimestamp = shipTimestamp.AddDays(2);
            request.RequestedShipment.ShipTimestamp = shipTimestamp; // Shipping date and time
            request.RequestedShipment.ShipTimestampSpecified = true;
            request.RequestedShipment.RateRequestTypes = new RateRequestType[1];
            if (_fedexFreightSettings.ApplyDiscounts)
            {
                request.RequestedShipment.RateRequestTypes[0] = RateRequestType.NONE;
            }
            else
            {
                request.RequestedShipment.RateRequestTypes[0] = RateRequestType.LIST;
            }


        }

        private void SetPayment(RateRequest request)
        {
            request.RequestedShipment.ShippingChargesPayment = new Payment(); // Payment Information
            request.RequestedShipment.ShippingChargesPayment.PaymentType = PaymentType.SENDER; // Payment options are RECIPIENT, SENDER, THIRD_PARTY
            request.RequestedShipment.ShippingChargesPayment.PaymentTypeSpecified = true;
            request.RequestedShipment.ShippingChargesPayment.Payor = new Payor();
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty = new Party();
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = _fedexFreightSettings.FreightAltAccountNumber;
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Contact = new
                Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Contact();

            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address = new Nop.Plugin.Shipping.FedexFreight.RateServiceWebReference.Address();
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address.StateOrProvinceCode = _fedexFreightSettings.FreightAltBillingState;
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address.City = _fedexFreightSettings.FreightAltBillingCity;
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address.CountryCode = _fedexFreightSettings.FreightAltBillingCountry;
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address.PostalCode = _fedexFreightSettings.FreightAltBillingPostalCode;
            List<string> streetlinesPAYORlist = new List<string>();
            streetlinesPAYORlist.Add(_fedexFreightSettings.FreightAltBillingAddress1);
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.Address.StreetLines = streetlinesPAYORlist.ToArray();

        }

        private void SetDestination(RateRequest request, GetShippingOptionRequest getShippingOptionRequest)
        {
            request.RequestedShipment.Recipient = new Party();
            request.RequestedShipment.Recipient.Address = new RateServiceWebReference.Address();
            if (_fedexFreightSettings.UseResidentialRates)
            {
                request.RequestedShipment.Recipient.Address.Residential = true;
                request.RequestedShipment.Recipient.Address.ResidentialSpecified = true;
            }
            request.RequestedShipment.Recipient.Address.StreetLines = new[] { getShippingOptionRequest.ShippingAddress.Address1 };
            request.RequestedShipment.Recipient.Address.City = getShippingOptionRequest.ShippingAddress.City;
            if (getShippingOptionRequest.ShippingAddress.StateProvince != null &&
                IncludeStateProvinceCode(getShippingOptionRequest.ShippingAddress.Country.TwoLetterIsoCode))
            {
                request.RequestedShipment.Recipient.Address.StateOrProvinceCode = getShippingOptionRequest.ShippingAddress.StateProvince.Abbreviation;
            }
            else
            {
                request.RequestedShipment.Recipient.Address.StateOrProvinceCode = string.Empty;
            }
            request.RequestedShipment.Recipient.Address.PostalCode = getShippingOptionRequest.ShippingAddress.ZipPostalCode;
            request.RequestedShipment.Recipient.Address.CountryCode = getShippingOptionRequest.ShippingAddress.Country.TwoLetterIsoCode;
        }

        private void SetOrigin(RateRequest request, GetShippingOptionRequest getShippingOptionRequest)
        {
            request.RequestedShipment.Shipper = new Party();
            request.RequestedShipment.Shipper.Address = new RateServiceWebReference.Address();

            if (getShippingOptionRequest.CountryFrom == null)
                throw new Exception("FROM country is not specified");

            request.RequestedShipment.Shipper.Address.StreetLines = new [] { getShippingOptionRequest.AddressFrom };
            request.RequestedShipment.Shipper.Address.City = getShippingOptionRequest.CityFrom;
            if (IncludeStateProvinceCode(getShippingOptionRequest.CountryFrom.TwoLetterIsoCode))
            {
                string stateProvinceAbbreviation = getShippingOptionRequest.StateProvinceFrom == null ? "" : getShippingOptionRequest.StateProvinceFrom.Abbreviation;
                request.RequestedShipment.Shipper.Address.StateOrProvinceCode = stateProvinceAbbreviation;
            }
            request.RequestedShipment.Shipper.Address.PostalCode = getShippingOptionRequest.ZipPostalCodeFrom;
            request.RequestedShipment.Shipper.Address.CountryCode = getShippingOptionRequest.CountryFrom.TwoLetterIsoCode;
        }

        private bool IncludeStateProvinceCode(string countryCode)
        {
            return (countryCode.Equals("US", StringComparison.InvariantCultureIgnoreCase) || 
                    countryCode.Equals("CA", StringComparison.InvariantCultureIgnoreCase));
        }

        public string LastReponse { get; set; }
        public List<ShippingOption> firstResponse { get; set; }

        private IEnumerable<ShippingOption> ParseResponse(RateReply reply, Currency requestedShipmentCurrency)
        {
            var result = new List<ShippingOption>();
            
            Debug.WriteLine("RateReply details:");
            Debug.WriteLine("**********************************************************");
            foreach (var rateDetail in reply.RateReplyDetails)
            {
                var shippingOption = new ShippingOption();
                string serviceName = rateDetail.ServiceType.ToString();

                // Skip the current service if services are selected and this service hasn't been selected
                //if (!String.IsNullOrEmpty(_fedexSettings.CarrierServicesOffered) && !_fedexSettings.CarrierServicesOffered.Contains(rateDetail.ServiceType.ToString()))
                //{
                //    continue;
                //}

                Debug.WriteLine("ServiceType: " + rateDetail.ServiceType);
                if (!serviceName.Equals("UNKNOWN"))
                {
                    shippingOption.Name = serviceName;
                    //if(shippingOption.Name != "FEDEX_FREIGHT_PRIORITY")
                    if (shippingOption.Name != "FEDEX_FREIGHT_ECONOMY") 
                    {
                        continue;
                    }
                    StringBuilder descriptionDetail = new StringBuilder();
                    foreach (RatedShipmentDetail shipmentDetail in rateDetail.RatedShipmentDetails)
                    {
                        //store surcharges and rate details in the description field.
                        Debug.WriteLine("RateType : " + shipmentDetail.ShipmentRateDetail.RateType);
                        if (shipmentDetail.ShipmentRateDetail.Surcharges != null)
                        {
                            foreach (var surcharge in shipmentDetail.ShipmentRateDetail.Surcharges)
                            {
                                Debug.WriteLine("Surcharge : " + surcharge.Description);
                                Debug.WriteLine("Surcharge Amount : " + surcharge.Amount.Amount);
                                descriptionDetail.Append("|SURCHARGE ");
                                descriptionDetail.Append(surcharge.Description);
                                descriptionDetail.Append(":");
                                descriptionDetail.Append(surcharge.Amount.Amount);
                            }
                        }
                        Debug.WriteLine("Total Billing Weight : " + shipmentDetail.ShipmentRateDetail.TotalBillingWeight.Value);
                        descriptionDetail.Append("|BILLING WEIGHT:");
                        descriptionDetail.Append(shipmentDetail.ShipmentRateDetail.TotalBillingWeight.Value);
                        Debug.WriteLine("Total Base Charge : " + shipmentDetail.ShipmentRateDetail.TotalBaseCharge.Amount);
                        descriptionDetail.Append("|BASE CHARGE:");
                        descriptionDetail.Append(shipmentDetail.ShipmentRateDetail.TotalBaseCharge.Amount);
                        Debug.WriteLine("Total Discount : " + shipmentDetail.ShipmentRateDetail.TotalFreightDiscounts.Amount);
                        Debug.WriteLine("Total Surcharges : " + shipmentDetail.ShipmentRateDetail.TotalSurcharges.Amount);
                        Debug.WriteLine("Net Charge : " + shipmentDetail.ShipmentRateDetail.TotalNetCharge.Amount + "(" + shipmentDetail.ShipmentRateDetail.TotalNetCharge.Currency + ")");
                        descriptionDetail.Append("|NET CHARGE:");
                        descriptionDetail.Append(shipmentDetail.ShipmentRateDetail.TotalNetCharge.Amount);
                        descriptionDetail.Append("|");
                        Debug.WriteLine("*********");
                        System.Web.HttpContext.Current.Session["freightChargesDetail"] = LastReponse;
                        LastReponse = descriptionDetail.ToString();
                        System.Web.HttpContext.Current.Session["freightChargesDetail"] = System.Web.HttpContext.Current.Session["freightChargesDetail"] + descriptionDetail.ToString();


                        
                        // Get discounted rates if option is selected
                        if (_fedexFreightSettings.ApplyDiscounts &
                            (shipmentDetail.ShipmentRateDetail.RateType == ReturnedRateType.PAYOR_ACCOUNT_PACKAGE ||
                            shipmentDetail.ShipmentRateDetail.RateType == ReturnedRateType.PAYOR_ACCOUNT_SHIPMENT))
                        {
                            decimal amount = ConvertChargeToPrimaryCurrency(shipmentDetail.ShipmentRateDetail.TotalNetCharge, requestedShipmentCurrency);
                            shippingOption.Rate = amount + _fedexFreightSettings.AdditionalHandlingCharge;
                            break;
                        }
                        else if (shipmentDetail.ShipmentRateDetail.RateType == ReturnedRateType.PAYOR_LIST_PACKAGE ||
                            shipmentDetail.ShipmentRateDetail.RateType == ReturnedRateType.PAYOR_LIST_SHIPMENT) // Get List Rates (not discount rates)
                        {
                            decimal amount = ConvertChargeToPrimaryCurrency(shipmentDetail.ShipmentRateDetail.TotalNetCharge, requestedShipmentCurrency);
                            shippingOption.Rate = amount + _fedexFreightSettings.AdditionalHandlingCharge;
                            break;
                        }
                        else // Skip the rate (RATED_ACCOUNT, PAYOR_MULTIWEIGHT, or RATED_LIST)
                        {
                            continue;
                        }

                    }
                    result.Add(shippingOption);
                }
                Debug.WriteLine("**********************************************************");
            }

            return result;
        }

        private Decimal ConvertChargeToPrimaryCurrency(Money charge, Currency requestedShipmentCurrency)
        {
            decimal amount;
            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (primaryStoreCurrency.CurrencyCode.Equals(charge.Currency, StringComparison.InvariantCultureIgnoreCase))
            {
                amount = charge.Amount;
            }
            else
            {
                Currency amountCurrency;
                if (charge.Currency == requestedShipmentCurrency.CurrencyCode)
                    amountCurrency = requestedShipmentCurrency;
                else
                    amountCurrency = _currencyService.GetCurrencyByCode(charge.Currency);

                //ensure the the currency exists; otherwise, presume that it was primary store currency
                if (amountCurrency == null)
                    amountCurrency = primaryStoreCurrency;

                amount = _currencyService.ConvertToPrimaryStoreCurrency(charge.Amount, amountCurrency);

                Debug.WriteLine("ConvertChargeToPrimaryCurrency - from {0} ({1}) to {2} ({3})",
                    charge.Amount, charge.Currency, amount, primaryStoreCurrency.CurrencyCode);
            }

            return amount;
        }
        
        private bool IsPackageTooLarge(int length, int height, int width)
        {
            int total = TotalPackageSize(length, height, width);
            if (total > 165)
                return true;
            
            return false;
        }

        private int TotalPackageSize(int length, int height, int width)
        {
            int girth = height + height + width + width;
            int total = girth + length;
            return total;
        }

        private bool IsPackageTooHeavy(int weight)
        {
            return weight > MAXPACKAGEWEIGHT;
        }

        private MeasureWeight GetUsedMeasureWeight()
        {
            var usedMeasureWeight = _measureService.GetMeasureWeightBySystemKeyword(MEASUREWEIGHTSYSTEMKEYWORD);
            if (usedMeasureWeight == null)
                throw new NopException("FedEx shipping service. Could not load \"{0}\" measure weight", MEASUREWEIGHTSYSTEMKEYWORD);
            return usedMeasureWeight;
        }

        private MeasureDimension GetUsedMeasureDimension()
        {
            var usedMeasureDimension = _measureService.GetMeasureDimensionBySystemKeyword(MEASUREDIMENSIONSYSTEMKEYWORD);
            if (usedMeasureDimension == null)
                throw new NopException("FedEx shipping service. Could not load \"{0}\" measure dimension", MEASUREDIMENSIONSYSTEMKEYWORD);

            return usedMeasureDimension;
        }

        private int ConvertFromPrimaryMeasureDimension(decimal quantity, MeasureDimension usedMeasureDimension)
        {
            return Convert.ToInt32(Math.Ceiling(_measureService.ConvertFromPrimaryMeasureDimension(quantity, usedMeasureDimension)));
        }

        private int ConvertFromPrimaryMeasureWeight(decimal quantity, MeasureWeight usedMeasureWeighht)
        {
            return Convert.ToInt32(Math.Ceiling(_measureService.ConvertFromPrimaryMeasureWeight(quantity, usedMeasureWeighht)));
        }
        
        private Currency GetRequestedShipmentCurrency(string originCountryCode, string destinCountryCode)
        {
            var primaryStoreCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

            //The solution coded here might be considered a bit of a hack
            //it only supports the scenario for US / Canada shipping
            //because nopCommerce does not have a concept of a designated currency for a Country.
            string originCurrencyCode;
            if (originCountryCode == "US")
                originCurrencyCode = "USD";
            else if (originCountryCode == "CA")
                originCurrencyCode = "CAD";
            else
                originCurrencyCode = primaryStoreCurrency.CurrencyCode;

            string destinCurrencyCode;
            if (destinCountryCode == "US")
                destinCurrencyCode = "USD";
            else if (destinCountryCode == "CA")
                destinCurrencyCode = "CAD";
            else
                destinCurrencyCode = primaryStoreCurrency.CurrencyCode;
            
            //when neither the shipping origin's currency or the destinations currency is the same as the store primary currency,
            //FedEx would complain that "There are no valid services available. (code: 556)".
            if (originCurrencyCode == primaryStoreCurrency.CurrencyCode || destinCurrencyCode == primaryStoreCurrency.CurrencyCode)
            {
                return primaryStoreCurrency;
            }
            
            //ensure that this currency exists
            return _currencyService.GetCurrencyByCode(originCurrencyCode) ?? primaryStoreCurrency;
        }
        
        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null)
            {
                response.AddError("No shipment items");
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress == null)
            {
                response.AddError("Shipping address is not set");
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress.Country == null)
            {
                response.AddError("Shipping country is not set");
                return response;
            }

            Currency requestedShipmentCurrency;
            var request = CreateRateRequest(getShippingOptionRequest, out requestedShipmentCurrency);
            var service = new RateService(); // Initialize the service
            service.Url = _fedexFreightSettings.Url;
            try
            {
                // This is the call to the web service passing in a RateRequest and returning a RateReply
                var reply = service.getRates(request); // Service call

                var getXML = CreateXML(reply);

                if (reply.HighestSeverity == RateServiceWebReference.NotificationSeverityType.SUCCESS || 
                    reply.HighestSeverity == RateServiceWebReference.NotificationSeverityType.NOTE || 
                    reply.HighestSeverity == RateServiceWebReference.NotificationSeverityType.WARNING) // check if the call was successful
                {
                    if (reply.RateReplyDetails != null)
                    {
                        var shippingOptions = ParseResponse(reply, requestedShipmentCurrency);
                        foreach (var shippingOption in shippingOptions)
                            response.ShippingOptions.Add(shippingOption);
                    }
                    else
                    {
                        if (reply.Notifications != null &&
                            reply.Notifications.Length > 0 &&
                            !String.IsNullOrEmpty(reply.Notifications[0].Message))
                        {
                            response.AddError(string.Format("{0} (code: {1})", reply.Notifications[0].Message, reply.Notifications[0].Code));
                            return response;
                        }

                        response.AddError("Could not get reply from shipping server");
                        return response;
                    }
                }
                else
                {
                    Debug.WriteLine(reply.Notifications[0].Message);
                    response.AddError(reply.Notifications[0].Message);
                    return response;
                }
            }
            catch (SoapException e)
            {
                Debug.WriteLine(e.Detail.InnerText);
                response.AddError(e.Detail.InnerText);
                return response;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                response.AddError(e.Message);
                return response;
            }

            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ShippingFedexFreight";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Shipping.FedexFreight.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new FedexFreightSettings
            {
                Url = "https://gatewaybeta.fedex.com:443/web-services/rate",
                DropoffType = DropoffType.BusinessServiceCenter,
                PackingPackageVolume = 5184
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Url", "URL");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Url.Hint", "Specify FedEx URL.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Key", "Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Key.Hint", "Specify FedEx key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Password", "Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Password.Hint", "Specify FedEx password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AccountNumber", "Account number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AccountNumber.Hint", "Specify FedEx account number.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.MeterNumber", "Meter number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.MeterNumber.Hint", "Specify FedEx meter number.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.UseResidentialRates", "Use residential rates");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.UseResidentialRates.Hint", "Check to use residential rates.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.ApplyDiscounts", "Use discounted rates");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.ApplyDiscounts.Hint", "Check to use discounted rates (instead of list rates).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AdditionalHandlingCharge", "Additional handling charge");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AdditionalHandlingCharge.Hint", "Enter additional handling fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.CarrierServices", "Carrier Services Offered");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.CarrierServices.Hint", "Select the services you want to offer to customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PassDimensions", "Pass dimensions");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PassDimensions.Hint", "Check if you want to pass package dimensions when requesting rates.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingType", "Packing type");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingType.Hint", "Choose preferred packing type.");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByDimensions", "Pack by dimensions");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByOneItemPerPackage", "Pack by one item per package");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByVolume", "Pack by volume");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingPackageVolume", "Package volume");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingPackageVolume.Hint", "Enter your package volume.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.DropoffType", "Dropoff Type");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.DropoffType.Hint", "Choose preferred dropoff type.");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.BusinessServiceCenter", "Business service center");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.DropBox", "Drop box");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.RegularPickup", "Regular pickup");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.RequestCourier", "Request courier");
            this.AddOrUpdatePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.Station", "Station");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAccountNumber", "Freight Account Number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress1", "Freight Billing Address1");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress2", "Freight Billing Address2");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingCity", "Freight Billing City");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingCountry", "Freight Billing Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingPostalCode", "Freight Billing PostalCode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingState", "Freight Billing State");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltAccountNumber", "Freight Alt Account Number");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress1", "Freight Alt Billing Address1");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress2", "Freight Alt Billing Address2");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCity", "Freight Alt Billing City");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCountry", "Freight Alt Billing Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingPostalCode", "Freight Alt Billing PostalCode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingState", "Freight Alt Billing State");


            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FedexFreightSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Url");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Url.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Key");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Key.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AccountNumber");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AccountNumber.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.MeterNumber");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.MeterNumber.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.UseResidentialRates");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.UseResidentialRates.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.ApplyDiscounts");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.ApplyDiscounts.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AdditionalHandlingCharge");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.AdditionalHandlingCharge.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.CarrierServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.CarrierServices.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PassDimensions");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PassDimensions.Hint");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByDimensions");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByOneItemPerPackage");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.PackingType.PackByVolume");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingType");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingType.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingPackageVolume");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.PackingPackageVolume.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.DropoffType");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.DropoffType.Hint");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.BusinessServiceCenter");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.DropBox");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.RegularPickup");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.RequestCourier");
            this.DeletePluginLocaleResource("Enums.Nop.Plugin.Shipping.FedexFreight.DropoffType.Station");

            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAccountNumber");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress1");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress2");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingCity");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingCountry");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingPostalCode");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightBillingState");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltAccountNumber");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress1");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress2");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCity");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCountry");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingPostalCode");
            this.DeletePluginLocaleResource("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingState");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get
            {
                return ShippingRateComputationMethodType.Realtime;
            }
        }

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker
        {
            get { return new FedexShipmentTracker(_logger, _fedexFreightSettings); }
        }

        #endregion
        private string CreateXML(Object YourClassObject)
        {
            XmlDocument xmlDoc = new XmlDocument();   //Represents an XML document, 
                                                      // Initializes a new instance of the XmlDocument class.          
            XmlSerializer xmlSerializer = new XmlSerializer(YourClassObject.GetType());
            // Creates a stream whose backing store is memory. 
            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.Serialize(xmlStream, YourClassObject);
                xmlStream.Position = 0;
                //Loads the XML document from the specified string.
                xmlDoc.Load(xmlStream);
                return xmlDoc.InnerXml;
            }
        }
    }

}