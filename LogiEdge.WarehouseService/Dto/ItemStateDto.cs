using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemStateDto
    {
        public Guid Id { get; set; }
        public required DateTime Date { get; set; }
        public required string Location { get; set; }
        public required bool IsQuarantined { get; set; }
        public required Guid WarehouseId { get; set; }
        public required Warehouse Warehouse { get; set; }

        /// <summary>
        /// The ID of the transaction which created this state, if any.
        /// </summary>
        public Guid? RelatedTransactionId { get; set; }
    }
}
