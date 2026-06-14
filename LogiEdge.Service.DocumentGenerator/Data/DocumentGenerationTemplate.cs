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
        public string HeaderLeft { get; set; } = string.Empty;
        public string HeaderCenter { get; set; } = string.Empty;
        public string HeaderRight { get; set; } = string.Empty;
        public string FooterLeft { get; set; } = string.Empty;
        public string FooterCenter { get; set; } = string.Empty;
        public string FooterRight { get; set; } = string.Empty;
    }
}
