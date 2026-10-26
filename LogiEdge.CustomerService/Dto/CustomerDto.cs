using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.CustomerService.Dto
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public required string Abbreviation { get; set; }
        public required string CanonicalName { get; set; }
        public required string ShortName { get; set; }
        public AddressDto? BillingAddress { get; set; }
        public List<AddressDto> DeliveryAddresses { get; set; } = [];
    }
}
