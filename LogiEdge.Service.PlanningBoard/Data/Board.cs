using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LogiEdge.Service.PlanningBoard.Data
{
    public class Board
    {
        public Guid Id { get; set; }
        [MaxLength(255)]
        public required string Title { get; set; }
        public required List<BoardColumn> Columns { get; set; }
        public required List<BoardItem> Items { get; set; }
    }
}
