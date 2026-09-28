using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace NopStation.Plugin.Misc.DiscountManagerPlus;

public class DiscountManagerPlusPermissionProvider
{
    public const string MANAGE_CONFIGURATION = "ManageNopStationDiscountManagerPlusConfiguration";
    public const string MANAGE_PROMOTION_RULES = "ManageNopStationDiscountManagerPlusRules";
}

public class DiscountManagerPlusPermissionConfigManager : IPermissionConfigManager
{
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("NopStation DiscountManagerPlus. Manage Configuration", DiscountManagerPlusPermissionProvider.MANAGE_CONFIGURATION, "NopStation", NopCustomerDefaults.AdministratorsRoleName),
        new("NopStation DiscountManagerPlus. Manage Promotion Rules", DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES, "NopStation", NopCustomerDefaults.AdministratorsRoleName)
    };
}
