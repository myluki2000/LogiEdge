using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemAdditionalPropertyDto
    {
        public required string Name { get; set; }
        public required object? Value { get; set; }
        public required ItemSchemaProperty.SupportedTypes Type { get; set; }
    }
}
