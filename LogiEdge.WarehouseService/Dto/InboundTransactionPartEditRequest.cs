using System;
using System.Collections.Generic;

namespace LogiEdge.WarehouseService.Dto
{
    /// <summary>
    /// The full desired state of a booked inbound part, as edited. Lines are matched to the current
    /// draft lines by DraftItemId (null = new line); current lines missing from the list are removed.
    /// </summary>
    public class InboundTransactionPartEditRequest
    {
        public required List<InboundDraftItemEdit> DraftItems { get; set; }

        public class InboundDraftItemEdit
        {
            /// <summary>The id of the existing draft line this edit applies to. Null for new lines.</summary>
            public Guid? DraftItemId { get; set; }
            public required string ItemNumber { get; set; }
            public required Guid CustomerId { get; set; }
            public required Guid ItemSchemaId { get; set; }
            public required Guid WarehouseId { get; set; }
            public required string Location { get; set; }
            /// <summary>Accepted for shape symmetry with the draft DTO; has no effect (booking creates one item per line).</summary>
            public required int Count { get; set; }
            public required bool IsQuarantined { get; set; }
            public required string Comments { get; set; }
            public required List<ItemAdditionalPropertyDto> AdditionalProperties { get; set; }
        }
    }
}
