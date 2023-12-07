using LogiEdge.CustomerService.Data;
using LogiEdge.CustomerService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.CustomerService.Services
{
    public class CustomerManagementService
    {
        private readonly IDbContextFactory<CustomerDbContext> dbContextFactory;

        public CustomerManagementService(IDbContextFactory<CustomerDbContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        public IEnumerable<Customer> GetCustomers()
        {
            CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            
            return dbContext.Customers.ToList();
        }

        public Customer CreateCustomer(Customer customer)
        {
            CustomerDbContext dbContext = dbContextFactory.CreateDbContext();

            EntityEntry<Customer> entity = dbContext.Customers.Add(customer);

            return entity.Entity;
        }
    }
}
