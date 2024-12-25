using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared.Attributes
{
    /// <summary>
    /// Decorate properties of an item class with this attribute to indicate that the item should be filterable by
    /// that property using query parameters. Comparisons happen as string comparisons.
    /// 
    /// If the property which is annotated with this attribute is a complex type, the compareQueryParamWith parameter
    /// can be set to the name of an alternate property of the item, which should be used for comparison.
    /// 
    /// For example, this makes sense if we have a navigation property which we want to make filterable. Then the
    /// compareQueryParamWith parameter should be set to the Key/ID of the property, which can be used for comparison.
    /// </summary>
    /// <param name="compareQueryParamWith"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class QueryFilterablePropertyAttribute(string? compareQueryParamWith = null) : Attribute
    {
        public string? CompareQueryParamWith { get; set; } = compareQueryParamWith;
    }
}
