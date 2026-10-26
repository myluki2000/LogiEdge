using LogiEdge.CustomerService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.CustomerService.Persistence
{
    /// <summary>
    /// Shared model configuration for the Customer/Address entities. Both CustomerDbContext and
    /// WarehouseDbContext map these entities (the latter transitively via items), so the
    /// relationship wiring must be identical in both models — configure it here, not per context.
    /// </summary>
    public static class CustomerModelConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.BillingAddress)
                .WithOne()
                .HasForeignKey<Address>(a => a.BillingCustomerId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.DeliveryAddresses)
                .WithOne()
                .HasForeignKey(a => a.DeliveryCustomerId);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Abbreviation)
                .IsUnique();

            modelBuilder.Entity<Address>()
                .HasCheckConstraint("CK_Addresses_SingleRole", "\"BillingCustomerId\" IS NULL OR \"DeliveryCustomerId\" IS NULL");
        }
    }
}
