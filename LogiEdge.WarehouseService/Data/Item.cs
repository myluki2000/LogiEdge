using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Data
{
    public class Item : IDisposable
    {
        [Key]
        public ItemId Id { get; set; }

        public JsonDocument? AdditionalProperties { get; set; }

        public string Comments { get; set; } = string.Empty;

        public required List<ItemState> ItemStates;

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }
    }
}
