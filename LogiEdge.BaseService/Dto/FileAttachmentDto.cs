using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.BaseService.Dto
{
    public class FileAttachmentDto
    {
        public Guid Id { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        public byte[]? Data { get; set; } = null;
        public string? UserInputJson { get; set; }
    }
}
