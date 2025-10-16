using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class BookingException : Exception
    {
        public BookingException(string message) : base(message)
        {
        }

        public BookingException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
