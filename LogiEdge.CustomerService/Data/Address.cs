using System;
using System.ComponentModel.DataAnnotations;

namespace LogiEdge.CustomerService.Data
{
    public class Address
    {
        [Key] public Guid Id { get; set; }
        [MaxLength(128)] public string? Label { get; set; }
        [MaxLength(256)] public required string Street { get; set; }
        [MaxLength(32)] public string? HouseNumber { get; set; }
        [MaxLength(16)] public required string ZipCode { get; set; }
        [MaxLength(128)] public required string City { get; set; }
        [MaxLength(64)] public required string Country { get; set; }
        public bool IsDefaultDeliveryAddress { get; set; }

        public Guid? BillingCustomerId { get; set; }
        public Guid? DeliveryCustomerId { get; set; }
    }
}
