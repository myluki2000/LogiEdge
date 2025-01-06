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
        private readonly IDbContextFactory<ApplicationDbContext> applicationDbContextFactory;

        internal FileAttachmentService(IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
        {
            this.applicationDbContextFactory = applicationDbContextFactory;
        }

        public FileAttachment? GetAttachment(Guid id)
        {
            return GetAttachmentAsync(id).Result;
        }

        public async Task<FileAttachment?> GetAttachmentAsync(Guid id)
        {
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
            return await context.Attachments.FindAsync(id);
        }

        public List<FileAttachment> GetAttachments(List<Guid> ids)
        {
            return GetAttachmentsAsync(ids).Result;
        }

        public async Task<List<FileAttachment>> GetAttachmentsAsync(List<Guid> ids)
        {
            if (ids.Count == 0)
                return [];

            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
            return context.Attachments.Where(at => ids.Contains(at.Id)).ToList();
        }

        public FileAttachment CreateAttachment(FileAttachment attachment)
        {
            return CreateAttachmentAsync(attachment).Result;
        }

        public async Task<FileAttachment> CreateAttachmentAsync(FileAttachment attachment)
        {
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
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
            await using ApplicationDbContext context = await applicationDbContextFactory.CreateDbContextAsync();
            FileAttachment? attachment = await context.Attachments.FindAsync(id);

            if (attachment is null) return attachment;

            context.Attachments.Remove(attachment);
            await context.SaveChangesAsync();
            return attachment;
        }
    }
}
