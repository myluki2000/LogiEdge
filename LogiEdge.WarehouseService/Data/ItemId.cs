using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public struct ItemId
    {
        public string CustomerPrefix;
        public int Id;

        public bool Equals(ItemId other)
        {
            return CustomerPrefix == other.CustomerPrefix && Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is ItemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomerPrefix, Id);
        }

        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }
    }
}
