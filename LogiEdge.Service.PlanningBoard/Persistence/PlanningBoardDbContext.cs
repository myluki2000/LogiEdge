using LogiEdge.Service.PlanningBoard.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.Service.PlanningBoard.Persistence
{
    public class PlanningBoardDbContext(DbContextOptions<PlanningBoardDbContext> options, IConfiguration configuration) 
        : LogiEdgeDbContext<PlanningBoardDbContext>(options, configuration)
    {
        public DbSet<Board> Boards { get; set; }
    }
}
