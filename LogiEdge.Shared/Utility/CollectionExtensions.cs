using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared.Utility
{
    public static class CollectionExtensions
    {
        public static ICollection<T> PopWhere<T>(this ICollection<T> list, Func<T, bool> predicate)
        {
            List<T> poppedItems = list.Where(predicate).ToList();

            foreach (T poppedItem in poppedItems)
            {
                while (list.Remove(poppedItem)) { }
            }

            return poppedItems;
        }

        public static T PopFirst<T>(this ICollection<T> list, Func<T, bool> predicate)
        {
            return list.PopWhere(predicate).First();
        }
    }
}
