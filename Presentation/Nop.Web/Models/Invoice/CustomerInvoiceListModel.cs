using System;
using System.Collections.Generic;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Invoice
{
    public partial class CustomerInvoiceListModel : BaseNopModel
    {
        public CustomerInvoiceListModel()
        {
            Invoices = new List<InvoiceDetailsModel>();
        }

        public IList<InvoiceDetailsModel> Invoices { get; set; }
        
        #region Nested classes

        public partial class InvoiceDetailsModel : BaseNopEntityModel
        {
            public string InvoiceNumber { get; set; }
            public string InvoiceTotal { get; set; }
            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}