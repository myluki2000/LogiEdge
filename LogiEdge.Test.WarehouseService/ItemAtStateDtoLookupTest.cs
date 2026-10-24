using LogiEdge.CustomerService.Data;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;
using System.Text.Json;

namespace LogiEdge.Test.WarehouseService
{
    public class ItemAtStateDtoLookupTest
    {
        private static readonly DateTime StateDate = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        private static (Item Item, ItemState State) Fixture()
        {
            Guid schemaId = Guid.NewGuid();
            Guid customerId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();

            ItemSchema schema = new()
            {
                Id = schemaId,
                Name = "Customer A — Spare Parts",
                AdditionalProperties =
                [
                    new ItemSchemaProperty
                    {
                        Name = "Color",
                        Type = ItemSchemaProperty.SupportedTypes.String,
                    },
                ],
                LookupColumns =
                [
                    new ItemSchemaLookupColumn
                    {
                        Id = Guid.NewGuid(),
                        ItemSchemaId = schemaId,
                        Name = "ArticleName",
                    },
                ],
            };

            Item item = new()
            {
                Id = Guid.NewGuid(),
                ItemSchemaId = schemaId,
                ItemSchema = schema,
                CustomerId = customerId,
                Customer = new Customer
                {
                    Id = customerId,
                    Abbreviation = "CA",
                    Name = "Customer A",
                },
                ItemNumber = "1140192",
                AdditionalProperties = JsonDocument.Parse("""{"Color":"Black"}"""),
                Comments = [],
                ItemStates = [],
            };

            ItemState state = new()
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                Date = StateDate,
                WarehouseId = warehouseId,
                Warehouse = new Warehouse
                {
                    Id = warehouseId,
                    Name = "Main",
                },
                Location = "A 1-1",
                IsQuarantined = false,
            };

            item.ItemStates = [state];

            return (item, state);
        }

        [Fact]
        public void FromItem_WithLookupValues_PopulatesLookupProperties()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(
                    item, state, new Dictionary<string, string> { ["ArticleName"] = "Casing, Color: Black" });

                Assert.Single(dto.LookupProperties);
                ItemAdditionalPropertyDto property = dto.LookupProperties[0];
                Assert.Equal("ArticleName", property.Name);
                Assert.Equal("Casing, Color: Black", property.Value);
                Assert.Equal(ItemSchemaProperty.SupportedTypes.String, property.Type);

                Assert.Equal(StateDate, dto.EntryDate);
            }
        }

        [Fact]
        public void GetPropertyValue_LookupColumn_ReturnsResolvedValue()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(
                    item, state, new Dictionary<string, string> { ["ArticleName"] = "Casing, Color: Black" });

                Assert.Equal("Casing, Color: Black", dto.GetPropertyValue("ArticleName"));
            }
        }

        [Fact]
        public void GetPropertyValue_AdditionalProperty_ReturnsAdditionalValue()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(item, state);

                Assert.Equal("Black", dto.GetPropertyValue("Color"));
            }
        }

        [Fact]
        public void GetPropertyValue_BaseProperty_ReturnsBaseValue()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(item, state);

                Assert.Equal("1140192", dto.GetPropertyValue(nameof(ItemAtStateDto.ItemNumber)));
            }
        }

        [Fact]
        public void FromItem_NullLookupValues_LookupPropertiesStayEmpty()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(item, state);

                Assert.Empty(dto.LookupProperties);
                Assert.Null(dto.GetPropertyValue("ArticleName"));
            }
        }

        [Fact]
        public void FromItem_DictionaryWithoutColumnName_LookupPropertiesStayEmpty()
        {
            var (item, state) = Fixture();
            using (item)
            {
                ItemAtStateDto dto = ItemAtStateDto.FromItem(
                    item, state, new Dictionary<string, string> { ["OtherColumn"] = "Some Value" });

                Assert.Empty(dto.LookupProperties);
                Assert.Null(dto.GetPropertyValue("ArticleName"));
            }
        }
    }
}
