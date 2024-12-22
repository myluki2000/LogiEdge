using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.CustomerService.Data
{
    public class Customer : IComparable
    {
        [Key] public Guid Id { get; set; }
        public required string Abbreviation { get; set; }
        public required string Name { get; set; }
        public int CompareTo(object? other)
        {
            if (other is not Customer otherCustomer)
                return 1;

            return string.Compare(Name, otherCustomer.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
