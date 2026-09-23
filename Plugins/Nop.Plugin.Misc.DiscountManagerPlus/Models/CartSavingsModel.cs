using System.Collections.Generic;
using System.Linq;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Models
{
    public class CartSavingsModel
    {
        public CartSavingsModel()
        {
            Items = new List<CartSavingsItemModel>();
            PendingRewards = new List<CartRewardSelectionModel>();
            Reminders = new List<string>();
            ExcludedProductWarnings = new List<string>();
            MultipleDiscountNotices = new List<string>();
            CheapestItemSelectionDetails = new List<string>();
            AttentionMessages = new List<PromotionAttentionMessage>();
            MaximizationTips = new List<SavingsMaximizationTip>();
        }

        public string SummaryText { get; set; }
        public string Title { get; set; }
        public string TotalLabel { get; set; }
        public decimal TotalSavings { get; set; }
        public string TotalSavingsFormatted { get; set; }
        public IList<CartSavingsItemModel> Items { get; set; }
        public string PendingRewardsTitle { get; set; }
        public string PendingRewardsDescription { get; set; }
        public string PendingRewardsSelectText { get; set; }
        public string PendingRewardsAddText { get; set; }
        public IList<CartRewardSelectionModel> PendingRewards { get; set; }
        public string ReminderTitle { get; set; }
        public IList<string> Reminders { get; set; }
        public IList<string> ExcludedProductWarnings { get; set; }
        public IList<string> MultipleDiscountNotices { get; set; }
        public IList<string> CheapestItemSelectionDetails { get; set; }
        public IList<PromotionAttentionMessage> AttentionMessages { get; set; }
        public bool HasAttentionMessages
        {
            get { return AttentionMessages != null && AttentionMessages.Any(); }
        }
        public DualOfferExplanation DualOfferExplanation { get; set; }
        public bool IsDualOfferScenario
        {
            get { return DualOfferExplanation != null && DualOfferExplanation.IsDualOfferScenario; }
        }
        public IList<SavingsMaximizationTip> MaximizationTips { get; set; }
        public bool HasMaximizationTips
        {
            get { return MaximizationTips != null && MaximizationTips.Any(); }
        }
        public decimal PotentialAdditionalSavings
        {
            get { return MaximizationTips == null ? 0 : MaximizationTips.Sum(t => t.PotentialSavings); }
        }
    }

    public class CartSavingsItemModel
    {
        public int PromotionRuleId { get; set; }
        public string RuleName { get; set; }
        public string BadgeText { get; set; }
        public string DetailText { get; set; }
        public bool IsBogoStyle { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountAmountFormatted { get; set; }
        public string TargetProductName { get; set; }
        public string AppliedToText { get; set; }
        public string DiscountType { get; set; }
        public string TargetSelectionText { get; set; }
        public bool IsCoordinatedDiscount { get; set; }
        public int DiscountPriority { get; set; }
    }

    public class CartRewardSelectionModel
    {
        public CartRewardSelectionModel()
        {
            Options = new List<CartRewardOptionModel>();
        }

        public int PromotionRuleId { get; set; }
        public string RuleName { get; set; }
        public int RewardQuantity { get; set; }
        public int MaxSelectableQuantity { get; set; }
        public string FormId { get; set; }
        public string PopupId { get; set; }
        public string AddToCartUrl { get; set; }
        public IList<CartRewardOptionModel> Options { get; set; }
    }

    public class CartRewardOptionModel
    {
        public CartRewardOptionModel()
        {
            Product = new ProductDetailsModel();
        }

        public int RewardProductId { get; set; }
        public string RewardProductName { get; set; }
        public string RewardProductOldPrice { get; set; }
        public string RewardProductPrice { get; set; }
        public string ImageUrl { get; set; }
        public bool IsSelected { get; set; }
        public ProductDetailsModel Product { get; set; }
    }
}
