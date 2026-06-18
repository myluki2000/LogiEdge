using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.DocumentGenerator.Data
{
    public abstract class DocumentGenerationTemplate
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string InputTypeName { get; set; }
    }

    public class HtmlDocumentGenerationTemplate : DocumentGenerationTemplate
    {
        public required string Html { get; set; }
        public string HeaderHtml { get; set; } = string.Empty;
        public string FooterHtml { get; set; } = string.Empty;
    }
}
