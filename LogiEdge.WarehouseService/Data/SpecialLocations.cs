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
        /// Special location string used when an item is in the common arrival area after having been unloaded
        /// but not having been put into a set storage location yet.
        /// </summary>
        public const string ARRIVAL_AREA = "__ARRIVAL_AREA";
        /// <summary>
        /// Special location string used when an item is in the common loading area where items are put to soon
        /// be loaded onto a truck (or similar) and leave the warehouse.
        /// </summary>
        public const string LOADING_AREA = "__LOADING_AREA";
    }
}
