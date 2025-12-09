using LogiEdge.ExcelImporterService.Data;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace LogiEdge.ExcelImporterService.Internal
{
    internal class InventoryFileMatcher
    {
        public string FolderPath { get; set; }
        public InventoryFileMatchingOptions MatchingOptions { get; set; }
        public DateTime FileDay { get; set; }

        public InventoryFileMatcher(string folderPath, DateTime day, InventoryFileMatchingOptions options)
        {
            FileDay = day;
            FolderPath = folderPath;
            MatchingOptions = options;
        }

        public class InventoryItem
        {
            public string ItemNumber { get; set; } = "";
            public DateTime EntryDate { get; set; }

            /// <summary>
            /// If the imported spreadsheet stores exit dates of items instead of just removing the row,
            /// this property can be used to store the exit date.
            /// </summary>
            public DateTime? ExitDate { get; set; } = null;
            public string StorageLocation { get; set; } = "";
            public string Comment { get; set; } = "";
            public Dictionary<string, string> AdditionalProperties { get; init; } = new();

            public bool InWarehouse => ExitDate == null;
        }

        public List<InventoryItem> Process()
        {
            using FileStream fs = File.OpenRead(Path.Combine(FolderPath, MatchingOptions.FileName));
            XLWorkbook wb = new XLWorkbook(fs);

            IXLWorksheet ws = wb.Worksheet(MatchingOptions.WorkSheet);

            if (!ws.AutoFilter.IsEnabled)
                throw new Exception("Worksheet does not have an autofilter.");

            IXLRange tableRange = ws.AutoFilter.Range;

            Dictionary<string, int> columnIndices = new();

            List<InventoryItem> items = new();

            foreach (string columnName in MatchingOptions.Columns.Keys)
            {
                bool found = false;
                for (int i = 1; i <= tableRange.ColumnCount(); i++)
                {
                    if (!tableRange.Row(1).Cell(i).Value.IsText)
                        continue;

                    string actualColumnName = tableRange.Row(1).Cell(i).Value.GetText();
                    if (actualColumnName.Trim() == columnName)
                    {
                        columnIndices.Add(columnName, i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception("Column listed in the matching options which could not be found in the auto filter table range.");
            }

            foreach (IXLRangeRow row in tableRange.Rows().Skip(1))
            {
                // item no. cell is empty, skip row
                IXLCell? cell = row.Cell(columnIndices[MatchingOptions.Columns.First(x => x.Value == InventoryFileMatchingOptions.ColumnType.ITEM_NUMBER).Key]);
                if (cell.IsEmpty() || (cell.Value.IsText && cell.Value.GetText().Trim() == ""))
                {
                    continue;
                }


                InventoryItem item = new();

                // if we don't have an item count column in the spreadsheet, assume that each row is exactly one item
                int itemCount = 1;

                foreach ((string columnName, InventoryFileMatchingOptions.ColumnType columnType) in MatchingOptions.Columns)
                {
                    int columnIndex = columnIndices[columnName];

                    XLCellValue value = row.Cell(columnIndex).Value;
                    switch (columnType)
                    {
                        case InventoryFileMatchingOptions.ColumnType.ITEM_NUMBER:
                            item.ItemNumber = value.IsNumber ? ((int)value.GetNumber()).ToString() : value.GetText();
                            break;
                        case InventoryFileMatchingOptions.ColumnType.ENTRY_DATE:
                            try
                            {
                                DateTime parsedDate = DateTime.SpecifyKind(
                                    (value.IsBlank || value.IsText && value.GetText().Trim() == "")
                                        ? DateTime.MinValue
                                        : value.GetDateTime(),
                                    DateTimeKind.Local);

                                if (parsedDate > FileDay)
                                {
                                    parsedDate = FileDay;
                                }

                                item.EntryDate = parsedDate.ToUniversalTime();
                            }
                            catch (InvalidCastException)
                            {
                                item.EntryDate = DateTime.MinValue.ToUniversalTime();
                            }

                            break;
                        case InventoryFileMatchingOptions.ColumnType.ITEM_COUNT:

                            if (value.IsNumber)
                            {
                                itemCount = (int)value.GetNumber();
                            }
                            else if (value.IsText)
                            {
                                string stringValue = value.GetText()
                                    .Replace("?", "")
                                    .Replace("GB", "")
                                    .Replace("Gibo", "", true, CultureInfo.InvariantCulture)
                                    .Trim();
                                itemCount = string.IsNullOrEmpty(stringValue) ? 0 : int.Parse(stringValue);
                            }
                            else
                            {
                                throw new Exception("Could not parse item count in Excel file.");
                            }

                            break;
                        case InventoryFileMatchingOptions.ColumnType.STORAGE_LOCATION:
                            item.StorageLocation = !value.IsText ? "?" : value.GetText();
                            break;
                        case InventoryFileMatchingOptions.ColumnType.OTHER:
                            if (value.IsText)
                                item.AdditionalProperties[columnName] = value.GetText();
                            else if (value.IsNumber)
                                item.AdditionalProperties[columnName] = value.GetNumber().ToString(CultureInfo.InvariantCulture);
                            else
                                throw new Exception("Could not parse additional property in Excel file.");
                            break;
                        case InventoryFileMatchingOptions.ColumnType.EXIT_DATE:
                            try
                            {
                                DateTime? parsedDate =
                                    (value.IsBlank || value.IsText && value.GetText().Trim() == "")
                                        ? null
                                        : DateTime.SpecifyKind(value.GetDateTime(), DateTimeKind.Local);

                                if (parsedDate.HasValue && parsedDate.Value > FileDay)
                                {
                                    parsedDate = FileDay;
                                }

                                item.ExitDate = parsedDate?.ToUniversalTime();
                            }
                            catch (InvalidCastException)
                            {
                                item.ExitDate = null;
                            }

                            break;
                        case InventoryFileMatchingOptions.ColumnType.COMMENT:
                            item.Comment = !value.IsText ? "" : value.GetText();
                            break;
                    }
                }

                for (int i = 0; i < itemCount; i++)
                {
                    items.Add(item);
                }
            }

            return items;
        }
    }
}
