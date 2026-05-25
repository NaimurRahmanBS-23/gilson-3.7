using Nop.Services.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Bss.Nop.Plugin.Custom.Admin.Models;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Security;

namespace Bss.Nop.Plugin.Custom.Admin.Controllers
{
    public class USPSTabController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;

        public USPSTabController(
            ICategoryService categoryService,
            IProductService productService,
            IGenericAttributeService genericAttributeService,
            IPermissionService permissionService,
            IStoreContext storeContext
            )
        {
            this._categoryService = categoryService;
            this._productService = productService;
            this._genericAttributeService = genericAttributeService;
            this._permissionService = permissionService;
            this._storeContext = storeContext;
        }

        // Load product admin fan builder tab view
        public ActionResult GetUSPSConfig(int productID)
        {
            if (ControllerContext == null)
            {
                ControllerContext context = new ControllerContext(System.Web.HttpContext.Current.Request.RequestContext, this);
                ControllerContext = context;
            }
            var product =_productService.GetProductById(productID);
            USPSTabModel model = new USPSTabModel();
            model.ProductId = productID;
            try
            {
                string keyValue = product.GetAttribute<string>("CanUSPS");
                if (keyValue != null)
                {
                    if (keyValue == "True")
                    {
                        model.USPS = true;
                    }
                    else
                    {
                        model.USPS = false;
                    }
                }
            }
            catch (Exception e)
            {
 
            }

            return PartialView("~/Plugins/Bss.Nop.Plugin.Custom/Admin/Views/USPSTab.cshtml", model);
        }
        // Get product id from url route
        public int GetProductId()
        {
            if (ControllerContext == null)
            {
                ControllerContext context = new ControllerContext(System.Web.HttpContext.Current.Request.RequestContext, this);
                ControllerContext = context;
            }
            int productId = Convert.ToInt32(ControllerContext.RequestContext.RouteData.Values["id"]);
            return productId;
        }
        public void SaveUSPSConfig(string isUSPS, int productId)
        {

            var product = _productService.GetProductById(productId);
            _genericAttributeService.SaveAttribute<string>(product, "CanUSPS", isUSPS);
        }
    }
}
