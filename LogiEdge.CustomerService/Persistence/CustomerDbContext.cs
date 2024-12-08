using LogiEdge.CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.CustomerService.Persistence
{
    public class CustomerDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; init; }

        public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options) { }
    }
}
