using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Validators;
using NopStation.Plugin.Misc.DiscountManagerPlus.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure.Cache;
using NopStation.Plugin.Misc.DiscountManagerPlus.Factories;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class PluginNopStartup : INopStartup
{
    #region Methods

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNopStationServices("NopStation.Plugin.Misc.DiscountManagerPlus");

        services.AddScoped<IPromotionRuleService, PromotionRuleService>();
        services.AddScoped<IPromotionEvaluationContextFactory, PromotionEvaluationContextFactory>();
        services.AddScoped<IPromotionConditionEvaluator, PromotionConditionEvaluator>();
        services.AddScoped<IPromotionDiscountAllocator, PromotionDiscountAllocator>();
        services.AddScoped<IRewardSynchronizationService, RewardSynchronizationService>();
        services.AddScoped<IPromotionRuleEvaluator, PromotionRuleEvaluator>();
        services.AddScoped<IDiscountCoordinationService, DiscountCoordinationService>();
        services.AddScoped<IDiscountManagerPlusService, DiscountManagerPlusService>();
        services.AddScoped<IDiscountManagerPlusRequirementService, DiscountManagerPlusRequirementService>();
        services.AddScoped<IPromotionOfferModelFactory, PromotionOfferModelFactory>();
        services.AddScoped<IPromotionRuleExcludedProductService, PromotionRuleExcludedProductService>();
        services.AddScoped<DiscountManagerPlusEventConsumer>();
        services.AddScoped<DiscountManagerPlusCartEventConsumer>();
        services.AddScoped<IRewardSynchronizationService, RewardSynchronizationService>();
        services.AddScoped<IPromotionRuleEvaluator, PromotionRuleEvaluator>();
        services.AddScoped<IDiscountCoordinationService, DiscountCoordinationService>();
        services.AddScoped<IDiscountManagerPlusService, DiscountManagerPlusService>();
        services.AddScoped<IDiscountManagerPlusRequirementService, DiscountManagerPlusRequirementService>();
        services.AddScoped<IPromotionOfferModelFactory, PromotionOfferModelFactory>();
        services.AddScoped<IPromotionRuleExcludedProductService, PromotionRuleExcludedProductService>();
        services.AddScoped<DiscountManagerPlusEventConsumer>();
        services.AddScoped<DiscountManagerPlusCartEventConsumer>();
        services.AddScoped<DiscountManagerPlusCartDisplayEventConsumer>();
        services.AddScoped<CheckoutPendingRewardSelectionFilter>();
        services.AddScoped<DiscountRequirementEventConsumer>();
        services.AddScoped<OrderPlacedEventConsumer>();
        services.AddScoped<PromotionRuleCacheEventConsumer>();
        services.AddScoped<PromotionRuleProductCacheEventConsumer>();
        services.AddScoped<PromotionRuleConditionCacheEventConsumer>();
        services.AddScoped<PromotionRuleTierCacheEventConsumer>();
        services.AddScoped<PromotionRuleTierProductMappingCacheEventConsumer>();

        // NEW: Register attention messaging service
        services.AddScoped<IPromotionAttentionMessageService, PromotionAttentionMessageService>();

        services.AddScoped<IPromotionRuleModelFactory, PromotionRuleModelFactory>();
        services.AddScoped<IPromotionAnalyticsModelFactory, PromotionAnalyticsModelFactory>();
        services.AddScoped<DiscountManagerPlusDiscountDetailsViewComponent>();
        services.AddScoped<PromotionBadgeViewComponent>();
        services.AddScoped<CartSavingsViewComponent>();
        services.AddScoped<OffersLinkViewComponent>();

        services.AddScoped<IValidator<PromotionRuleModel>, PromotionRuleValidator>();
        services.AddScoped<IValidator<PromotionRuleTierModel>, PromotionRuleTierValidator>();
        services.AddScoped<IValidator<DiscountManagerPlusRequirementModel>, DiscountManagerPlusRequirementValidator>();

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<CheckoutPendingRewardSelectionFilter>();
        });
    }

    public void Configure(IApplicationBuilder application)
    {
        using var scope = application.ApplicationServices.CreateScope();
        var migrationManager = scope.ServiceProvider.GetRequiredService<IMigrationManager>();
        migrationManager.ApplyUpMigrations(typeof(DiscountManagerPlusPlugin).Assembly, MigrationProcessType.NoMatter);
    }

    #endregion

    #region Properties

    public int Order => 10;

    #endregion
}
