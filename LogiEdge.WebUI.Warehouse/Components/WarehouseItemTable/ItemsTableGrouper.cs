using System.Linq.Expressions;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Services.ItemsTable;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    public class ItemsTableGrouper
    {
        public IEnumerable<IGrouping<ItemGroupPropertiesValues, ItemAtStateDto>> Group(List<ItemAtStateDto> itemsAtMoment, ItemsTableSortParameters sortParameters)
        {
            ParameterExpression parameterExp = Expression.Parameter(typeof(ItemState), "st");

            string[] groupByProperties = sortParameters.GroupByProperties ?? Array.Empty<string>();

            if (groupByProperties.Length == 0)
                return itemsAtMoment
                    .Select(st => new Grouping<ItemGroupPropertiesValues, ItemAtStateDto>(
                        ItemGroupPropertiesValues.OfItemAtState(st, groupByProperties), 
                        [st]));

            return itemsAtMoment.GroupBy(st => ItemGroupPropertiesValues.OfItemAtState(st, groupByProperties));
        }

        public class ItemGroupPropertiesValues(IReadOnlyDictionary<string, object?> groupProperties)
        {
            private readonly IReadOnlyDictionary<string, object?> _groupProperties = groupProperties;

            public object? this[string propertyName] => _groupProperties[propertyName];

            public IEnumerable<string> Properties => _groupProperties.Keys;

            public bool ContainsProperty(string propertyName) => _groupProperties.ContainsKey(propertyName);

            public static ItemGroupPropertiesValues OfItemAtState(ItemAtStateDto item, string[] groupByPropertyNames)
            {
                Dictionary<string, object?> groupProperties = [];

                foreach (string propertyName in groupByPropertyNames)
                {
                    groupProperties.Add(propertyName, item.GetPropertyValue(propertyName));
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
