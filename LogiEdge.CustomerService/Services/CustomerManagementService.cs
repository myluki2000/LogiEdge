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
        public List<Customer> GetCustomers()
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            
            return dbContext.Customers.ToList();
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Customers.ToListAsync();
        }

        public Customer? GetCustomerByAbbreviation(string abbreviation)
        {
            abbreviation = abbreviation.ToUpper();

            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();

            return dbContext.Customers.FirstOrDefault(x => x.Abbreviation == abbreviation);
        }

        public async Task<Customer?> GetCustomerByAbbreviationAsync(string abbreviation)
        {
            abbreviation = abbreviation.ToUpper();
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Customers.FirstOrDefaultAsync(x => x.Abbreviation == abbreviation);
        }

        public Customer? GetCustomerById(Guid id)
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            return dbContext.Customers.Find(id);
        }

        public async Task<Customer?> GetCustomerByIdAsync(Guid id)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.Customers.FindAsync(id);
        }

        public Customer CreateCustomer(Customer customer)
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();

            EntityEntry<Customer> entity = dbContext.Customers.Add(customer);

            dbContext.SaveChanges();

            return entity.Entity;
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            EntityEntry<Customer> entity = dbContext.Customers.Add(customer);
            return await dbContext.SaveChangesAsync().ContinueWith(t => entity.Entity);
        }
    }
}
