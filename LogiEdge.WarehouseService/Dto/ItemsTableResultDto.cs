using System.Collections.Generic;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemsTableResultDto
    {
        public required List<WarehouseItemDto> Items { get; set; }
        public required List<ItemPropertyInfoDto> ItemProperties { get; set; }
    }
}
