using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.DiscountManagerPlus
{
    public class DiscountManagerPlusPermissionProvider : IPermissionProvider
    {
        public static readonly PermissionRecord ManageConfiguration = new PermissionRecord
        {
            Name = "Admin area. Manage Discount Manager Plus Configuration",
            SystemName = "ManageDiscountManagerPlusConfiguration",
            Category = "DiscountManagerPlus"
        };

        public static readonly PermissionRecord ManagePromotionRules = new PermissionRecord
        {
            Name = "Admin area. Manage Discount Manager Plus Rules",
            SystemName = "ManageDiscountManagerPlusRules",
            Category = "DiscountManagerPlus"
        };

        public virtual IEnumerable<PermissionRecord> GetPermissions()
        {
            return new[]
            {
                ManageConfiguration,
                ManagePromotionRules
            };
        }

        public virtual IEnumerable<DefaultPermissionRecord> GetDefaultPermissions()
        {
            return new[]
            {
                new DefaultPermissionRecord
                {
                    CustomerRoleSystemName = SystemCustomerRoleNames.Administrators,
                    PermissionRecords = new[]
                    {
                        ManageConfiguration,
                        ManagePromotionRules
                    }
                }
            };
        }
    }
}
