using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public abstract class InventoryTransaction
    {
        [Key]
        public Guid Id { get; set; }
        public required DateTime Date { get; set; }
        public required Guid CreatedByUserId { get; set; }
        public required string HandledByWorker { get; set; }
        public string Comments { get; set; } = string.Empty;
        public List<FileAttachment> Attachments { get; set; } = [];
    }
}
