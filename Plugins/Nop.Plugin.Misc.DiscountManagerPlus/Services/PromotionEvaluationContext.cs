using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionEvaluationContext
    {
        public PromotionEvaluationContext()
        {
            Cart = new List<ShoppingCartItem>();
            CouponCodes = new List<string>();
            SelectedPaymentMethodSystemName = string.Empty;
            CountryIso2 = string.Empty;
            CurrentUtc = DateTime.UtcNow;
            SocialShareProofToken = string.Empty;
        }

        public int StoreId { get; set; }
        public int CustomerId { get; set; }
        public IList<ShoppingCartItem> Cart { get; set; }
        public IList<string> CouponCodes { get; set; }
        public string SelectedPaymentMethodSystemName { get; set; }
        public string CountryIso2 { get; set; }
        public DateTime CurrentUtc { get; set; }
        public string SocialShareProofToken { get; set; }
    }
}
