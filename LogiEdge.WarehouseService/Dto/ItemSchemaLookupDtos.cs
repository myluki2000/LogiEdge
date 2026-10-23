using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    /// <summary>
    /// A lookup column of an item schema, as listed on the schema (with its entry count only).
    /// </summary>
    public class ItemSchemaLookupColumnDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public int EntryCount { get; set; }
    }

    /// <summary>
    /// A single key-value entry of a lookup column.
    /// </summary>
    public class ItemSchemaLookupEntryDto
    {
        public required Guid Id { get; set; }
        public required string Key { get; set; }
        public required string Value { get; set; }
    }

    /// <summary>
    /// A lookup column including all of its entries, for the schema detail page.
    /// </summary>
    public class ItemSchemaLookupColumnDetailDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required List<ItemSchemaLookupEntryDto> Entries { get; set; } = [];
    }
}
