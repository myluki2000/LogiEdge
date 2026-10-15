using LogiEdge.BaseService.Services;
using LogiEdge.Service.DocumentGenerator.Data;
using LogiEdge.Service.DocumentGenerator.DocumentGeneration;
using LogiEdge.Service.DocumentGenerator.Persistence;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using LogiEdge.Service.DocumentGenerator.Dto;

namespace LogiEdge.Service.DocumentGenerator.Services
{
    public class DocumentGenerationService(IServiceProvider serviceProvider)
    {
        private readonly IDbContextFactory<DocumentGeneratorDbContext> _dbContextFactory =
            serviceProvider.GetRequiredService<IDbContextFactory<DocumentGeneratorDbContext>>();

        private static readonly List<Type> DOCUMENT_GENERATORS = typeof(DocumentGenerationService).Assembly.GetTypes()
            .Where(t => typeof(IDocumentGenerator).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).ToList();

        /// <summary>
        /// Generates a document using the specified generator and input data.
        /// </summary>
        public async Task<GeneratedDocumentDto> GenerateDocument<T>(IDocumentGenerator<T> generator, T inputData)
        {
            byte[] doc = await generator.GenerateDocumentAsync(inputData);
            return new GeneratedDocumentDto()
            {
                ContentType = "application/pdf", // TODO: Must depend on the generator used
                Data = doc,
                FileName = $"{generator.GetType().Name}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
            };
        }

        /// <summary>
        /// Get the available document generators for the specified type of document.
        /// </summary>
        /// <typeparam name="T">Type of document.</typeparam>
        /// <returns>IEnumerable of available document generator for the passed document type.</returns>
        public async IAsyncEnumerable<IDocumentGenerator<T>> GetGeneratorsForDocumentType<T>()
        {
            // TODO: Should be cached
            foreach (DocumentGenerationTemplate template in await GetTemplatesForDocumentType<T>())
            {
                Type generatorType = DOCUMENT_GENERATORS
                    .Select(x => x.MakeGenericType(typeof(T))) // make a concrete type for each generator with the input type as generic param
                    .First(g => // find the generator where the TemplateType property matches the template type
                        g.GetProperty("LogiEdge.Service.DocumentGenerator.DocumentGeneration.IDocumentGenerator.TemplateType", 
                            BindingFlags.NonPublic | BindingFlags.Static)!
                            .GetValue(null)!
                            .Equals(template.GetType()));

                // create an instance of the generator with the template DTO as a constructor parameter
                IDocumentGenerator<T> generator =
                    (IDocumentGenerator<T>)Activator.CreateInstance(generatorType, serviceProvider, DocumentGenerationTemplateToDto(template))!;
                yield return generator;
            }
        }

        public async Task<DocumentGenerationTemplateDto?> GetTemplate(Guid templateId)
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            DocumentGenerationTemplate? template = await ctx.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == templateId);
            return template is null ? null : DocumentGenerationTemplateToDto(template);
        }

        public async Task<IEnumerable<DocumentGenerationTemplateDto>> GetTemplates()
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            List<DocumentGenerationTemplate> templates = await ctx.DocumentTemplates.ToListAsync();
            return templates.Select(DocumentGenerationTemplateToDto).ToList();
        }

        private async Task<List<DocumentGenerationTemplate>> GetTemplatesForDocumentType<T>()
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            return await ctx.DocumentTemplates.Where(t => t.InputTypeName == typeof(T).FullName).ToListAsync();
        }

        public async Task<DocumentGenerationTemplateDto> CreateTemplateForType<TTemplateType, TInputType>(
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

            return DocumentGenerationTemplateToDto(template);
        }

        /// <summary>
        /// Updates an existing template with the data from the given DTO and returns the updated template as a DTO.
        /// </summary>
        public async Task<DocumentGenerationTemplateDto> UpdateTemplate(DocumentGenerationTemplateDto templateDto)
        {
            await using DocumentGeneratorDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

            if (templateDto is not HtmlDocumentGenerationTemplateDto htmlDto)
            {
                throw new ArgumentException($"Unsupported template type: {templateDto.GetType().Name}.", nameof(templateDto));
            }

            HtmlDocumentGenerationTemplate? template = await ctx.HtmlTemplates
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == htmlDto.Id);
            if (template is null)
            {
                throw new InvalidOperationException($"Template with ID {htmlDto.Id} does not exist.");
            }

            template.Name = htmlDto.Name;
            template.InputTypeName = htmlDto.InputTypeName;
            template.Html = htmlDto.Html;
            template.HeaderHtml = htmlDto.HeaderHtml;
            template.FooterHtml = htmlDto.FooterHtml;
            await ctx.SaveChangesAsync();

            return DocumentGenerationTemplateToDto(template);
        }

        private static DocumentGenerationTemplateDto DocumentGenerationTemplateToDto(DocumentGenerationTemplate template)
        {
            if (template is HtmlDocumentGenerationTemplate html)
            {
                return new HtmlDocumentGenerationTemplateDto()
                {
                    Id = html.Id,
                    Name = html.Name,
                    InputTypeName = html.InputTypeName,
                    Html = html.Html,
                    HeaderHtml = html.HeaderHtml,
                    FooterHtml = html.FooterHtml,
                };
            }

            throw new NotSupportedException($"Unsupported template type: {template.GetType().Name}.");
        }
    }
}
