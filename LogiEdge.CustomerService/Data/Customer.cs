using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;

namespace LogiEdge.CustomerService.Data
{
    public class Customer : IComparable
    {
        [Key] public Guid Id { get; set; }
        public required string Abbreviation { get; set; }
        [MaxLength(256)] public required string CanonicalName { get; set; }
        [MaxLength(256)] public required string ShortName { get; set; }

        public Address? BillingAddress { get; set; }
        public List<Address> DeliveryAddresses { get; set; } = [];

        public int CompareTo(object? other)
        {
            if (other is not Customer otherCustomer)
                return 1;

            return string.Compare(CanonicalName, otherCustomer.CanonicalName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
