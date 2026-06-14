using LogiEdge.Service.DocumentGenerator.Data;
using LogiEdge.Service.DocumentGenerator.Services;
using System;
using System.Collections.Generic;
using System.Text;
using WkHtmlToPdfDotNet;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    internal class HtmlPdfDocumentGenerator<T>(HtmlDocumentGenerationTemplate template) : HtmlPdfDocumentGeneratorBase, IDocumentGenerator<T>
    {
        public byte[] GenerateDocument(T input)
        {
            HtmlToPdfDocument doc = new()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                },
                Objects =
                {
                    new ObjectSettings()
                    {
                        HtmlContent = template.Html,
                        WebSettings = { DefaultEncoding = "utf-8", },
                        HeaderSettings =
                        {
                            Left = template.HeaderLeft,
                            Center = template.HeaderCenter,
                            Right = template.HeaderRight,
                        },
                        FooterSettings =
                        {
                            Left = template.FooterLeft,
                            Center = template.FooterCenter,
                            Right = template.FooterRight,
                        }
                    }
                }
            };

            return Converter.Convert(doc);
        }

        public DocumentGenerationTemplate Template => template;

        static Type IDocumentGenerator.TemplateType => typeof(HtmlDocumentGenerationTemplate);
    }

    /// <summary>
    /// Base class needed for the static Converter field, which needs to be shared between all HtmlPdfDocumentGenerator
    /// instances to avoid multiple instances of the converter being created.
    /// </summary>
    internal abstract class HtmlPdfDocumentGeneratorBase
    {
        protected static readonly SynchronizedConverter Converter = new(new PdfTools());
    }
}
