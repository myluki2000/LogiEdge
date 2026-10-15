using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.DocumentGenerator.Dto
{
    public class GeneratedDocumentDto
    {
        public required string ContentType { get; set; }
        public required string FileName { get; set; }
        public required byte[] Data { get; set; }
    }
}
