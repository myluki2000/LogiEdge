using LogiEdge.Service.PlanningBoard.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.PlanningBoard.Persistence
{
    public class PlanningBoardDbContext(DbContextOptions<PlanningBoardDbContext> options) : LogiEdgeDbContext(options)
    {
        public DbSet<Board> Boards { get; set; }
    }
}
