using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared.Attributes
{
    /// <summary>
    /// Attribute to decorate properties of entities which should be displayed as a column if the entities
    /// are shown to the user in a table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayColumnPropertyAttribute : Attribute
    {
    }
}
