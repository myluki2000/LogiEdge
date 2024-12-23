using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.ExcelImporterService.Data
{
    public class ExcelImporterConfig
    {
        public required string FolderPath { get; init; }
        public required int DayOffset { get; init; }
        public required List<InventoryItemSchema> InventoryItemSchemas { get; init; }
        public required List<InventoryFileMatchingOptions> InventoryFileMatchingOptions { get; init; }
    }

    public class InventoryItemSchema
    {
        public required string Name { get; init; }
        public required List<string> AdditionalProperties { get; init; }
        public required List<string> AdditionalPropertiesTypes { get; init; }
    }

    public class InventoryFileMatchingOptions
    {
        public required string CustomerName { get; init; }
        public required string WarehouseName { get; init; }
        public required string FileName { get; init; }
        public required string WorkSheet { get; init; }
        public required string UseSchema { get; init; }
        public required Dictionary<string, ColumnType> Columns { get; init; }
        public required List<string> IdentifyingColumns { get; init; }

        public enum ColumnType
        {
            ITEM_NUMBER,
            ENTRY_DATE,
            ITEM_COUNT,
            STORAGE_LOCATION,
            OTHER,
            EXIT_DATE,
        }
    }
}
