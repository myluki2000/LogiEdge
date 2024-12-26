using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared.Utility
{
    public static class DateTimeExtensions
    {
        public static DateTime SpecifyKind(this DateTime dt, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(dt, kind);
        }
    }
}
