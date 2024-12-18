using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FilterablePropertyAttribute : Attribute
    {
    }
}
