using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Dto;
using LogiEdge.BaseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.BaseService.Services
{
    public class FileAttachmentService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;

        internal FileAttachmentService(IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
        {
            this._applicationDbContextFactory = applicationDbContextFactory;
        }

        /// <summary>
        /// Gets the attachment with the given id. If includeData is true, the attachment's data will also be included in the result.
        /// </summary>
        public async Task<FileAttachmentDto?> GetAttachment(Guid id, bool includeData = false)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();

            FileAttachment? att = includeData
                ? await context.Attachments.Include(a => a.Data).FirstOrDefaultAsync(a => a.Id == id)
                : await context.Attachments.FindAsync(id);

            if (att == null)
            {
                return null;
            }

            return FileAttachmentToDto(att);
        }

        public async Task<List<FileAttachmentDto>> GetAttachments(List<Guid> ids, bool includeData = false)
        {
            if (ids.Count == 0)
                return [];

            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            var q = context.Attachments.Where(at => ids.Contains(at.Id));

            if (includeData)
                q = q.Include(a => a.Data);

            return q.AsEnumerable().Select(FileAttachmentToDto).ToList();
        }

        public async Task<FileAttachmentDto> CreateAttachment(string fileName, string contentType, byte[] data, string? userInputJson = null)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            EntityEntry<FileAttachment> saved = await context.Attachments.AddAsync(new FileAttachment()
            {
                FileName = fileName,
                ContentType = contentType,
                Data = new BinaryData(data),
                UserInputJson = userInputJson,
            });
            await context.SaveChangesAsync();
            return FileAttachmentToDto(saved.Entity);
        }

        public async Task<FileAttachmentDto?> DeleteAttachment(Guid id)
        {
            await using ApplicationDbContext context = await _applicationDbContextFactory.CreateDbContextAsync();
            FileAttachment? attachment = await context.Attachments.FindAsync(id);

            if (attachment is null) return null;

            context.Attachments.Remove(attachment);

            // if delete fails because of concurrency exception, that means the attachment was already deleted,
            // so we don't care
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) { }

            return FileAttachmentToDto(attachment);
        }

        private FileAttachmentDto FileAttachmentToDto(FileAttachment a)
        {
            return new FileAttachmentDto()
            {
                Id = a.Id,
                ContentType = a.ContentType,
                FileName = a.FileName,
                Data = a.Data?.Data,
                UserInputJson = a.UserInputJson,
            };
        }
    }
}
