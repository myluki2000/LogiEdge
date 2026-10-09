using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace LogiEdge.WarehouseService.Dto
{
    public class InboundTransactionPartDto : InventoryTransactionPartDto
    {
        public List<InboundDraftItemDto> DraftItems { get; set; } = [];

        public class InboundDraftItemDto
        {
            public Guid Id { get; set; }
            public Guid ItemSchemaId { get; set; }
            public Guid CustomerId { get; set; }
            public Guid WarehouseId { get; set; }
            public string ItemNumber { get; set; } = string.Empty;
            public List<ItemAdditionalPropertyDto> AdditionalProperties { get; set; } = [];
            public List<CommentDto> Comments { get; set; } = [];
            public string Location { get; set; } = string.Empty;
            public int Count { get; set; }
            public bool IsQuarantined { get; set; }
        }
    }
}
