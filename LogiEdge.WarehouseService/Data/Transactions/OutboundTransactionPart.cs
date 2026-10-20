using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Dto;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class OutboundTransactionPart : InventoryTransactionPart
    {
        /// <summary>
        /// If this transaction is in DRAFT state, this will contain the items which
        /// have been selected for outbound.
        /// </summary>
        public List<Item>? DraftSelectedItems { get; set; }
        /// <summary>
        /// If this transaction is in DRAFT state, this will contain items which are part
        /// of the transaction but for which the user hasn't yet selected a specific item
        /// from the warehouse.
        /// </summary>
        public List<Dictionary<string, string>>? DraftPlaceholderItems { get; set; }

        /// <summary>
        /// Maps this part to a DTO. The DraftSelectedItems navigation (including each item's ItemSchema, Customer,
        /// Comments and ItemStates) as well as NewItemStates must be loaded, e.g. via .Include().
        /// </summary>
        public OutboundTransactionPartDto ToDto()
        {
            return new OutboundTransactionPartDto()
            {
                TransactionId = this.TransactionId,
                // represent each selected item at its latest state (the state it is in before the shipment)
                DraftSelectedItems = this.DraftSelectedItems?
                    .Select(it => ItemAtStateDto.FromItem(it, it.ItemStates.OrderByDescending(st => st.Date).First()))
                    .ToList(),
                DraftPlaceholderItems = this.DraftPlaceholderItems,
                NewItemStates = this.NewItemStates,
            };
        }
    }
}
