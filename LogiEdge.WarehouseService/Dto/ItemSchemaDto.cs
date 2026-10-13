using System;
using System.Collections.Generic;
using System.Text;
using static LogiEdge.WarehouseService.Data.ItemSchemaProperty;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemSchemaDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        /// <summary>
        /// Additional properties this schema defines for items.
        /// </summary>
        public required List<ItemPropertyInfoDto> AdditionalProperties { get; set; }
        /// <summary>
        /// List of IDs of customers whose items use this schema.
        /// </summary>
        public required List<Guid> CustomerIds { get; set; }
    }
}
