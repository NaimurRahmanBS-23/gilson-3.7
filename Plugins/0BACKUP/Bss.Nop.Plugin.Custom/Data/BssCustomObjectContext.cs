using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Data.Mapping;

namespace Bss.Nop.Plugin.Custom.Data
{
    public class BssCustomObjectContext : DbContext, IDbContext
    {
        public BssCustomObjectContext(string nameOrConnectionString) : base(nameOrConnectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Dynamically load all NopEntityTypeConfiguration in this plugin
            // so we don't have to add each one manually

            var typesToRegister = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => !String.IsNullOrEmpty(type.Namespace))
            .Where(type => type.BaseType != null && type.BaseType.IsGenericType &&
                type.BaseType.GetGenericTypeDefinition() == typeof(NopEntityTypeConfiguration<>));
            foreach (var type in typesToRegister)
            {
                dynamic configurationInstance = Activator.CreateInstance(type);
                modelBuilder.Configurations.Add(configurationInstance);
            }

            base.OnModelCreating(modelBuilder);
        }

        public string CreateDatabaseInstallationScript()
        {
            return ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript();
        }

        public void Install()
        {
            // It's required to set initializer to null (for SQL Server Compact).
            // otherwise, you'll get something like "The model backing the 'your context name' context 
			// has changed since the database was created. Consider using Code First Migrations to update the database"
            Database.SetInitializer<BssCustomObjectContext>(null);

            //create the table
            var dbScript = CreateDatabaseInstallationScript();
            if (!string.IsNullOrEmpty(dbScript))
            {
                Database.ExecuteSqlCommand(dbScript);
            }
            SaveChanges();
        }

        public void Uninstall()
        {
            //drop the table
            //var tableName = this.GetTableName<OrderIdReferenceRecord>();
            //this.DropPluginTable(tableName);
        }

        public new IDbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        public System.Collections.Generic.IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            ((IObjectContextAdapter)this).ObjectContext.Detach(entity);
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether proxy creation setting is enabled (used in EF)
        /// </summary>
        public virtual bool ProxyCreationEnabled
        {
            get
            {
                return this.Configuration.ProxyCreationEnabled;
            }
            set
            {
                this.Configuration.ProxyCreationEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether auto detect changes setting is enabled (used in EF)
        /// </summary>
        public virtual bool AutoDetectChangesEnabled
        {
            get
            {
                return this.Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                this.Configuration.AutoDetectChangesEnabled = value;
            }
        }

        #endregion
    }
}
