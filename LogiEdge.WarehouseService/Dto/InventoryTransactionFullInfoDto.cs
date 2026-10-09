using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.Shared;

namespace LogiEdge.WarehouseService.Dto
{
    public class InventoryTransactionFullInfoDto : InventoryTransactionBaseInfoDto, IDocumentGeneratorInput
    {
        public required InboundTransactionPartDto? InboundTransactionPart { get; set; }
        public required OutboundTransactionPartDto? OutboundTransactionPart { get; set; }
        public required RelocationTransactionPartDto? RelocationTransactionPart { get; set; }
    }
}
