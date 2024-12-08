using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Test.Shared.Utility
{
    public class TestContextFactory<T>(Func<T> contextCreationFunc) : IDbContextFactory<T> where T : DbContext
    {
        public T CreateDbContext()
        {
            return contextCreationFunc.Invoke();
        }
    }
}
