using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.Shared.Utility;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    public record ItemsTableStateParameters
    {
        public required DateTime? AtTime { get; init; }
        public required bool ShowShipped { get; init; }

        public virtual bool Equals(ItemsTableStateParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Nullable.Equals(AtTime, other.AtTime) && ShowShipped == other.ShowShipped;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AtTime, ShowShipped);
        }

        public Dictionary<string, StringValues> ToQueryParameters()
        {
            Dictionary<string, StringValues> result = [];

            if (AtTime.HasValue)
                result["at"] = AtTime.Value.ToString("o");

            if (ShowShipped)
                result["showShipped"] = ShowShipped.ToString();

            return result;
        }

        public static ItemsTableStateParameters FromQueryParameters(Dictionary<string, StringValues> queryParameters)
        {
            return new ItemsTableStateParameters()
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
            };
        }
    }
}
