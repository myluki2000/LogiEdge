using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemPropertyInfoDto
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsDisplayable { get; set; }
    }
}
