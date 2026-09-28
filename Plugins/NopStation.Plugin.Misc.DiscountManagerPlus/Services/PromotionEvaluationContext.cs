using Nop.Core.Domain.Orders;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class PromotionEvaluationContext
{
    public int StoreId { get; set; }
    public int CustomerId { get; set; }
    public IList<ShoppingCartItem> Cart { get; set; } = new List<ShoppingCartItem>();
    public IList<string> CouponCodes { get; set; } = new List<string>();
    public string SelectedPaymentMethodSystemName { get; set; } = string.Empty;
    public string CountryIso2 { get; set; } = string.Empty;
    public DateTime CurrentUtc { get; set; } = DateTime.UtcNow;
    public string SocialShareProofToken { get; set; } = string.Empty;
}
