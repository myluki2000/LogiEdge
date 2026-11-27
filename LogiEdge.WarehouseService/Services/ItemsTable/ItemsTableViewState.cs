using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    public sealed record ItemsTableViewState
    {
        public required ItemsTableFilterParameters Filter { get; init; }
        public required ItemsTableSortParameters Sort { get; init; }
        public required ItemsTableColumnDisplayParameters Columns { get; init; }
        public required ItemsTableStateParameters State { get; init; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string[]>>
            {
                ["filter"] = Serialize(Filter.ToQueryParameters()),
                ["sort"] = Serialize(Sort.ToQueryParameters()),
                ["columns"] = Serialize(Columns.ToQueryParameters()),
                ["state"] = Serialize(State.ToQueryParameters()),
            });
        }

        public static ItemsTableViewState FromJson(string json)
        {
            Dictionary<string, Dictionary<string, string[]>> sections =
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(json)!;

            return new ItemsTableViewState()
            {
                Filter = ItemsTableFilterParameters.FromQueryParameters(ToQueryParameters(sections["filter"])),
                Sort = ItemsTableSortParameters.FromQueryParameters(ToQueryParameters(sections["sort"])),
                Columns = ItemsTableColumnDisplayParameters.FromQueryParameters(ToQueryParameters(sections["columns"])),
                State = ItemsTableStateParameters.FromQueryParameters(ToQueryParameters(sections["state"])),
            };
        }

        private static Dictionary<string, string[]> Serialize(Dictionary<string, StringValues> parameters) =>
            parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(v => v!).ToArray());

        private static Dictionary<string, StringValues> ToQueryParameters(Dictionary<string, string[]> values) =>
            values.ToDictionary(kvp => kvp.Key, kvp => new StringValues(kvp.Value));
    }

    public sealed record ItemsTableViewDto(Guid Id, string Name, bool IsDefault);
}
