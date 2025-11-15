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

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    public record ItemsTableFilterParameters
    {
        public required DateTime? AtTime { get; init; }
        public required bool ShowShipped { get; init; }
        public required IReadOnlyDictionary<string, object> BaseParameters { get; init; }
        public required IReadOnlyDictionary<string, object> StateParameters { get; init; }
        public required IReadOnlyDictionary<string, string> AdditionalPropertyParameters { get; init; }

        public virtual bool Equals(ItemsTableFilterParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Nullable.Equals(AtTime, other.AtTime) 
                   && ShowShipped == other.ShowShipped 
                   && BaseParameters.SequenceEqual(other.BaseParameters) 
                   && StateParameters.SequenceEqual(other.StateParameters) 
                   && AdditionalPropertyParameters.SequenceEqual(other.AdditionalPropertyParameters);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AtTime, ShowShipped, BaseParameters, StateParameters, AdditionalPropertyParameters);
        }

        public Dictionary<string, StringValues> ToQueryParameters()
        {
            Dictionary<string, StringValues> result = [];

            if (AtTime.HasValue)
                result["at"] = AtTime.Value.ToString("o");

            if (ShowShipped)
                result["showShipped"] = ShowShipped.ToString();

            foreach (KeyValuePair<string, object> kvp in BaseParameters)
            {
                result[kvp.Key] = PropertyToString(kvp.Value);
            }

            foreach (KeyValuePair<string, object> kvp in StateParameters)
            {
                result[kvp.Key] = PropertyToString(kvp.Value);
            }

            foreach (KeyValuePair<string, string> kvp in AdditionalPropertyParameters)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        public static ItemsTableFilterParameters FromQueryParameters(Dictionary<string, StringValues> queryParameters)
        {
            List<PropertyInfo> basePropInfos = typeof(Item)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();
            List<PropertyInfo> statePropInfos = typeof(ItemState)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();

            return new ItemsTableFilterParameters()
            {
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
                .ToDictionary(
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
                    .ToDictionary(
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
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.First()!)
            };
        }

        private static object PropertyToType(string propertyValue, Type propertyType)
        {
            return propertyType switch
            {
                { } when propertyType == typeof(int) => int.Parse(propertyValue),
                not null when propertyType == typeof(decimal) => decimal.Parse(propertyValue),
                not null when propertyType == typeof(DateTime) => DateTime.SpecifyKind(DateTime.Parse(propertyValue), DateTimeKind.Utc),
                not null when propertyType == typeof(Guid) => Guid.Parse(propertyValue),
                _ => propertyValue
            };
        }

        private static string PropertyToString(object propertyValue)
        {
            Type propertyType = propertyValue.GetType();
            return propertyType switch
            {
                not null when propertyType == typeof(DateTime) => ((DateTime)propertyValue).ToString("o"),
                _ => propertyValue.ToString() ?? string.Empty
            };
        }
    }
}
