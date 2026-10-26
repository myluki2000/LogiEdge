using LogiEdge.CustomerService.Data;
using LogiEdge.CustomerService.Dto;
using LogiEdge.CustomerService.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogiEdge.CustomerService.Services
{
    public class CustomerManagementService(IDbContextFactory<CustomerDbContext> dbContextFactory)
    {
        public List<CustomerDto> GetCustomers()
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();

            return dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .AsEnumerable()
                .OrderBy(x => x.CanonicalName, StringComparer.CurrentCultureIgnoreCase)
                .Select(CustomerToDto)
                .ToList();
        }

        public async Task<List<CustomerDto>> GetCustomersAsync()
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

            return dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .AsEnumerable()
                .OrderBy(x => x.CanonicalName, StringComparer.CurrentCultureIgnoreCase)
                .Select(CustomerToDto)
                .ToList();
        }

        public async Task<CustomerDto?> GetCustomerByAbbreviation(string abbreviation)
        {
            abbreviation = abbreviation.ToUpper();
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Abbreviation == abbreviation);

            return customer == null ? null : CustomerToDto(customer);
        }

        public CustomerDto? GetCustomerById(Guid id)
        {
            using CustomerDbContext dbContext = dbContextFactory.CreateDbContext();
            Customer? customer = dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefault(x => x.Id == id);

            return customer == null ? null : CustomerToDto(customer);
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == id);

            return customer == null ? null : CustomerToDto(customer);
        }

        public async Task<CustomerDto?> UpdateCustomerAsync(Guid id, string canonicalName, string shortName)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .AsTracking()
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (customer == null)
            {
                return null;
            }

            customer.CanonicalName = canonicalName;
            customer.ShortName = shortName;
            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        public async Task<CustomerDto> CreateCustomer(string canonicalName, string shortName, string abbreviation, AddressDto? billingAddress = null)
        {
            if (await GetCustomerByAbbreviation(abbreviation) != null)
            {
                throw new InvalidOperationException($"A customer with the abbreviation '{abbreviation.ToUpper()}' already exists.");
            }

            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer customer = new Customer()
            {
                CanonicalName = canonicalName,
                ShortName = shortName,
                Abbreviation = abbreviation.ToUpper(),
                BillingAddress = billingAddress == null ? null : AddressFromDto(billingAddress),
            };

            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        public async Task<CustomerDto?> SetBillingAddressAsync(Guid customerId, AddressDto? address)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .AsTracking()
                .Include(c => c.BillingAddress)
                .FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer == null)
            {
                return null;
            }

            if (address == null)
            {
                if (customer.BillingAddress != null)
                {
                    dbContext.Addresses.Remove(customer.BillingAddress);
                }
            }
            else if (customer.BillingAddress != null)
            {
                CopyAddressFields(customer.BillingAddress, address);
            }
            else
            {
                customer.BillingAddress = AddressFromDto(address);
            }

            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        public async Task<CustomerDto?> AddDeliveryAddressAsync(Guid customerId, AddressDto address)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .AsTracking()
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer == null)
            {
                return null;
            }

            if (address.IsDefaultDeliveryAddress)
            {
                ClearOtherDefaultDeliveryAddresses(customer, excludeAddressId: null);
            }

            customer.DeliveryAddresses.Add(AddressFromDto(address));
            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        public async Task<CustomerDto?> UpdateAddressAsync(Guid addressId, AddressDto address)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Address? existingAddress = await dbContext.Addresses.AsTracking().FirstOrDefaultAsync(x => x.Id == addressId);
            if (existingAddress == null)
            {
                return null;
            }

            Guid? customerId = existingAddress.BillingCustomerId ?? existingAddress.DeliveryCustomerId;
            Customer? customer = await dbContext.Customers
                .AsTracking()
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer == null)
            {
                return null;
            }

            CopyAddressFields(existingAddress, address);
            if (existingAddress.DeliveryCustomerId != null && address.IsDefaultDeliveryAddress)
            {
                ClearOtherDefaultDeliveryAddresses(customer, addressId);
            }

            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        public async Task<CustomerDto?> RemoveDeliveryAddressAsync(Guid addressId)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Address? address = await dbContext.Addresses.AsTracking().FirstOrDefaultAsync(x => x.Id == addressId);
            if (address == null)
            {
                return null;
            }

            Guid? customerId = address.BillingCustomerId ?? address.DeliveryCustomerId;
            dbContext.Addresses.Remove(address);
            await dbContext.SaveChangesAsync();

            Customer? customer = await dbContext.Customers
                .Include(c => c.BillingAddress)
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == customerId);
            return customer == null ? null : CustomerToDto(customer);
        }

        public async Task<CustomerDto?> SetDefaultDeliveryAddressAsync(Guid customerId, Guid? addressId)
        {
            await using CustomerDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
            Customer? customer = await dbContext.Customers
                .AsTracking()
                .Include(c => c.DeliveryAddresses)
                .FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer == null)
            {
                return null;
            }

            if (addressId != null && !customer.DeliveryAddresses.Any(x => x.Id == addressId.Value))
            {
                return null;
            }

            ClearOtherDefaultDeliveryAddresses(customer, addressId);
            if (addressId != null)
            {
                customer.DeliveryAddresses.First(x => x.Id == addressId.Value).IsDefaultDeliveryAddress = true;
            }

            await dbContext.SaveChangesAsync();
            return CustomerToDto(customer);
        }

        private static void ClearOtherDefaultDeliveryAddresses(Customer customer, Guid? excludeAddressId)
        {
            foreach (Address deliveryAddress in customer.DeliveryAddresses.Where(x => x.IsDefaultDeliveryAddress && x.Id != excludeAddressId))
            {
                deliveryAddress.IsDefaultDeliveryAddress = false;
            }
        }

        private static CustomerDto CustomerToDto(Customer customer)
        {
            return new CustomerDto()
            {
                Id = customer.Id,
                Abbreviation = customer.Abbreviation,
                CanonicalName = customer.CanonicalName,
                ShortName = customer.ShortName,
                BillingAddress = customer.BillingAddress == null ? null : AddressToDto(customer.BillingAddress),
                DeliveryAddresses = customer.DeliveryAddresses.Select(AddressToDto).ToList(),
            };
        }

        private static AddressDto AddressToDto(Address address)
        {
            return new AddressDto()
            {
                Id = address.Id,
                Label = address.Label,
                Street = address.Street,
                HouseNumber = address.HouseNumber,
                ZipCode = address.ZipCode,
                City = address.City,
                Country = address.Country,
                IsDefaultDeliveryAddress = address.IsDefaultDeliveryAddress,
            };
        }

        private static Address AddressFromDto(AddressDto address)
        {
            return new Address()
            {
                Label = address.Label,
                Street = address.Street,
                HouseNumber = address.HouseNumber,
                ZipCode = address.ZipCode,
                City = address.City,
                Country = address.Country,
                IsDefaultDeliveryAddress = address.IsDefaultDeliveryAddress,
            };
        }

        private static void CopyAddressFields(Address target, AddressDto source)
        {
            target.Label = source.Label;
            target.Street = source.Street;
            target.HouseNumber = source.HouseNumber;
            target.ZipCode = source.ZipCode;
            target.City = source.City;
            target.Country = source.Country;
            target.IsDefaultDeliveryAddress = source.IsDefaultDeliveryAddress;
        }
    }
}
