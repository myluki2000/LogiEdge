using LogiEdge.CustomerService.Data;
using LogiEdge.CustomerService.Dto;
using LogiEdge.CustomerService.Persistence;
using LogiEdge.CustomerService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LogiEdge.Test.CustomerService
{
    public class CustomerManagementServiceTest(DatabaseFixture dbFixture) : IClassFixture<DatabaseFixture>
    {
        private readonly CustomerManagementService _service = new(dbFixture.CustomerDbContextFactory);

        [Fact]
        public async Task CreateCustomer_WithBillingAddress_CreatesCustomerAndBillingRow()
        {
            AddressDto billing = new()
            {
                Label = "HQ",
                Street = "Main Street",
                HouseNumber = "12",
                ZipCode = "10115",
                City = "Berlin",
                Country = "Germany",
            };

            CustomerDto customer = await _service.CreateCustomer("Judo Trading Co. GmbH", "Judo Trading", "JT", billingAddress: billing);

            Assert.Equal("Judo Trading Co. GmbH", customer.CanonicalName);
            Assert.Equal("Judo Trading", customer.ShortName);
            Assert.Equal("JT", customer.Abbreviation);
            Assert.NotNull(customer.BillingAddress);
            Assert.Equal("Main Street", customer.BillingAddress!.Street);
            Assert.Equal("12", customer.BillingAddress.HouseNumber);
            Assert.Empty(customer.DeliveryAddresses);

            // the billing address row must carry the billing FK only (one role per row)
            using CustomerDbContext ctx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            Address row = ctx.Addresses.Single(x => x.Id == customer.BillingAddress.Id);
            Assert.Equal(customer.Id, row.BillingCustomerId);
            Assert.Null(row.DeliveryCustomerId);
        }

        [Fact]
        public async Task UpdateCustomer_RenamesBothNames_KeepsAbbreviation()
        {
            CustomerDto customer = await _service.CreateCustomer("Old Canonical Name", "Old Short Name", "UPD");

            CustomerDto? updated = await _service.UpdateCustomerAsync(customer.Id, "New Canonical Name", "New Short Name");

            Assert.NotNull(updated);
            Assert.Equal("New Canonical Name", updated!.CanonicalName);
            Assert.Equal("New Short Name", updated.ShortName);
            Assert.Equal("UPD", updated.Abbreviation);

            // persisted to the database, not just returned
            CustomerDto? reloaded = await _service.GetCustomerByIdAsync(customer.Id);
            Assert.Equal("New Canonical Name", reloaded!.CanonicalName);
            Assert.Equal("New Short Name", reloaded.ShortName);
        }

        [Fact]
        public async Task AbbreviationUniqueness_IsEnforcedByTheDatabase()
        {
            await _service.CreateCustomer("First Co.", "First", "UNIQ");

            // bypass the service-level check so the DB unique index on Customers.Abbreviation is what rejects the insert
            using CustomerDbContext ctx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            ctx.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                Abbreviation = "UNIQ",
                CanonicalName = "Second Co.",
                ShortName = "Second",
            });

            DbUpdateException ex = await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            PostgresException pgEx = Assert.IsType<PostgresException>(ex.InnerException);
            Assert.Equal("23505", pgEx.SqlState); // unique_violation
        }

        [Fact]
        public async Task DeliveryAddresses_AtMostOneDefaultPerCustomer()
        {
            CustomerDto customer = await _service.CreateCustomer("Delivery Co.", "Delivery", "DFA");

            CustomerDto afterFirst = await _service.AddDeliveryAddressAsync(customer.Id, new AddressDto
            {
                Street = "Street One",
                ZipCode = "10115",
                City = "Berlin",
                Country = "Germany",
                IsDefaultDeliveryAddress = true,
            });
            Assert.Single(afterFirst.DeliveryAddresses);
            Guid firstId = afterFirst.DeliveryAddresses[0].Id;
            Assert.True(afterFirst.DeliveryAddresses[0].IsDefaultDeliveryAddress);

            CustomerDto afterSecond = await _service.AddDeliveryAddressAsync(customer.Id, new AddressDto
            {
                Street = "Street Two",
                ZipCode = "20095",
                City = "Hamburg",
                Country = "Germany",
                IsDefaultDeliveryAddress = true,
            });
            Assert.Equal(2, afterSecond.DeliveryAddresses.Count);

            // adding a second default clears the first one — exactly one default remains
            List<AddressDto> defaults = afterSecond.DeliveryAddresses.Where(x => x.IsDefaultDeliveryAddress).ToList();
            Assert.Single(defaults);
            Assert.NotEqual(firstId, defaults[0].Id);

            // switching the default back to the first address works
            CustomerDto? switched = await _service.SetDefaultDeliveryAddressAsync(customer.Id, firstId);
            List<AddressDto> switchedDefaults = switched!.DeliveryAddresses.Where(x => x.IsDefaultDeliveryAddress).ToList();
            Assert.Single(switchedDefaults);
            Assert.Equal(firstId, switchedDefaults[0].Id);

            // passing null clears all defaults
            CustomerDto? cleared = await _service.SetDefaultDeliveryAddressAsync(customer.Id, null);
            Assert.Empty(cleared!.DeliveryAddresses.Where(x => x.IsDefaultDeliveryAddress));
        }

        [Fact]
        public async Task SetBillingAddress_ReplacesInPlace_NullClears()
        {
            CustomerDto customer = await _service.CreateCustomer("Billing Co.", "Billing", "BILL");
            Assert.Null(customer.BillingAddress);

            CustomerDto withFirst = await _service.SetBillingAddressAsync(customer.Id, new AddressDto
            {
                Label = "HQ",
                Street = "Main Street",
                HouseNumber = "1",
                ZipCode = "10115",
                City = "Berlin",
                Country = "Germany",
            });
            Guid firstId = withFirst.BillingAddress!.Id;

            CustomerDto withSecond = await _service.SetBillingAddressAsync(customer.Id, new AddressDto
            {
                Label = "HQ 2",
                Street = "Other Street",
                HouseNumber = "2",
                ZipCode = "20095",
                City = "Hamburg",
                Country = "Germany",
            });

            // replaced in place: same row, updated fields
            Assert.Equal(firstId, withSecond.BillingAddress!.Id);
            Assert.Equal("Other Street", withSecond.BillingAddress.Street);
            Assert.Equal("HQ 2", withSecond.BillingAddress.Label);

            CustomerDto? cleared = await _service.SetBillingAddressAsync(customer.Id, null);
            Assert.Null(cleared!.BillingAddress);

            // the row is removed from the database
            using CustomerDbContext ctx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            Assert.Null(ctx.Addresses.Find(firstId));
        }

        [Fact]
        public async Task RemoveDeliveryAddress_RemovesTheRow()
        {
            CustomerDto customer = await _service.CreateCustomer("Remove Co.", "Remove", "RMD");
            CustomerDto withAddress = await _service.AddDeliveryAddressAsync(customer.Id, new AddressDto
            {
                Street = "Street One",
                ZipCode = "10115",
                City = "Berlin",
                Country = "Germany",
                IsDefaultDeliveryAddress = true,
            });
            Guid addressId = withAddress.DeliveryAddresses[0].Id;

            CustomerDto? afterRemoval = await _service.RemoveDeliveryAddressAsync(addressId);

            Assert.NotNull(afterRemoval);
            Assert.Empty(afterRemoval!.DeliveryAddresses);

            using CustomerDbContext ctx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            Assert.Null(ctx.Addresses.Find(addressId));
        }
    }
}
