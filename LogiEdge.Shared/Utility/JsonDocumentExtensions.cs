using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogiEdge.Shared.Utility
{
    public static class JsonDocumentExtensions
    {
        /// <summary>
        /// Creates a new JsonDocument with the same content, so that the copy can be mutated or
        /// disposed independently of the original.
        /// </summary>
        public static JsonDocument DeepClone(this JsonDocument document)
        {
            string json = document.RootElement.GetRawText();
            return JsonDocument.Parse(json);
        }
    }
}
