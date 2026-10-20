using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.ItemsTable;
using Microsoft.Extensions.Primitives;

namespace LogiEdge.Test.WarehouseService
{
    public class ItemsTableFilterParametersTest
    {
        private static ItemsTableFilterParameters EmptyParams() => new()
        {
            BaseParameters = new Dictionary<string, object>(),
            StateParameters = new Dictionary<string, object>(),
            AdditionalPropertyParameters = new Dictionary<string, string>(),
        };

        [Fact]
        public void WithParameter_Location_IsStoredAsStateParameter()
        {
            ItemsTableFilterParameters result = EmptyParams().WithParameter(nameof(ItemState.Location), "A 1-1");

            Assert.Equal("A 1-1", result.StateParameters[nameof(ItemState.Location)]);
            Assert.Empty(result.AdditionalPropertyParameters);
        }

        [Fact]
        public void GetParameter_Location_ReturnsStoredValue()
        {
            ItemsTableFilterParameters parameters = EmptyParams().WithParameter(nameof(ItemState.Location), "A 1-1");

            Assert.Equal("A 1-1", parameters.GetParameter(nameof(ItemState.Location)));
        }

        [Fact]
        public void WithoutParameter_Location_RemovesStateParameter()
        {
            ItemsTableFilterParameters parameters = EmptyParams()
                .WithParameter(nameof(ItemState.Location), "A 1-1")
                .WithoutParameter(nameof(ItemState.Location));

            Assert.Empty(parameters.StateParameters);
            Assert.Null(parameters.GetParameter(nameof(ItemState.Location)));
        }

        [Fact]
        public void WithParameter_Warehouse_IsStoredAsStateParameter()
        {
            Guid warehouseId = Guid.NewGuid();

            ItemsTableFilterParameters result = EmptyParams().WithParameter(nameof(ItemState.Warehouse), warehouseId);

            Assert.Equal(warehouseId, result.StateParameters[nameof(ItemState.Warehouse)]);
            Assert.Empty(result.AdditionalPropertyParameters);
        }

        [Fact]
        public void WithParameter_UnknownProperty_IsStoredAsAdditionalProperty()
        {
            ItemsTableFilterParameters result = EmptyParams().WithParameter("SomeCustomProp", "value");

            Assert.Equal("value", result.AdditionalPropertyParameters["SomeCustomProp"]);
        }

        [Fact]
        public void FromQueryParameters_LocationQueryParam_IsParsedAsStateParameter()
        {
            Dictionary<string, StringValues> queryParameters = new()
            {
                ["Location"] = "A 1-1",
            };

            ItemsTableFilterParameters result = ItemsTableFilterParameters.FromQueryParameters(queryParameters);

            Assert.Equal("A 1-1", result.StateParameters[nameof(ItemState.Location)]);
            Assert.Empty(result.AdditionalPropertyParameters);
        }
    }
}
