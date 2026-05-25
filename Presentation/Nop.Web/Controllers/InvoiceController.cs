using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Web.Framework.Security;
using Nop.Web.Models.Common;
using Nop.Web.Models.Invoice;


namespace Nop.Web.Controllers
{
    public partial class InvoiceController : BasePublicController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly PdfSettings _pdfSettings;

        #endregion

        #region Constructors

        public InvoiceController(IWorkContext workContext, PdfSettings pdfSettings)
        {
            this._workContext = workContext;
            this._pdfSettings = pdfSettings;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual CustomerInvoiceListModel PrepareGpCustomerInvoiceListModel()
        {
            //HACK: Customization - MSS - GP Invoices
            
            var model = new CustomerInvoiceListModel();

            if (string.IsNullOrWhiteSpace(_workContext.CurrentCustomer.GilsonCustomerNumber)) return model;

            var dataSettingsManager = new Core.Data.DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();
                                        
            using (var conn = new SqlConnection(dataProviderSettings.DataConnectionString))
            {
                using (var cmd = new SqlCommand{Connection = conn, CommandText = "GetGPInvoices", CommandType = CommandType.StoredProcedure})
                {
                    cmd.Parameters.Add("@GilsonCustomerNumber", SqlDbType.Text).Value = _workContext.CurrentCustomer.GilsonCustomerNumber;
                            
                    cmd.Connection.Open();
                    var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                    while (dr.Read())
                    {
                        var invoiceModel = new CustomerInvoiceListModel.InvoiceDetailsModel
                        {
                            InvoiceNumber = (string) dr["InvoiceNumber"],
                            CreatedOn = (DateTime) dr["InvoiceDate"],
                            InvoiceTotal = (string) dr["InvoiceTotal"]
                        };

                        model.Invoices.Add(invoiceModel);
                    }
                }
            }
            return model;
        }
        

        [NonAction]
        protected virtual InvoiceDetailsModel PrepareGpInvoiceDetailsModel(string invoiceNumber)
        {
            //HACK: Customization - MSS - PrepareGPInvoiceDetailsModel

            if (string.IsNullOrWhiteSpace(invoiceNumber)) return null;
            if (string.IsNullOrWhiteSpace(_workContext.CurrentCustomer.GilsonCustomerNumber)) return null;

            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();

            InvoiceDetailsModel model;
            
            using (var conn = new SqlConnection(dataProviderSettings.DataConnectionString))
            {
                #region "Invoice Header"

                using (var cmdHeader = new SqlCommand{Connection = conn, CommandText = "GetGPInvoiceHeader", CommandType = CommandType.StoredProcedure})
                {
                    cmdHeader.Parameters.Add("@InvoiceNumber", SqlDbType.VarChar).Value = invoiceNumber;

                    cmdHeader.Connection.Open();
                    using (var dr = cmdHeader.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (dr.Read())
                        {
                            //return model for Current Customer only
                            if (_workContext.CurrentCustomer.GilsonCustomerNumber != (string)dr["GilsonCustomerNumber"]) return null;

                            model = new InvoiceDetailsModel
                            {
                                InvoiceNumber = (string)dr["InvoiceNumber"],
                                OrderNumber = (string)dr["OrderNumber"],
                                OrderDate = (DateTime)dr["OrderDate"],
                                InvoiceDate = (DateTime) dr["InvoiceDate"],
                                GilsonCustomerNumber = (string)dr["GilsonCustomerNumber"],
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
                                InvoiceSubtotal = (string)dr["SubTotal"],
                                InvoiceShipping = (string)dr["Shipping"],
                                Tax = (string) dr["Tax"],
                                InvoiceTotal = (string)dr["InvoiceTotal"]
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                }


                #endregion


                #region "Invoice Detail"

                //purchased products
                using (var cmdItems = new SqlCommand{Connection = conn, CommandText = "GetGPInvoiceDetails", CommandType = CommandType.StoredProcedure})
                {
                    cmdItems.Parameters.Add("@InvoiceNumber", SqlDbType.VarChar).Value = invoiceNumber;
                    cmdItems.Connection.Open();
                    using (var dr = cmdItems.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            var invoiceItemModel = new InvoiceDetailsModel.InvoiceItemModel
                            {
                                Sku = (string) dr["ItemNumber"],
                                ProductName = (string)dr["ProductName"],
                                ProductSeName = (string)dr["ProductSeName"],
                                Quantity = (int)dr["Quantity"],
                                UnitPrice = (string)dr["UnitPrice"],
                                SubTotal = (string)dr["ExtendedPrice"],
                            };
                            model.Items.Add(invoiceItemModel);
                        }
                    }
                }

                #endregion

            }
            
            return model;
        }
        
        #endregion

        #region Methods

        //My account / Invoices
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult CustomerInvoices()
        {
            //Not a registered customer?  GTFO!!!
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            var model = PrepareGpCustomerInvoiceListModel();

            if (model == null)
            {
                return RedirectToRoute("CustomerInvoices");
            }

            return View(model);
        }

        //My account / Invoice details page
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult Details(string invoiceNumber)
        {
            //Not a registered customer?  GTFO!!!
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new HttpUnauthorizedResult();

            var model = PrepareGpInvoiceDetailsModel(invoiceNumber);

            if (model == null)
            {
                return RedirectToRoute("CustomerInvoices");
            }

            return View(model);
        }


        //My account / Invoice details page / Print
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult PrintInvoiceDetails(string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber)) return RedirectToRoute("CustomerInvoices");

            var model = PrepareGpInvoiceDetailsModel(invoiceNumber);

            if (model == null)
            {
                return RedirectToRoute("CustomerInvoices");
            }

            model.PrintMode = true;

            return View("Details", model);
        }



        #endregion
    }
}
