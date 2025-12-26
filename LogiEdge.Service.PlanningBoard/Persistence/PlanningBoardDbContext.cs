using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.Service.PlanningBoard.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Service.PlanningBoard.Persistence
{
    public class PlanningBoardDbContext(DbContextOptions<PlanningBoardDbContext> options) : DbContext(options)
    {
        public DbSet<Board> Boards { get; set; }
    }
}
