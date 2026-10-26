using System;

namespace LogiEdge.CustomerService.Dto
{
    public class AddressDto
    {
        public Guid Id { get; set; }
        public string? Label { get; set; }
        public required string Street { get; set; }
        public string? HouseNumber { get; set; }
        public required string ZipCode { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }
        public bool IsDefaultDeliveryAddress { get; set; }
    }
}
