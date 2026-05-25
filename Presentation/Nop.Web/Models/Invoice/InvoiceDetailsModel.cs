using System;
using System.Collections.Generic;
using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Common;

namespace Nop.Web.Models.Invoice
{
    public partial class InvoiceDetailsModel : BaseNopEntityModel
    {
        public InvoiceDetailsModel()
        {
            Items = new List<InvoiceItemModel>();
            BillingAddress = new AddressModel();
            CustomValues = new Dictionary<string, object>();
            ShippingAddress = new AddressModel();
        }
        
        public string GilsonCustomerNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public string OrderNumber { get; set; }

        public bool PrintMode { get; set; }
        public bool PdfInvoiceDisabled { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime InvoiceDate { get; set; }

        public AddressModel BillingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public Dictionary<string, object> CustomValues { get; set; }
        public string CheckoutAttributeInfo { get; set; }

        public AddressModel ShippingAddress { get; set; }
        public string ShippingMethod { get; set; }
        
        public string InvoiceSubtotal { get; set; }
        public string Tax { get; set; }
        public string InvoiceShipping { get; set; }
        public string InvoiceTotal { get; set; }
        
        public IList<InvoiceItemModel> Items { get; set; }
        
		#region Nested Classes

        public partial class InvoiceItemModel : BaseNopEntityModel
        {
            public int ProductId { get; set; }
            public string Sku { get; set; }
            public string ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string UnitPrice { get; set; }
            public int Quantity { get; set; }
            public string SubTotal { get; set; }
        }

		#endregion
    }
}