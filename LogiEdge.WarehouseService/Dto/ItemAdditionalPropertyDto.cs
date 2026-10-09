using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemAdditionalPropertyDto
    {
        public required string Name { get; set; }
        public required object? Value { get; set; }
    }
}
