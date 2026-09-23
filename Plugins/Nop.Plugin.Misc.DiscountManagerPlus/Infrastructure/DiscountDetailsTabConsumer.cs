using System;
using System.Text;
using System.Web.Mvc;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DiscountDetailsTabConsumer : IConsumer<AdminTabStripCreated>
    {
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;

        public DiscountDetailsTabConsumer(
            ILocalizationService localizationService,
            IPermissionService permissionService)
        {
            _localizationService = localizationService;
            _permissionService = permissionService;
        }

        public void HandleEvent(AdminTabStripCreated eventMessage)
        {
            if (eventMessage == null || eventMessage.TabStripName != "discount-edit")
                return;

            if (!_permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManagePromotionRules) &&
                !_permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManageConfiguration))
                return;

            var discountId = GetDiscountId(eventMessage);
            if (discountId <= 0)
                return;

            var tabName = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Title");
            if (string.IsNullOrWhiteSpace(tabName) || tabName == "Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Title")
                tabName = "DiscountManagerPlus";

            var url = "/Plugins/DiscountManagerPlus/DiscountDetails?discountId=" + discountId;
            var sb = new StringBuilder();
            sb.Append("<script language=\"javascript\" type=\"text/javascript\">");
            sb.Append(Environment.NewLine);
            sb.Append("$(document).ready(function () {");
            sb.Append(Environment.NewLine);
            sb.Append("var kTabs = $('#discount-edit').data('kendoTabStrip');");
            sb.Append(Environment.NewLine);
            sb.Append("if (kTabs) {");
            sb.Append(Environment.NewLine);
            sb.Append(" kTabs.append({ text: \"" + tabName.Replace("\"", "\\\"") + "\", contentUrl: \"" + url + "\" });");
            sb.Append(Environment.NewLine);
            sb.Append("}");
            sb.Append(Environment.NewLine);
            sb.Append("});");
            sb.Append(Environment.NewLine);
            sb.Append("</script>");
            sb.Append(Environment.NewLine);
            eventMessage.BlocksToRender.Add(MvcHtmlString.Create(sb.ToString()));
        }

        private static int GetDiscountId(AdminTabStripCreated eventMessage)
        {
            if (eventMessage.Helper == null || eventMessage.Helper.ViewContext == null)
                return 0;

            var routeData = eventMessage.Helper.ViewContext.RouteData;
            if (routeData != null && routeData.Values != null && routeData.Values.ContainsKey("id"))
            {
                int routeId;
                if (int.TryParse(Convert.ToString(routeData.Values["id"]), out routeId) && routeId > 0)
                    return routeId;
            }

            var model = eventMessage.Helper.ViewData != null ? eventMessage.Helper.ViewData.Model : null;
            if (model == null)
                return 0;

            var idProperty = model.GetType().GetProperty("Id");
            if (idProperty == null)
                return 0;

            return Convert.ToInt32(idProperty.GetValue(model, null));
        }
    }
}
