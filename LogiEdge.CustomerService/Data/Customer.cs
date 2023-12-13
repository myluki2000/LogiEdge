using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.CustomerService.Data
{
    public class Customer
    {
        [Key]
        public required string Abbreviation { get; set; }
        public required string Name { get; set; }
    }
}
