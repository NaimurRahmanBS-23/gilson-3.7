using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Checkout
{
    public partial class CheckoutCompletedModel : BaseNopModel
    {
        public int OrderId { get; set; }
        public bool OnePageCheckoutEnabled { get; set; }

        //HACK: Customization - MSS - Fields for Guest reqistration on checkout completed.
        public bool IsGuestCustomer { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool CatalogOnly { get; set; }
        //HACK: Customization - MSS - Fields for download only and orders contains download
        public bool TrialDownloadOnly { get; set; }
        public bool DownloadOnly { get; set; }
        public bool ContainsTrialDownload { get; set; }
        public bool ContainsDownload { get; set; }
    }
}