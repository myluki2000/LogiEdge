using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using LogiEdge.Service.PlanningBoard.Persistence;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Service.PlanningBoard
{
    public class PlanningBoardServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
                                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContextFactory<PlanningBoardDbContext>(options => options.UseNpgsql(connectionString));
        }

        public void OnAppBuilt(WebApplication app)
        {
            using PlanningBoardDbContext ctx = app.Services.GetService<IDbContextFactory<PlanningBoardDbContext>>()!
                .CreateDbContext();

            ctx.Database.Migrate();
        }

        public Assembly Assembly => typeof(PlanningBoardServiceModuleConfiguration).Assembly;
    }
}
