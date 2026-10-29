using System;

namespace LogiEdge.Service.DocumentGenerator.Dto
{
    public abstract class DocumentGenerationTemplateDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string InputTypeName { get; set; }
    }

    public class HtmlDocumentGenerationTemplateDto : DocumentGenerationTemplateDto
    {
        public required string Html { get; set; }
        public string HeaderHtml { get; set; } = string.Empty;
        public string FooterHtml { get; set; } = string.Empty;
        public string? InputFormHtml { get; set; }
    }
}
