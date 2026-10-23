using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.ItemsTable;

namespace LogiEdge.Test.WarehouseService
{
    public class LookupValueResolverTest
    {
        private static ItemSchemaLookupColumn Column(Guid schemaId, string name) => new()
        {
            Id = Guid.NewGuid(),
            ItemSchemaId = schemaId,
            Name = name,
        };

        private static ItemSchemaLookupEntry Entry(string key, string value, ItemSchemaLookupColumn column) => new()
        {
            ColumnId = column.Id,
            Column = column,
            Key = key,
            Value = value,
        };

        private static Item CreateItem(Guid schemaId, string itemNumber) => new()
        {
            Id = Guid.NewGuid(),
            ItemSchemaId = schemaId,
            CustomerId = Guid.NewGuid(),
            ItemNumber = itemNumber,
            Comments = [],
            ItemStates = [],
        };

        // --- Group ---

        [Fact]
        public void Group_MultipleSchemasAndColumns_BuildsNestedStructure()
        {
            Guid schemaA = Guid.NewGuid();
            Guid schemaB = Guid.NewGuid();
            List<ItemSchemaLookupEntry> entries =
            [
                Entry("1140192", "Casing, Color: Black", Column(schemaA, "ArticleName")),
                Entry("1140193", "Casing, Color: Red", Column(schemaA, "ArticleName")),
                Entry("1140192", "ACME GmbH", Column(schemaA, "Supplier")),
                Entry("9000001", "Bolt M6", Column(schemaB, "ArticleName")),
            ];

            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped =
                LookupValueResolver.Group(entries);

            Assert.Equal(2, grouped.Count);

            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> schemaAColumns = grouped[schemaA];
            Assert.Equal(2, schemaAColumns.Count);
            Assert.Equal("Casing, Color: Black", schemaAColumns["ArticleName"]["1140192"]);
            Assert.Equal("Casing, Color: Red", schemaAColumns["ArticleName"]["1140193"]);
            Assert.Equal("ACME GmbH", schemaAColumns["Supplier"]["1140192"]);

            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> schemaBColumns = grouped[schemaB];
            Assert.Single(schemaBColumns);
            Assert.Equal("Bolt M6", schemaBColumns["ArticleName"]["9000001"]);
        }

        [Fact]
        public void Group_NoEntries_ReturnsEmptyResult()
        {
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped =
                LookupValueResolver.Group([]);

            Assert.Empty(grouped);
        }

        // --- ResolveForItem ---

        [Fact]
        public void ResolveForItem_MatchingItemNumber_ReturnsValuesOfAllMatchingColumns()
        {
            Guid schemaId = Guid.NewGuid();
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped = LookupValueResolver.Group(
            [
                Entry("1140192", "Casing, Color: Black", Column(schemaId, "ArticleName")),
                Entry("1140192", "ACME GmbH", Column(schemaId, "Supplier")),
            ]);

            using Item item = CreateItem(schemaId, "1140192");

            Dictionary<string, string> result = LookupValueResolver.ResolveForItem(item, grouped);

            Assert.Equal(2, result.Count);
            Assert.Equal("Casing, Color: Black", result["ArticleName"]);
            Assert.Equal("ACME GmbH", result["Supplier"]);
        }

        [Fact]
        public void ResolveForItem_NoMatchingKey_ReturnsEmptyResult()
        {
            Guid schemaId = Guid.NewGuid();
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped = LookupValueResolver.Group(
            [
                Entry("1140192", "Casing, Color: Black", Column(schemaId, "ArticleName")),
            ]);

            using Item item = CreateItem(schemaId, "9999999");

            Assert.Empty(LookupValueResolver.ResolveForItem(item, grouped));
        }

        [Fact]
        public void ResolveForItem_SchemaNotInGroupedData_ReturnsEmptyResult()
        {
            Guid otherSchemaId = Guid.NewGuid();
            IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> grouped = LookupValueResolver.Group(
            [
                Entry("1140192", "Casing, Color: Black", Column(otherSchemaId, "ArticleName")),
            ]);

            using Item item = CreateItem(Guid.NewGuid(), "1140192");

            Assert.Empty(LookupValueResolver.ResolveForItem(item, grouped));
        }

        // --- ClassifyFilterParameters ---

        [Fact]
        public void ClassifyFilterParameters_MixedSchemas_RoutesLookupColumnsToLookupFiltersAndEverythingElseToSql()
        {
            Guid schemaAId = Guid.NewGuid();
            Guid schemaBId = Guid.NewGuid();
            List<ItemSchema> schemas =
            [
                new ItemSchema
                {
                    Id = schemaAId,
                    Name = "Customer A — Spare Parts",
                    AdditionalProperties =
                    [
                        new ItemSchemaProperty
                        {
                            Name = "Color",
                            Type = ItemSchemaProperty.SupportedTypes.String,
                        },
                    ],
                    LookupColumns = [Column(schemaAId, "ArticleName")],
                },
                new ItemSchema
                {
                    Id = schemaBId,
                    Name = "Customer B — Packaging",
                    AdditionalProperties =
                    [
                        new ItemSchemaProperty
                        {
                            Name = "Weight",
                            Type = ItemSchemaProperty.SupportedTypes.Single,
                        },
                    ],
                    LookupColumns = [],
                },
            ];

            Dictionary<string, string> parameters = new()
            {
                ["Color"] = "red",
                ["ArticleName"] = "Casing",
                ["ItemNumber"] = "123",
            };

            (IReadOnlyDictionary<string, string> sqlParameters, IReadOnlyDictionary<string, string> lookupFilters) =
                LookupValueResolver.ClassifyFilterParameters(schemas, parameters);

            Assert.Single(lookupFilters);
            Assert.Equal("Casing", lookupFilters["ArticleName"]);

            Assert.Equal(2, sqlParameters.Count);
            Assert.Equal("red", sqlParameters["Color"]);
            Assert.Equal("123", sqlParameters["ItemNumber"]);

            List<string> allOutputKeys = sqlParameters.Keys.Concat(lookupFilters.Keys).ToList();
            Assert.Equal(parameters.Count, allOutputKeys.Count);
            Assert.Equal(parameters.Keys.OrderBy(x => x), allOutputKeys.OrderBy(x => x));
        }
    }
}
