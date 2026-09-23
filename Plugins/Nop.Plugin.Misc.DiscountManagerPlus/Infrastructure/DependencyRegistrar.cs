using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Data;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        private const string CONTEXT_NAME = "nop_object_context_discount_manager_plus";
        private const string BASE_PRICE_CALCULATION_SERVICE = "nop_price_calculation_service";

        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            var basePriceCalculation = ResolvedParameter.ForNamed<IPriceCalculationService>(BASE_PRICE_CALCULATION_SERVICE);

            builder.RegisterType<PriceCalculationService>()
                .Named<IPriceCalculationService>(BASE_PRICE_CALCULATION_SERVICE)
                .InstancePerLifetimeScope();

            builder.RegisterType<PromotionRuleService>().As<IPromotionRuleService>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionEvaluationContextFactory>().As<IPromotionEvaluationContextFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionConditionEvaluator>().As<IPromotionConditionEvaluator>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<PromotionDiscountAllocator>().As<IPromotionDiscountAllocator>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<RewardSynchronizationService>().As<IRewardSynchronizationService>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionRuleEvaluator>().As<IPromotionRuleEvaluator>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<DiscountCoordinationService>().As<IDiscountCoordinationService>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<DiscountManagerPlusService>().As<IDiscountManagerPlusService>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<DiscountManagerPlusRequirementService>().As<IDiscountManagerPlusRequirementService>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionOfferModelFactory>().As<IPromotionOfferModelFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionRuleExcludedProductService>().As<IPromotionRuleExcludedProductService>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionAttentionMessageService>().As<IPromotionAttentionMessageService>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<PromotionRuleModelFactory>().As<IPromotionRuleModelFactory>()
                .WithParameter(basePriceCalculation)
                .InstancePerLifetimeScope();
            builder.RegisterType<PromotionAnalyticsModelFactory>().As<IPromotionAnalyticsModelFactory>().InstancePerLifetimeScope();
            builder.RegisterType<DiscountManagerPlusPriceCalculationService>().As<IPriceCalculationService>().InstancePerLifetimeScope();

            this.RegisterPluginDataContext<DiscountManagerPlusObjectContext>(builder, CONTEXT_NAME);

            builder.RegisterType<EfRepository<PromotionRule>>()
                .As<IRepository<PromotionRule>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleProduct>>()
                .As<IRepository<PromotionRuleProduct>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleCondition>>()
                .As<IRepository<PromotionRuleCondition>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleTier>>()
                .As<IRepository<PromotionRuleTier>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleUsage>>()
                .As<IRepository<PromotionRuleUsage>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleExcludedProduct>>()
                .As<IRepository<PromotionRuleExcludedProduct>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionRuleTierProductMapping>>()
                .As<IRepository<PromotionRuleTierProductMapping>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
            builder.RegisterType<EfRepository<PromotionSocialShareEvent>>()
                .As<IRepository<PromotionSocialShareEvent>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME))
                .InstancePerLifetimeScope();
        }

        public int Order
        {
            get { return 20; }
        }
    }
}
