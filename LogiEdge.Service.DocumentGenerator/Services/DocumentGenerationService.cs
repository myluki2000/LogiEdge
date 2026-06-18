using LogiEdge.BaseService.Services;
using LogiEdge.Service.DocumentGenerator.Data;
using LogiEdge.Service.DocumentGenerator.DocumentGeneration;
using LogiEdge.Service.DocumentGenerator.Persistence;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using LogiEdge.BaseService.Data;

namespace LogiEdge.Service.DocumentGenerator.Services
{
    public class DocumentGenerationService(IServiceProvider serviceProvider)
    {
        private readonly FileAttachmentService _fileAttachmentService = serviceProvider.GetRequiredService<FileAttachmentService>();

        private readonly IDbContextFactory<DocumentGeneratorDbContext> _dbContextFactory =
            serviceProvider.GetRequiredService<IDbContextFactory<DocumentGeneratorDbContext>>();

        private static readonly List<Type> DOCUMENT_GENERATORS = typeof(DocumentGenerationService).Assembly.GetTypes()
            .Where(t => typeof(IDocumentGenerator).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).ToList();

        /// <summary>
        /// Generates a document using the specified generator and input data, and returns it as an UNSAVED FileAttachment.
        /// </summary>
        public async Task<FileAttachment> GenerateDocument<T>(IDocumentGenerator<T> generator, T inputData)
        {
            byte[] doc = await generator.GenerateDocumentAsync(inputData);
            FileAttachment att = new FileAttachment()
            {
                ContentType = "application/pdf", // TODO: Must depend on the generator used
                Data = new BinaryData(doc),
                FileName = $"{generator.GetType().Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
            };

            return att;
        }

        /// <summary>
        /// Get the available document generators for the specified type of document.
        /// </summary>
        /// <typeparam name="T">Type of document.</typeparam>
        /// <returns>IEnumerable of available document generator for the passed document type.</returns>
        public async IAsyncEnumerable<IDocumentGenerator<T>> GetGeneratorsForDocumentTypeAsync<T>()
        {
            // TODO: Should be cached
            foreach (DocumentGenerationTemplate template in await GetTemplatesForDocumentTypeAsync<T>())
            {
                Type generatorType = DOCUMENT_GENERATORS
                    .Select(x => x.MakeGenericType(typeof(T))) // make a concrete type for each generator with the input type as generic param
                    .First(g => // find the generator where the TemplateType property matches the template type
                        g.GetProperty("LogiEdge.Service.DocumentGenerator.DocumentGeneration.IDocumentGenerator.TemplateType", 
                            BindingFlags.NonPublic | BindingFlags.Static)!
                            .GetValue(null)!
                            .Equals(template.GetType()));

                // create an instance of the generator with the template as a constructor parameter
                IDocumentGenerator<T> generator =
                    (IDocumentGenerator<T>)Activator.CreateInstance(generatorType, serviceProvider, template)!;
                yield return generator;
            }
        }

        public async Task<DocumentGenerationTemplate?> GetTemplateAsync(Guid templateId)
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
            return await ctx.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == templateId);
        }

        public async Task<IEnumerable<DocumentGenerationTemplate>> GetTemplatesAsync()
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            return await ctx.DocumentTemplates.ToListAsync();
        }

        public async Task<IEnumerable<DocumentGenerationTemplate>> GetTemplatesForDocumentTypeAsync<T>()
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            return await ctx.DocumentTemplates.Where(t => t.InputTypeName == typeof(T).FullName).ToListAsync();
        }

        public async Task<DocumentGenerationTemplate> CreateTemplateForType<TTemplateType, TInputType>(
            string templateName)
            where TTemplateType : DocumentGenerationTemplate
            where TInputType : IDocumentGeneratorInput
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            TTemplateType template = Activator.CreateInstance<TTemplateType>();
            template.Name = templateName;
            template.InputTypeName = typeof(TInputType).FullName!;

            ctx.DocumentTemplates.Add(template);
            await ctx.SaveChangesAsync();

            return template;
        }

        public async Task<DocumentGenerationTemplate> UpdateTemplateAsync(DocumentGenerationTemplate template)
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
            ctx.DocumentTemplates.Update(template);
            await ctx.SaveChangesAsync();
            return template;
        }
    }
}
