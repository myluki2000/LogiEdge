using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Services.ItemsTable;

namespace LogiEdge.Test.WarehouseService
{
    public class ItemsTableSorterTest
    {
        private static ItemAtStateDto Dto(string itemNumber, string? color = null, string? articleName = null)
        {
            List<ItemAdditionalPropertyDto> additionalProperties = [];
            if (color is not null)
                additionalProperties.Add(new ItemAdditionalPropertyDto()
                {
                    Name = "Color",
                    Value = color,
                    Type = ItemSchemaProperty.SupportedTypes.String,
                });

            List<ItemAdditionalPropertyDto> lookupProperties = [];
            if (articleName is not null)
                lookupProperties.Add(new ItemAdditionalPropertyDto()
                {
                    Name = "ArticleName",
                    Value = articleName,
                    Type = ItemSchemaProperty.SupportedTypes.String,
                });

            Warehouse warehouse = new()
            {
                Id = Guid.NewGuid(),
                Name = "Main",
            };

            return new ItemAtStateDto()
            {
                Id = Guid.NewGuid(),
                ItemSchema = new ItemSchema()
                {
                    Id = Guid.NewGuid(),
                    Name = "Customer A — Spare Parts",
                    AdditionalProperties = [],
                },
                CustomerId = Guid.NewGuid(),
                ItemNumber = itemNumber,
                AdditionalProperties = additionalProperties,
                LookupProperties = lookupProperties,
                Comments = [],
                Date = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
                Location = "A 1-1",
                WarehouseId = warehouse.Id,
                Warehouse = warehouse,
                IsQuarantined = false,
            };
        }

        private static ItemsTableResultDto Result(params ItemAtStateDto[] items) => new()
        {
            Items = [.. items],
            ItemProperties = [],
        };

        private static ItemsTableSortParameters SortBy(string property, bool descending = false) => new()
        {
            GroupByProperties = [],
            SortByProperty = property,
            SortOrderDescending = descending,
        };

        [Fact]
        public void Sort_ByLookupColumnAscending_OrdersItemsByLookupValue()
        {
            ItemsTableResultDto result = Result(
                Dto("300", articleName: "Zeta"),
                Dto("100", articleName: "Alpha"),
                Dto("200", articleName: "Mid"));

            new ItemsTableSorter().Sort(result, SortBy("ArticleName"));

            Assert.Equal(new[] { "100", "200", "300" }, result.Items.Select(x => x.ItemNumber));
        }

        [Fact]
        public void Sort_ByLookupColumnDescending_ReversesOrder()
        {
            ItemsTableResultDto result = Result(
                Dto("100", articleName: "Alpha"),
                Dto("300", articleName: "Zeta"),
                Dto("200", articleName: "Mid"));

            new ItemsTableSorter().Sort(result, SortBy("ArticleName", descending: true));

            Assert.Equal(new[] { "300", "200", "100" }, result.Items.Select(x => x.ItemNumber));
        }

        [Fact]
        public void Sort_ByAdditionalProperty_OrdersItemsByPropertyValue()
        {
            ItemsTableResultDto result = Result(
                Dto("300", color: "Red"),
                Dto("100", color: "Green"),
                Dto("200", color: "Blue"));

            new ItemsTableSorter().Sort(result, SortBy("Color"));

            Assert.Equal(new[] { "200", "100", "300" }, result.Items.Select(x => x.ItemNumber));
        }

        [Fact]
        public void Sort_ByBaseProperty_ItemNumberStillWorks()
        {
            ItemsTableResultDto result = Result(
                Dto("300"),
                Dto("100"),
                Dto("200"));

            new ItemsTableSorter().Sort(result, SortBy(nameof(ItemAtStateDto.ItemNumber)));

            Assert.Equal(new[] { "100", "200", "300" }, result.Items.Select(x => x.ItemNumber));
        }

        [Fact]
        public void Sort_ByBasePropertyId_DescendingOrdersByGuid()
        {
            ItemsTableResultDto result = Result(
                Dto("1"),
                Dto("2"),
                Dto("3"));
            List<Guid> expected = result.Items.Select(x => x.Id).OrderByDescending(x => x).ToList();

            new ItemsTableSorter().Sort(result, SortBy(nameof(ItemAtStateDto.Id), descending: true));

            Assert.Equal(expected, result.Items.Select(x => x.Id));
        }
    }
}
