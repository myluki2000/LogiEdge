using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public class Comment : IComparable<Comment>
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool Retracted { get; set; }

        public int CompareTo(Comment? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Date.CompareTo(other.Date);
        }
    }
}
