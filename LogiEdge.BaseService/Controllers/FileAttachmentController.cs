using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Dto;
using LogiEdge.BaseService.Persistence;
using LogiEdge.BaseService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Controllers
{
    [ApiController]
    [Route("/api/attachments/{attachmentId:guid}")]
    [Authorize(AuthenticationSchemes = AppAuthSchemes.CookieAndToken)]
    public class FileAttachmentController(FileAttachmentService service)
        : ControllerBase
    {
        [HttpGet("download")]
        public async Task<IActionResult> Download(Guid attachmentId)
        {
            FileAttachmentDto? attachment = await service.GetAttachment(attachmentId, true);

            if (attachment == null)
                return NotFound();

            return File(attachment.Data!, attachment.ContentType);
        }
    }
}
