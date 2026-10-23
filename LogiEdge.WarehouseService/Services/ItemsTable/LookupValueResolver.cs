using System;
using System.Collections.Generic;
using System.Linq;
using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    /// <summary>
    /// Pure in-memory helpers for the item schema lookup columns: grouping loaded entries, resolving the values of a
    /// single item and classifying filter parameters into their SQL-pushable and in-memory parts. Deliberately free of
    /// DI and database access so it can be unit tested without a context.
    /// </summary>
    public static class LookupValueResolver
    {
        /// <summary>
        /// Groups lookup entries by schema, column and key: schemaId → (columnName → (key → value)).
        /// </summary>
        public static IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> Group(
            IEnumerable<ItemSchemaLookupEntry> entries)
        {
            return entries
                .GroupBy(e => e.Column.ItemSchemaId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>)g.GroupBy(e => e.Column.Name)
                        .ToDictionary(
                            cg => cg.Key,
                            cg => (IReadOnlyDictionary<string, string>)cg.ToDictionary(e => e.Key, e => e.Value)));
        }

        /// <summary>
        /// Resolves the lookup values for a single item: for each of its schema's lookup columns whose key equals the
        /// item's ItemNumber (v1: the only supported key source), maps column name → value. Empty when the schema has
        /// no lookup data or nothing matches.
        /// </summary>
        public static Dictionary<string, string> ResolveForItem(
            Item item,
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped)
        {
            if (!grouped.TryGetValue(item.ItemSchemaId, out IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? columns))
                return [];

            Dictionary<string, string> result = [];
            foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> column in columns)
            {
                if (column.Value.TryGetValue(item.ItemNumber, out string? value))
                    result[column.Key] = value;
            }

            return result;
        }

        /// <summary>
        /// Splits additional-property filter parameters: names that are lookup columns on any of the given schemas go to
        /// lookupFilters (resolved and filtered in memory), everything else to sqlParameters. Every input entry appears
        /// in exactly one of the two outputs.
        /// </summary>
        public static (IReadOnlyDictionary<string, string> sqlParameters, IReadOnlyDictionary<string, string> lookupFilters) ClassifyFilterParameters(
            IReadOnlyCollection<ItemSchema> schemas,
            IReadOnlyDictionary<string, string> additionalPropertyParameters)
        {
            HashSet<string> lookupColumnNames = schemas
                .SelectMany(sch => sch.LookupColumns)
                .Select(col => col.Name)
                .ToHashSet();

            Dictionary<string, string> sqlParameters = [];
            Dictionary<string, string> lookupFilters = [];
            foreach (KeyValuePair<string, string> parameter in additionalPropertyParameters)
            {
                if (lookupColumnNames.Contains(parameter.Key))
                    lookupFilters[parameter.Key] = parameter.Value;
                else
                    sqlParameters[parameter.Key] = parameter.Value;
            }

            return (sqlParameters, lookupFilters);
        }
    }
}
