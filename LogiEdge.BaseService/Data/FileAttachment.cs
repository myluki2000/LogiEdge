using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.Shared.Conventions;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Data
{
    public class FileAttachment
    {
        [Key]
        public Guid Id { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        [AutoInclude(false)]
        public required BinaryData Data { get; set; }
    }

    [Owned]
    public class BinaryData(byte[] data)
    {
        public byte[] Data { get; set; } = data;
    }
}
