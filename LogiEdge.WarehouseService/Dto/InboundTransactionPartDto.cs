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
            public required Guid Id { get; set; }
            /// <summary>
            /// The items created for this draft line when the parent transaction was booked — one item per
            /// count unit, in creation order. Empty while the parent transaction is still in DRAFT state.
            /// </summary>
            public required List<Guid> CreatedItemIds { get; set; } = [];
            public required Guid ItemSchemaId { get; set; }
            public required Guid CustomerId { get; set; }
            public required Guid WarehouseId { get; set; }
            public required string ItemNumber { get; set; }
            public required List<ItemAdditionalPropertyDto> AdditionalProperties { get; set; }
            public required List<CommentDto> Comments { get; set; }
            public required string Location { get; set; }
            public required int Count { get; set; }
            public required bool IsQuarantined { get; set; }
        }
    }
}
