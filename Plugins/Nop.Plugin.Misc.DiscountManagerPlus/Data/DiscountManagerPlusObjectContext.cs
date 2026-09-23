using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Data
{
    public class DiscountManagerPlusObjectContext : DbContext, IDbContext
    {
        public DiscountManagerPlusObjectContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new PromotionRuleMap());
            modelBuilder.Configurations.Add(new PromotionRuleProductMap());
            modelBuilder.Configurations.Add(new PromotionRuleConditionMap());
            modelBuilder.Configurations.Add(new PromotionRuleTierMap());
            modelBuilder.Configurations.Add(new PromotionRuleUsageMap());
            modelBuilder.Configurations.Add(new PromotionRuleExcludedProductMap());
            modelBuilder.Configurations.Add(new PromotionRuleTierProductMappingMap());
            modelBuilder.Configurations.Add(new PromotionSocialShareEventMap());
            base.OnModelCreating(modelBuilder);
        }

        public string CreateDatabaseScript()
        {
            return ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript();
        }

        public new IDbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        public void Install()
        {
            var dbScript = CreateDatabaseScript();
            Database.ExecuteSqlCommand(dbScript);
            SaveChanges();
        }

        public void Uninstall()
        {
            this.DropPluginTable(this.GetTableName<PromotionRuleExcludedProduct>());
            this.DropPluginTable(this.GetTableName<PromotionRuleTierProductMapping>());
            this.DropPluginTable(this.GetTableName<PromotionSocialShareEvent>());
            this.DropPluginTable(this.GetTableName<PromotionRuleUsage>());
            this.DropPluginTable(this.GetTableName<PromotionRuleTier>());
            this.DropPluginTable(this.GetTableName<PromotionRuleCondition>());
            this.DropPluginTable(this.GetTableName<PromotionRuleProduct>());
            this.DropPluginTable(this.GetTableName<PromotionRule>());
        }

        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            ((IObjectContextAdapter)this).ObjectContext.Detach(entity);
        }

        public virtual bool ProxyCreationEnabled
        {
            get { return this.Configuration.ProxyCreationEnabled; }
            set { this.Configuration.ProxyCreationEnabled = value; }
        }

        public virtual bool AutoDetectChangesEnabled
        {
            get { return this.Configuration.AutoDetectChangesEnabled; }
            set { this.Configuration.AutoDetectChangesEnabled = value; }
        }
    }
}
