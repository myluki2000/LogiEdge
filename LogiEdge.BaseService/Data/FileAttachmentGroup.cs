using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.BaseService.Data
{
    public class FileAttachmentGroup
    {
        public Guid Id { get; set; }
        public List<Guid> Attachments { get; set; } = [];
    }
}
