using LogiEdge.Shared.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Shared
{
    public class LogiEdgeDbContext : DbContext
    {
        public LogiEdgeDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Conventions.Add(sp => new AutoIncludeAttributeConvention(sp.GetRequiredService<ProviderConventionSetBuilderDependencies>()));
        }
    }
}
