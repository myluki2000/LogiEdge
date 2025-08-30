using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.Shared.Attributes;
using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using Microsoft.Extensions.Primitives;

namespace LogiEdge.Warehouse
{
    internal class ItemsPageQueryParameters
    {
        public required DateTime? AtTime { get; init; }
        public required string? GroupByProperty { get; init; }
        public required string? SortByProperty { get; init; }
        public required bool SortOrderDescending { get; init; }
        public required bool ShowShipped { get; init; }
        public required Dictionary<string, object> BaseParameters { get; init; }
        public required Dictionary<string, object> StateParameters { get; init; }
        public required Dictionary<string, string> AdditionalPropertyParameters { get; init; }

        private readonly List<string> columnsToDisplayParam;

        private ItemsPageQueryParameters(List<string> columnsToDisplayParam)
        {
            this.columnsToDisplayParam = columnsToDisplayParam;
        }

        public IEnumerable<string> GetColumnsToDisplay(IList<ItemSchema> itemSchemas)
        {
            if (columnsToDisplayParam.Count > 0)
                return columnsToDisplayParam;
            else
                return
                    new List<string>
                    {
                        nameof(Item.Id),
                        nameof(Item.Customer),
                        nameof(Item.ItemNumber),
                        nameof(ItemState.Warehouse),
                        nameof(ItemState.Location),
                        nameof(Item.EntryDate)
                    }.Concat(itemSchemas.SelectMany(sch => sch.AdditionalProperties).Select(pr => pr.Name));

        }

        public static ItemsPageQueryParameters FromQueryParameters(Dictionary<string, StringValues> queryParametersInput)
        {
            Dictionary<string, StringValues> queryParameters = new(queryParametersInput);

            List<PropertyInfo> basePropInfos = typeof(Item)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();
            List<PropertyInfo> statePropInfos = typeof(ItemState)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();

            List<string> columnsToDisplayParam = queryParameters
                .PopWhere(x => x.Key == "col")
                .SelectMany(x => x.Value.ToList())
                .OfType<string>()
                .ToList();

            return new ItemsPageQueryParameters(columnsToDisplayParam)
            {
                SortByProperty = queryParameters
                    .PopWhere(param => param.Key == "sortBy")
                    .Select(param => param.Value.FirstOrDefault())
                    .FirstOrDefault(),
                SortOrderDescending = queryParameters
                    .PopWhere(param => param.Key == "orderDescending")
                    .Select(param => bool.Parse(param.Value.FirstOrDefault()!))
                    .FirstOrDefault(),
                GroupByProperty = queryParameters
                    .PopWhere(param => param.Key == "groupBy")
                    .Select(param => param.Value.FirstOrDefault())
                    .FirstOrDefault(),
                AtTime = queryParameters
                    .PopWhere(param => param.Key == "at" && param.Value.Count > 0)
                    .Select(param => DateTime.SpecifyKind(DateTime.Parse(param.Value.First()!), DateTimeKind.Utc))
                    .Cast<DateTime?>()
                    .FirstOrDefault(),
                ShowShipped = queryParameters
                    .PopWhere(param => param.Key == "showShipped" && param.Value.Count > 0)
                    .Select(param => bool.Parse(param.Value.First()!))
                    .FirstOrDefault(),
                BaseParameters = queryParameters
                .PopWhere(param => basePropInfos.Any(prop => prop.Name == param.Key) && param.Value.Count > 0)
                .ToDictionary<KeyValuePair<string, StringValues>, string, object>(
                    x => x.Key,
                    x =>
                    {
                        string stringValue = x.Value.First()!;

                        PropertyInfo propInfo = basePropInfos.FirstOrDefault(prop => prop.Name == x.Key)
                                                ?? throw new Exception("Could not find property with name " + x.Key);

                        Type propertyType;

                        if (propInfo.GetCustomAttribute<QueryFilterablePropertyAttribute>() is { CompareQueryParamWith: not null } attr)
                        {
                            PropertyInfo comparePropInfo = typeof(Item).GetProperty(attr.CompareQueryParamWith)
                                                           ?? throw new Exception(
                                                               "Property " + attr.CompareQueryParamWith + ", referenced by [QueryFilterableProperty] attribute on property "
                                                               + propInfo.Name + ", could not be found on type " + propInfo.DeclaringType?.Name);

                            propertyType = comparePropInfo.PropertyType;
                        }
                        else
                        {
                            propertyType = propInfo.PropertyType;
                        }

                        return PropertyToType(stringValue, propertyType);
                    }),
                StateParameters = queryParameters
                    .PopWhere(param => statePropInfos.Any(prop => prop.Name == param.Key) && param.Value.Count > 0)
                    .ToDictionary<KeyValuePair<string, StringValues>, string, object>(
                        x => x.Key,
                        x =>
                        {
                            string stringValue = x.Value.First()!;

                            PropertyInfo propInfo = statePropInfos.FirstOrDefault(prop => prop.Name == x.Key)
                                                    ?? throw new Exception("Could not find property with name " + x.Key);

                            Type propertyType;

                            if (propInfo.GetCustomAttribute<QueryFilterablePropertyAttribute>() is { CompareQueryParamWith: not null } attr)
                            {
                                PropertyInfo comparePropInfo = typeof(ItemState).GetProperty(attr.CompareQueryParamWith)
                                                               ?? throw new Exception(
                                                                   "Property " + attr.CompareQueryParamWith + ", referenced by [QueryFilterableProperty] attribute on property "
                                                                   + propInfo.Name + ", could not be found on type " + propInfo.DeclaringType?.Name);

                                propertyType = comparePropInfo.PropertyType;
                            }
                            else
                            {
                                propertyType = propInfo.PropertyType;
                            }

                            return PropertyToType(stringValue, propertyType);
                        }),
                AdditionalPropertyParameters = queryParameters
                    .Where(x => x.Value.Count > 0)
                    .ToDictionary<KeyValuePair<string, StringValues>, string, string>(
                        x => x.Key,
                        x => x.Value.First()!)
            };
        }

        private static object PropertyToType(string propertyValue, Type propertyType)
        {
            return propertyType switch
            {
                { } t when t == typeof(int) => int.Parse(propertyValue),
                { } t when t == typeof(decimal) => decimal.Parse(propertyValue),
                { } t when t == typeof(DateTime) => DateTime.SpecifyKind(DateTime.Parse(propertyValue), DateTimeKind.Utc),
                { } t when t == typeof(Guid) => Guid.Parse(propertyValue),
                _ => propertyValue
            };
        }
    }
}
