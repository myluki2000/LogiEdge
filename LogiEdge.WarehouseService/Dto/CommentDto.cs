using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool Retracted { get; set; }
    }
}
