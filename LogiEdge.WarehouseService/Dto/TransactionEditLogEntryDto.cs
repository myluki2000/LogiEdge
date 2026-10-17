using LogiEdge.WarehouseService.Data.Transactions;

namespace LogiEdge.WarehouseService.Dto
{
    public class TransactionEditLogEntryDto
    {
        public Guid Id { get; set; }
        public TransactionEditPartType? PartType { get; set; }
        public required DateTime EditedDate { get; set; }
        public required Guid EditedByUserId { get; set; }
        public required string ChangeSummary { get; set; }
        /// <summary>Raw JSON of the old → new change set.</summary>
        public required string ChangesJson { get; set; }
    }
}
