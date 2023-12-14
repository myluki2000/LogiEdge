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
    public class CustomerManagementService(IDbContextFactory<CustomerDbContext> dbContextFactory)
    {
        public IEnumerable<Customer> GetCustomers()
        {
            CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            
            return dbContext.Customers.ToList();
        }

        public Customer? GetCustomerByAbbreviation(string abbreviation)
        {
            abbreviation = abbreviation.ToUpper();

            CustomerDbContext context = dbContextFactory.CreateDbContext();

            return context.Customers.FirstOrDefault(x => x.Abbreviation == abbreviation);
        }

        public Customer CreateCustomer(Customer customer)
        {
            CustomerDbContext dbContext = dbContextFactory.CreateDbContext();

            EntityEntry<Customer> entity = dbContext.Customers.Add(customer);

            dbContext.SaveChanges();

            return entity.Entity;
        }
    }
}
