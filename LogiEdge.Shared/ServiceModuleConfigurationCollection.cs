using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.Shared
{
    public class ServiceModuleConfigurationCollection : List<IServiceModuleConfiguration>
    {
        public ServiceModuleConfigurationCollection()
        {
        }

        public ServiceModuleConfigurationCollection(IEnumerable<IServiceModuleConfiguration> collection) : base(collection)
        {
        }

        public ServiceModuleConfigurationCollection(int capacity) : base(capacity)
        {
        }
    }
}
