using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Web.Framework.Themes;

namespace Bss.Nop.Plugin.Custom
{
    public class BssCustomViewEngine : ThemeableRazorViewEngine
    {
        public BssCustomViewEngine()
        {
            ViewLocationFormats = new[] { "~/Plugins/Bss.Nop.Plugin.Custom/Views/{1}/{0}.cshtml" };
            PartialViewLocationFormats = new[] { "~/Plugins/Bss.Nop.Plugin.Custom/Views/{1}/{0}.cshtml" };
            AreaPartialViewLocationFormats = new[] { "~/Plugins/Bss.Nop.Plugin.Custom/Views/{1}/{0}.cshtml" };
            AreaViewLocationFormats = new[] { "~/Plugins/Bss.Nop.Plugin.Custom/Views/{1}/{0}.cshtml" };
        }
    }
}
