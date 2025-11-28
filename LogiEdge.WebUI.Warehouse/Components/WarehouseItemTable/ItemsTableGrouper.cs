using LogiEdge.WarehouseService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    internal class ItemsTableGrouper
    {
        public IEnumerable<IGrouping<ItemGroupPropertiesValues, ItemState>> Group(List<ItemState> itemsAtMoment, ItemsTableSortParameters sortParameters)
        {
            ParameterExpression parameterExp = Expression.Parameter(typeof(ItemState), "st");

            string[] groupByProperties = sortParameters.GroupByProperties ?? Array.Empty<string>();

            if (groupByProperties.Length == 0)
                return itemsAtMoment
                    .Select(st => new Grouping<ItemGroupPropertiesValues, ItemState>(
                        ItemGroupPropertiesValues.OfItemState(st, groupByProperties), 
                        [st]));

            return itemsAtMoment.GroupBy(st => ItemGroupPropertiesValues.OfItemState(st, groupByProperties));
        }

        public class ItemGroupPropertiesValues(IReadOnlyDictionary<string, object?> groupProperties)
        {
            private readonly IReadOnlyDictionary<string, object?> _groupProperties = groupProperties;

            public object? this[string propertyName] => _groupProperties[propertyName];

            public bool ContainsProperty(string propertyName) => _groupProperties.ContainsKey(propertyName);

            public static ItemGroupPropertiesValues OfItemState(ItemState itemState, string[] groupByPropertyNames)
            {
                Dictionary<string, object?> groupProperties = [];

                foreach (string propertyName in groupByPropertyNames)
                {
                    if (typeof(ItemState).GetProperty(propertyName) is { } stateInfo)
                    {
                        groupProperties.Add(propertyName, stateInfo.GetValue(itemState));
                    } 
                    else if (typeof(Item).GetProperty(propertyName) is { } itemInfo)
                    {
                        groupProperties.Add(propertyName, itemInfo.GetValue(itemState.Item));
                    }
                    else
                    {
                        groupProperties.Add(propertyName, itemState.Item.GetAdditionalProperty(propertyName));
                    }
                }

                return new ItemGroupPropertiesValues(groupProperties);
            }

            protected bool Equals(ItemGroupPropertiesValues other)
            {
                return _groupProperties.SequenceEqual(other._groupProperties);
            }

            public override bool Equals(object? obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ItemGroupPropertiesValues)obj);
            }

            public override int GetHashCode()
            {
                return 0;
            }

            public static bool operator ==(ItemGroupPropertiesValues? left, ItemGroupPropertiesValues? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(ItemGroupPropertiesValues? left, ItemGroupPropertiesValues? right)
            {
                return !Equals(left, right);
            }
        }

        private class Grouping<TKey, TElement> : List<TElement>, IGrouping<TKey, TElement>
        {
            public Grouping(TKey key) => Key = key;
            public Grouping(TKey key, int capacity) : base(capacity) => Key = key;
            public Grouping(TKey key, IEnumerable<TElement> collection)
                : base(collection) => Key = key;
            public TKey Key { get; }
        }
    }
}
