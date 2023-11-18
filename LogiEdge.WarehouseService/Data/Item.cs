using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Data
{
    public class Item
    {
        [Key]
        public ItemId Id { get; set; }

        /// <summary>
        /// Arrival date & time of this item at the warehouse. Note that arrival of an item does not
        /// imply it also being stored in a specific location in the warehouse at the same time. To
        /// get information about time of storage <see cref="StoredOn"/>
        /// </summary>
        public DateTime? ArrivedOn { get; set; }

        /// <summary>
        /// Storage date & time of this item in the warehouse. Note that an item is only counted as
        /// "stored" if it has been placed in a specific (designated) location within the warehouse.
        /// </summary>
        public DateTime? StoredOn { get; set; }

        /// <summary>
        /// Retrieval date & time of this item in the warehouse. Note that an item is counted as
        /// "retrieved" if it has been removed from (semi-)permanent storage in the warehouse to be
        /// shipped soon. For information about the shipping date <see cref="ShippedOn"/>
        /// </summary>
        public DateTime? RetrievedOn { get; set; }

        /// <summary>
        /// Shipping date & time from the warehouse of this item. Note that an item is counted as
        /// "shipped" if it has left the warehouse and moved to a vehicle to be transported to a
        /// new destination.
        /// </summary>
        public DateTime? ShippedOn { get; set; }
    }
}
