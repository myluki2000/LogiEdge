using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public static class SpecialLocations
    {
        /// <summary>
        /// Special location string used for items which have not actually arrived in the warehouse yet but
        /// should still be saved in the system.
        /// </summary>
        public const string PRE_ARRIVAL = "__PRE_ARRIVAL";
        /// <summary>
        /// Special location string used when an item is in the common arrival area after having been unloaded
        /// but not having been put into a set storage location yet.
        /// </summary>
        public const string ARRIVAL_AREA = "__ARRIVAL_AREA";
        /// <summary>
        /// Special location string used when an item is in the common loading area where items are put to soon
        /// be loaded onto a truck (or similar) and leave the warehouse.
        /// </summary>
        public const string LOADING_AREA = "__LOADING_AREA";
        /// <summary>
        /// Special location string used for items which have shipped from the warehouse (and are thus no longer
        /// part of the warehouse's inventory)
        /// </summary>
        public const string SHIPPED = "__SHIPPED";
    }
}
