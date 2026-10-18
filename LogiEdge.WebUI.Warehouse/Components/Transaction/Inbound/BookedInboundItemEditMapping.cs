using LogiEdge.WarehouseService.Dto;

namespace LogiEdge.WebUI.Warehouse.Components.Transaction.Inbound
{
    /// <summary>
    /// Mapping between the read DTO of a booked inbound part and the edit request DTO, shared by
    /// the single-item modal and the row delete action (both must send the full desired state).
    /// </summary>
    internal static class BookedInboundItemEditMapping
    {
        public static InboundTransactionPartEditRequest.InboundDraftItemEdit ToEditLine(this InboundTransactionPartDto.InboundDraftItemDto d) => new()
        {
            DraftItemId = d.Id,
            ItemNumber = d.ItemNumber,
            CustomerId = d.CustomerId,
            ItemSchemaId = d.ItemSchemaId,
            WarehouseId = d.WarehouseId,
            Location = d.Location,
            Count = d.Count,
            IsQuarantined = d.IsQuarantined,
            Comments = d.Comments.FirstOrDefault()?.Text ?? string.Empty,
            AdditionalProperties = d.AdditionalProperties.ToList(),
        };
    }
}
