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
            public string StorageLocation { get; set; } = "";
            public Dictionary<string, string> AdditionalProperties { get; init; } = new();
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

                int itemCount = 0;

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
                            catch(InvalidCastException)
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
                            if (!value.IsText)
                                item.StorageLocation = "?";
                            else
                                item.StorageLocation = value.GetText();
                            break;
                        case InventoryFileMatchingOptions.ColumnType.OTHER:
                            item.AdditionalProperties[columnName] = value.GetText();
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
