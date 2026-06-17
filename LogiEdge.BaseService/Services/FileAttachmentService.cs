using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Services
{
    public class FileAttachmentService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;

        internal FileAttachmentService(IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
        {
            this._applicationDbContextFactory = applicationDbContextFactory;
        }

        public FileAttachment? GetAttachment(Guid id, bool includeData = false)
        {
            return GetAttachmentAsync(id, includeData).Result;
        }

        /// <summary>
        /// Gets the attachment with the given id. If includeData is true, the attachment's data will also be included in the result.
        /// </summary>
        public async Task<FileAttachment?> GetAttachmentAsync(Guid id, bool includeData = false)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();

            if (includeData)
                return await context.Attachments.Include(a => a.Data).FirstOrDefaultAsync(a => a.Id == id);
            
            return await context.Attachments.FindAsync(id);
        }

        public List<FileAttachment> GetAttachments(List<Guid> ids, bool includeData = false)
        {
            return GetAttachmentsAsync(ids, includeData).Result;
        }

        public async Task<List<FileAttachment>> GetAttachmentsAsync(List<Guid> ids, bool includeData = false)
        {
            if (ids.Count == 0)
                return [];

            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            var q = context.Attachments.Where(at => ids.Contains(at.Id));

            if (includeData)
                q = q.Include(a => a.Data);

            return await q.ToListAsync();
        }

        public FileAttachment CreateAttachment(FileAttachment attachment)
        {
            return CreateAttachmentAsync(attachment).Result;
        }

        public async Task<FileAttachment> CreateAttachmentAsync(FileAttachment attachment)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            await context.Attachments.AddAsync(attachment);
            await context.SaveChangesAsync();
            return attachment;
        }

        public FileAttachment? DeleteAttachment(Guid id)
        {
            return DeleteAttachmentAsync(id).Result;
        }

        public async Task<FileAttachment?> DeleteAttachmentAsync(Guid id)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            FileAttachment? attachment = await context.Attachments.FindAsync(id);

            if (attachment is null) return attachment;

            context.Attachments.Remove(attachment);

            // if delete fails because of concurrency exception, that means the attachment was already deleted,
            // so we don't care
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) { }

            return attachment;
        }
    }
}
