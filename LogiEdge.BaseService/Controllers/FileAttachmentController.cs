using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Persistence;
using LogiEdge.BaseService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Controllers
{
    [ApiController]
    [Route("/api/attachments/{attachmentId:guid}")]
    public class FileAttachmentController(FileAttachmentService service)
        : ControllerBase
    {
        [HttpGet("download")]
        public async Task<IActionResult> Download(Guid attachmentId)
        {
            FileAttachment? attachment = await service.GetAttachmentAsync(attachmentId);

            if (attachment == null)
                return NotFound();

            return File(attachment.Data.Data, attachment.ContentType);
        }
    }
}
