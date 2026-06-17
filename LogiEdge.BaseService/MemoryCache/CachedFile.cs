using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.BaseService.MemoryCache
{
    public class CachedFile
    {
        public required string MimeType { get; set; }
        public required byte[] Data { get; set; }
        /// <summary>
        /// If set to true, the file will be served at the endpoint "/tmp/{id:guid}" and can be accessed by its ID.
        /// Otherwise, it can only be accessed by the reference returned when it is cached.
        /// </summary>
        public required bool ServeAtEndpoint { get; set; }
    }
}
