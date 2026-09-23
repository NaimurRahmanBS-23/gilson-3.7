using System.Web.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DiscountManagerPlusViewEngine : RazorViewEngine
    {
        public DiscountManagerPlusViewEngine()
        {
            var locations = new[]
            {
                "~/Plugins/Misc.DiscountManagerPlus/Views/{1}/{0}.cshtml",
                "~/Plugins/Misc.DiscountManagerPlus/Views/Shared/{0}.cshtml"
            };

            ViewLocationFormats = locations;
            PartialViewLocationFormats = locations;
            AreaViewLocationFormats = locations;
            AreaPartialViewLocationFormats = locations;
            MasterLocationFormats = new[]
            {
                "~/Administration/Views/Shared/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml"
            };
            AreaMasterLocationFormats = MasterLocationFormats;
        }
    }
}
