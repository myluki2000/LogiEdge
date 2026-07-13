using LogiEdge.Shared.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.Shared
{
    public class LogiEdgeDbContext<TContext> : DbContext where TContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public LogiEdgeDbContext(DbContextOptions<TContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string applicationConnectionString = _configuration.GetConnectionString("DatabaseConnection")
                                                 ?? throw new InvalidOperationException("Connection string 'ApplicationConnection' not found.");

            optionsBuilder
                .UseNpgsql(applicationConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseProjectables();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Conventions.Add(sp => new AutoIncludeAttributeConvention(sp.GetRequiredService<ProviderConventionSetBuilderDependencies>()));
        }
    }
}
