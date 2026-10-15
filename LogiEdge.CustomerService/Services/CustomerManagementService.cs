using LogiEdge.CustomerService.Data;
using LogiEdge.CustomerService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Dto;

namespace LogiEdge.CustomerService.Services
{
    public class CustomerManagementService(IDbContextFactory<CustomerDbContext> dbContextFactory)
    {
        public List<CustomerDto> GetCustomers()
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            
            return dbContext.Customers.Select(CustomerToDto).ToList();
        }

        public async Task<List<CustomerDto>> GetCustomersAsync()
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return dbContext.Customers.Select(CustomerToDto).ToList();
        }

        public async Task<CustomerDto?> GetCustomerByAbbreviation(string abbreviation)
        {
            abbreviation = abbreviation.ToUpper();
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return dbContext.Customers.Select(CustomerToDto).FirstOrDefault(x => x.Abbreviation == abbreviation);
        }

        public CustomerDto? GetCustomerById(Guid id)
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            return dbContext.Customers.Select(CustomerToDto).FirstOrDefault(x => x.Id == id);
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            return dbContext.Customers.Select(CustomerToDto).FirstOrDefault(x => x.Id == id);
        }

        public async Task<CustomerDto> CreateCustomer(string name, string abbreviation)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            EntityEntry<Customer> entity = dbContext.Customers.Add(new Customer()
            {
                Name = name,
                Abbreviation = abbreviation,
            });
            return await dbContext.SaveChangesAsync().ContinueWith(t => CustomerToDto(entity.Entity));
        }

        private CustomerDto CustomerToDto(Customer c)
        {
            return new CustomerDto()
            {
                Id = c.Id,
                Name = c.Name,
                Abbreviation = c.Abbreviation,
            };
        }
    }
}
