using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Shipping.ShipHawk.Models;
using Nop.Plugin.Shipping.ShipHawk.Services;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Shipping.ShipHawk.Infrastructure
{
    /// <summary>
    /// Represents event consumer for address and order events
    /// </summary>
    public class EventConsumer :
        IConsumer<EntityInserted<Address>>,
        IConsumer<EntityUpdated<Address>>,
        IConsumer<OrderPlacedEvent>
    {
        private readonly IShipHawkAddressService _shipHawkAddressService;
        private readonly ILogger _logger;
        private readonly ShipHawkSettings _shipHawkSettings;
        private readonly IOrderService _orderService;
        private readonly IGenericAttributeService _genericAttributeService;

        public EventConsumer(
            IShipHawkAddressService shipHawkAddressService,
            ILogger logger,
            ShipHawkSettings shipHawkSettings,
            IOrderService orderService,
            IGenericAttributeService genericAttributeService)
        {
            _shipHawkAddressService = shipHawkAddressService;
            _logger = logger;
            _shipHawkSettings = shipHawkSettings;
            _orderService = orderService;
            _genericAttributeService = genericAttributeService;
        }

        public void HandleEvent(EntityInserted<Address> eventMessage)
        {
            if (eventMessage?.Entity == null)
                return;

            if (string.IsNullOrEmpty(_shipHawkSettings.ApiKey) || string.IsNullOrEmpty(_shipHawkSettings.ApiUrl))
                return;

            ValidateAndCacheAddress(eventMessage.Entity);
        }

        public void HandleEvent(EntityUpdated<Address> eventMessage)
        {
            if (eventMessage?.Entity == null)
                return;

            if (string.IsNullOrEmpty(_shipHawkSettings.ApiKey) || string.IsNullOrEmpty(_shipHawkSettings.ApiUrl))
                return;

            _shipHawkAddressService.ClearIsResidentialCache(eventMessage.Entity);
            ValidateAndCacheAddress(eventMessage.Entity);
        }

        public void HandleEvent(OrderPlacedEvent eventMessage)
        {
            var order = eventMessage?.Order;

            if (order == null ||
                string.IsNullOrEmpty(order.ShippingRateComputationMethodSystemName) ||
                !order.ShippingRateComputationMethodSystemName.Contains("ShipHawk"))
                return;

            var sanitized = SanitizeServiceName(order.ShippingMethod);
            var attributeKey = "ShipHawkRateDetail_" + sanitized;

            var allCustomerAttributes = _genericAttributeService.GetAttributesForEntity(order.CustomerId, "Customer");
            var rateDetailAttr = allCustomerAttributes.FirstOrDefault(a =>
                a.Key != null && a.Key.Equals(attributeKey, StringComparison.OrdinalIgnoreCase) && a.StoreId == order.StoreId);
            var detailJson = rateDetailAttr?.Value;

            if (string.IsNullOrEmpty(detailJson))
            {
                _logger.Information("ShipHawk: No rate detail found for order " + order.Id + ", shipping method '" + order.ShippingMethod + "'");
                return;
            }

            try
            {
                var detail = JsonConvert.DeserializeObject<ShipHawkRateDetail>(detailJson);

                if (detail?.WarehouseBreakdowns == null || !detail.WarehouseBreakdowns.Any())
                    return;

                var noteLines = detail.WarehouseBreakdowns
                    .Select(b => "- " + b.WarehouseCode + ": " + b.ServiceName + " - $" + b.Rate.ToString("F2"));
                var note = "ShipHawk Rate Detail (selected: " + detail.ServiceName + "):\n" +
                           string.Join("\n", noteLines);

                order.OrderNotes.Add(new OrderNote
                {
                    OrderId = order.Id,
                    Note = note,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);

                var allAttributes = _genericAttributeService.GetAttributesForEntity(order.CustomerId, "Customer");
                var rateDetailAttributes = allAttributes
                    .Where(a => a.Key != null && a.Key.StartsWith("ShipHawkRateDetail_"))
                    .ToList();

                foreach (var attr in rateDetailAttributes)
                {
                    _genericAttributeService.DeleteAttribute(attr);
                }

                if (rateDetailAttributes.Any())
                {
                    _logger.Information("ShipHawk: Cleaned up " + rateDetailAttributes.Count + " ShipHawkRateDetail_* attributes for order " + order.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ShipHawk: Error writing order note for order " + order.Id + ": " + ex.Message, ex);
            }
        }

        private void ValidateAndCacheAddress(Address address)
        {
            try
            {
                if (string.IsNullOrEmpty(address.Address1) ||
                    string.IsNullOrEmpty(address.City) ||
                    string.IsNullOrEmpty(address.ZipPostalCode))
                {
                    return;
                }

                var validatedAddress = _shipHawkAddressService.ValidateAddress(address);

                if (validatedAddress?.IsResidential.HasValue == true)
                {
                    _shipHawkAddressService.SaveIsResidential(address, validatedAddress.IsResidential.Value);

                    if (_shipHawkSettings.Tracing)
                    {
                        _logger.Information("ShipHawk: Address validated - " + address.Address1 + ", " + address.City + ", " + address.ZipPostalCode + " - IsResidential: " + validatedAddress.IsResidential.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("ShipHawk: Failed to validate address during save - " + address.Address1 + ", " + address.City + ", " + address.ZipPostalCode + ": " + ex.Message, ex);
            }
        }

        private string SanitizeServiceName(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
                return "unknown";

            var sanitized = serviceName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "")
                .Replace("$", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("/", "_")
                .Replace("\\", "_");

            while (sanitized.Contains("__"))
                sanitized = sanitized.Replace("__", "_");

            return sanitized.Trim('_').ToLowerInvariant();
        }
    }
}