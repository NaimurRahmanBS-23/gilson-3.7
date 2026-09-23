using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Models
{
    public partial class OfferPageLinkModel : BaseNopModel
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }
}
