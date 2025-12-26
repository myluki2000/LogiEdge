using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Service.PlanningBoard.Data
{
    [Owned]
    public class BoardItemPosition
    {
        public Guid? ColumnId { get; set; }
        public BoardColumn? Column { get; set; }
        public DateOnly? StartDate { get; set; }
        public int PositionInStartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int PositionInEndDate { get; set; }
    }
}
