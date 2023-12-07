using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    [Owned]
    public class ItemId
    {
        public string CustomerPrefix { get; set; }
        public int Id { get; set; }

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
