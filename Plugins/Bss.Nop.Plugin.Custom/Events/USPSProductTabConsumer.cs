using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Bss.Nop.Plugin.Custom.Admin.Controllers;
using Bss.Nop.Plugin.Custom.Common;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Events;

namespace Bss.Nop.Plugin.Custom.Events
{
    public class USPSProductTabConsumer : IConsumer<AdminTabStripCreated>
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;

        public USPSProductTabConsumer(
            ICategoryService categoryService,
            IProductService productService,
            ILocalizationService localizationService,
            IGenericAttributeService genericAttributeService,
            IPermissionService permissionService,
            IStoreContext storeContext
            )
        {
            this._categoryService = categoryService;
            this._productService = productService;
            this._localizationService = localizationService;
            this._genericAttributeService = genericAttributeService;
            this._permissionService = permissionService;
            this._storeContext = storeContext;

        }
        public void HandleEvent(AdminTabStripCreated eventMessage)
        {
            if (eventMessage.TabStripName == "product-edit")
            {
                USPSTabController controller = new USPSTabController(_categoryService, _productService,
                    _genericAttributeService, _permissionService, _storeContext);

                int productId = controller.GetProductId();
                string url = "/Plugins/Bss/Admin/GetUSPSConfig?productId=" + productId;
                string tabName = "USPS";
                var sb = new StringBuilder();

                sb.Append("<script language=\"javascript\" type=\"text/javascript\">");
                sb.Append(Environment.NewLine);
                sb.Append("$(document).ready(function () {");
                sb.Append(Environment.NewLine);
                sb.Append("var kTabs = $('#product-edit').data('kendoTabStrip');");
                sb.Append(Environment.NewLine);
                sb.Append(" kTabs.append({ text: \"" + tabName + "\", contentUrl: \"" + url + "\" });");
                sb.Append(Environment.NewLine);
                sb.Append("});");
                sb.Append(Environment.NewLine);
                sb.Append("</script>");
                sb.Append(Environment.NewLine);
                eventMessage.BlocksToRender.Add(MvcHtmlString.Create(sb.ToString()));

            }
        }

    }
}
