﻿using Volo.Abp.Modularity;

namespace Liar
{
    [DependsOn()]
    public class LiarDomainSharedModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
