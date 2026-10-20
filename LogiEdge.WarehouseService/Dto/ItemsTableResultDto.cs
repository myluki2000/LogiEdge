using System.Collections.Generic;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemsTableResultDto
    {
        public required List<ItemAtStateDto> Items { get; set; }
        public required List<ItemPropertyInfoDto> ItemProperties { get; set; }
    }
}
