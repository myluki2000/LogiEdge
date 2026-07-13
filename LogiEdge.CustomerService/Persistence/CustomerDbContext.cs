using LogiEdge.CustomerService.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.CustomerService.Persistence
{
    public class CustomerDbContext : LogiEdgeDbContext<CustomerDbContext>
    {
        public DbSet<Customer> Customers { get; init; }

        public CustomerDbContext(DbContextOptions<CustomerDbContext> options, IConfiguration configuration) : base(options, configuration) { }
    }
}
