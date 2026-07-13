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
            builder.Services.AddDbContextFactory<PlanningBoardDbContext>();
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
