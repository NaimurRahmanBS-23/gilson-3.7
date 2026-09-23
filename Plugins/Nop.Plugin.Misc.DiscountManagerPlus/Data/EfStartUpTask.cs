using System.Data.Entity;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            Database.SetInitializer<DiscountManagerPlusObjectContext>(null);
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
