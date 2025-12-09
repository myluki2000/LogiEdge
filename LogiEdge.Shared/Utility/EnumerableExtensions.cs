using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Shared.Utility
{
    public static class EnumerableExtensions
    {
        extension<T>(IEnumerable<T>)
        {
            public static IEnumerable<T> Of(params T[] items)
            {
                foreach (T item in items)
                {
                    yield return item;
                }
            }
        }
    }
}
