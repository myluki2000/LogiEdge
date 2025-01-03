using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.BaseService.Data
{
    public class FileAttachment
    {
        [Key]
        public Guid Id { get; set; }
        public required byte[] Data { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
    }
}
