using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    internal class ItemsTableFilter()
    {
        private static readonly ParameterExpression itemParameterExpression = Expression.Parameter(typeof(Item), "it");
        private static readonly ParameterExpression itemStateParameterExpression = Expression.Parameter(typeof(ItemState), "st");
        private static readonly MethodInfo jsonContainsMethod = typeof(NpgsqlJsonDbFunctionsExtensions)
            .GetMethod(nameof(NpgsqlJsonDbFunctionsExtensions.JsonContains), [typeof(DbFunctions), typeof(JsonDocument), typeof(JsonDocument)])!;

        public IQueryable<ItemState> Filter(IQueryable<Item> itemsQueryableInput, ItemsTableFilterParameters parameters)
        {
            IQueryable<ItemState> itemStatesQueryable = parameters.AtTime.HasValue
                ? itemsQueryableInput
                    .Select(it => it.ItemStates
                        .OrderByDescending(st => st.Date)
                        .First(st => st.Date <= parameters.AtTime))
                : itemsQueryableInput
                    .Select(it => it.ItemStates
                        .OrderByDescending(st => st.Date)
                        .First());

            if (!parameters.ShowShipped)
            {
                itemStatesQueryable = itemStatesQueryable
                    .Where(st => st.Location != SpecialLocations.SHIPPED);
            }

            // parse params which are not additional properties but "base properties" we store directly
            // as a DB column instead of as JSON
            List<PropertyInfo> basePropInfos = typeof(Item)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();

            // do the same for item state params
            List<PropertyInfo> statePropInfos = typeof(ItemState)
                .GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)))
                .ToList();

            MemberExpression itemPropertyExpression = Expression.Property(itemStateParameterExpression, nameof(ItemState.Item));

            // construct an expression to filter for the base item properties passed as query params
            foreach (KeyValuePair<string, object> param in parameters.BaseParameters)
            {
                string? foreignComparisonKey = basePropInfos
                    .FirstOrDefault(pi => pi.Name == param.Key)?
                    .GetCustomAttribute<QueryFilterablePropertyAttribute>()?
                    .CompareQueryParamWith;

                MemberExpression paramPropertyExpression = Expression.Property(itemPropertyExpression, foreignComparisonKey ?? param.Key);
                BinaryExpression equalityExpression = Expression.Equal(
                    paramPropertyExpression,
                    Expression.Constant(param.Value));
                Expression<Func<ItemState, bool>> lambda = Expression.Lambda<Func<ItemState, bool>>(equalityExpression, itemStateParameterExpression);
                itemStatesQueryable = itemStatesQueryable.Where(lambda);
            }

            // construct an expression to filter for the item state properties passed as query params
            foreach (KeyValuePair<string, object> param in parameters.StateParameters)
            {
                string? foreignComparisonKey = statePropInfos
                    .FirstOrDefault(pi => pi.Name == param.Key)?
                    .GetCustomAttribute<QueryFilterablePropertyAttribute>()?
                    .CompareQueryParamWith;

                MemberExpression propertyExpression = Expression.Property(itemStateParameterExpression, foreignComparisonKey ?? param.Key);
                BinaryExpression equalityExpression = Expression.Equal(
                    propertyExpression,
                    Expression.Constant(param.Value));
                Expression<Func<ItemState, bool>> lambda = Expression.Lambda<Func<ItemState, bool>>(
                    equalityExpression,
                    itemStateParameterExpression
                );

                itemStatesQueryable = itemStatesQueryable.Where(lambda);
            }

            // if any AdditionalProperties query params have been specified, construct an expression to filter for those
            if (parameters.AdditionalPropertyParameters.Count > 0)
            {
                // create a json document which contains the properties we want to filter for
                JsonObject jsonFilter = new();
                foreach (KeyValuePair<string, string> queryParam in parameters.AdditionalPropertyParameters)
                {
                    jsonFilter[queryParam.Key] = queryParam.Value;
                }
                JsonDocument jsonFilterDoc = jsonFilter.Deserialize<JsonDocument>()!;

                // call the JsonContains function with the json filter document we built
                MethodCallExpression callJsonContainsExpression = Expression.Call(
                    jsonContainsMethod,
                    Expression.Constant(EF.Functions),
                    Expression.Property(itemPropertyExpression, nameof(Item.AdditionalProperties)),
                    Expression.Constant(jsonFilterDoc));
                Expression<Func<ItemState, bool>> lambda = Expression.Lambda<Func<ItemState, bool>>(callJsonContainsExpression, itemStateParameterExpression);
                itemStatesQueryable = itemStatesQueryable.Where(lambda);
            }

            return itemStatesQueryable;
        } 
    }
}
