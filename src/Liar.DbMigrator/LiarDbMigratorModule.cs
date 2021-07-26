﻿using Liar.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Liar.DbMigrator
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(LiarEntityFrameworkCoreDbMigrationsModule),
        typeof(LiarApplicationContractsModule)
        )]
    public class LiarDbMigratorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        { 
        }
    }
}
