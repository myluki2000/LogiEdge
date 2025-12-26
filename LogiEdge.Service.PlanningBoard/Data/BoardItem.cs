using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.PlanningBoard.Data
{
    public class BoardItem
    {
        public Guid Id { get; set; }
        public BoardItemPosition? Position { get; set; }
    }
}
