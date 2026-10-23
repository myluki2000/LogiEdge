using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemSchemaLookupColumn
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("ItemSchema")]
        public required Guid ItemSchemaId { get; set; }
        public ItemSchema ItemSchema { get; set; } = null!;

        /// <summary>
        /// Name of the lookup column, e.g. "ArticleName". Displayed like a regular item property.
        /// Must be unique per schema and must not collide with a base or additional property name.
        /// </summary>
        [MaxLength(64)]
        public required string Name { get; set; } = string.Empty;

        /// <summary>
        /// Which item property is used as the key for lookups in this column.
        /// </summary>
        public LookupKeySource KeySource { get; set; } = LookupKeySource.ItemNumber;

        [MaxLength(256)]
        public string? Description { get; set; }

        public List<ItemSchemaLookupEntry> Entries { get; set; } = [];
    }

    /// <summary>
    /// Determines which item property is used as the lookup key for a column.
    /// Currently only ItemNumber is supported; further sources can be added without a schema change.
    /// </summary>
    public enum LookupKeySource
    {
        ItemNumber,
    }

    public class ItemSchemaLookupEntry
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Column")]
        public required Guid ColumnId { get; set; }
        public ItemSchemaLookupColumn Column { get; set; } = null!;

        /// <summary>
        /// The key to look up with (currently: the item's ItemNumber). Unique per column.
        /// </summary>
        [MaxLength(64)]
        public required string Key { get; set; } = string.Empty;

        [MaxLength(512)]
        public required string Value { get; set; } = string.Empty;
    }
}
