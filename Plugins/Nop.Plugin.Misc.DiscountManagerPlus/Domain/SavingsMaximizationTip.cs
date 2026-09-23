using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class SavingsMaximizationTip
    {
        public SavingsMaximizationTip()
        {
            SuggestedProducts = new List<string>();
            SuggestedProductIds = new List<int>();
            GeneratedAt = DateTime.UtcNow;
        }

        public string TipId { get; set; }
        public string Message { get; set; }
        public MaximizationType MaximizationType { get; set; }
        public decimal PotentialSavings { get; set; }
        public IList<string> SuggestedProducts { get; set; }
        public IList<int> SuggestedProductIds { get; set; }
        public int SuggestedQuantity { get; set; }
        public string TargetProductName { get; set; }
        public int? TargetProductId { get; set; }
        public int CurrentQuantity { get; set; }
        public int RecommendedQuantity { get; set; }
        public string RelatedPromotionName { get; set; }
        public int Priority { get; set; }
        public bool IsQuickWin { get; set; }
        public int? EstimatedTimeToAchieve { get; set; }
        public string AdditionalDetails { get; set; }
        public string ActionUrl { get; set; }
        public bool IsActedUpon { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public enum MaximizationType
    {
        AddProduct = 1,
        IncreaseQuantity = 2,
        AddDifferentProduct = 3,
        ReplaceProduct = 4,
        CreateBundle = 5,
        UpgradeProduct = 6,
        GeneralOptimization = 7
    }
}
