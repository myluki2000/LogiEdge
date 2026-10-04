using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data.Transactions;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemState
    {
        public Guid Id { get; set; }

        public Guid ItemId { get; set; }

        public Item Item { get; set; } = null!;

        public required DateTime Date { get; set; }

        public required Guid WarehouseId { get; set; }

        [QueryFilterableProperty(nameof(WarehouseId))]
        [DisplayColumnProperty]
        public Warehouse Warehouse { get; set; } = null!;

        [QueryFilterableProperty]
        [DisplayColumnProperty]
        [MaxLength(32)]
        public required string Location { get; set; }

        [QueryFilterableProperty]
        [DisplayColumnProperty]
        public required bool IsQuarantined { get; set; }

        public Guid? RelatedTransactionId { get; set; }
        public InventoryTransaction? RelatedTransaction { get; set; }

        public bool IsInWarehouse => IsInWarehouseExpr.Compile().Invoke(this);

        public bool IsShipped => Location == SpecialLocations.SHIPPED;

        public static Expression<Func<ItemState, bool>> IsInWarehouseExpr => st =>
            st.Location != SpecialLocations.PRE_ARRIVAL && st.Location != SpecialLocations.SHIPPED;
    }
}
