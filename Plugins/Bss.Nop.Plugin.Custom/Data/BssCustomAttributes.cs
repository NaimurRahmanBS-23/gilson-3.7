using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;

namespace Bss.Nop.Plugin.Custom.Data
{
    public class CustomAttributes
    {
        #region Fields

        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICustomerAttributeParser _customerAttributeParser;

        #endregion

        #region Ctor

        public CustomAttributes(
            ICheckoutAttributeParser checkoutAttributeParser,
            ICustomerAttributeParser customerAttributeParser)
        {
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._customerAttributeParser = customerAttributeParser;

        }

        #endregion

        public string GetCustomerId(Customer customer)
        {
            return GetCustomerTextAttribute(customer, "Customer");
        }

        public string GetCustomerTerms(Customer customer)
        {
            return GetCustomerTextAttribute(customer, "Terms");
        }

        private string GetCustomerTextAttribute(Customer customer, string attributeName)
        {
            string attrVal = string.Empty;

            var customCustomerAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CustomCustomerAttributes);

            var attributeList = _customerAttributeParser.ParseCustomerAttributes(customCustomerAttributesXml);
            var customerAttribute = attributeList.FirstOrDefault(a => a.Name == attributeName);
            if (customerAttribute != null)
            {
                var valueStr = _customerAttributeParser.ParseValues(customCustomerAttributesXml, customerAttribute.Id);
                if (valueStr != null)
                {
                    attrVal = valueStr[0];
                }
            }
            return attrVal;
        }

        public string GetPoNumber(Order order)
        {
            string poNumber = string.Empty;

            var attributeList = _checkoutAttributeParser.ParseCheckoutAttributes(order.CheckoutAttributesXml);
            var poAttribute = attributeList.FirstOrDefault(a => a.Name == "PurchaseOrder");
            if (poAttribute != null)
            {
                var valueStr = _checkoutAttributeParser.ParseValues(order.CheckoutAttributesXml, poAttribute.Id);
                if (valueStr != null)
                {
                    poNumber = valueStr[0];
                }
            }
            return poNumber;
        }

    }
}
