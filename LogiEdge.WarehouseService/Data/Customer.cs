using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public class Customer
    {
        public Guid Id { get; set; }
        public required string Abbreviation { get; set; }
    }
}
