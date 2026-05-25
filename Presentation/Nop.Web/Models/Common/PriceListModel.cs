using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Common
{
    public partial class PriceListModel : BaseNopModel
    {
        public string PriceListPdf { get; set; }
        public string DealerPriceListPdf { get; set; }
    }
}