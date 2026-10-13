using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public class WarehouseBaseInfoDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
