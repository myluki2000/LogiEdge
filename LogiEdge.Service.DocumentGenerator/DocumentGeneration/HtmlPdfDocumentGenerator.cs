using LogiEdge.Service.DocumentGenerator.Data;
using System.Text.Json;
using LogiEdge.Service.DocumentGenerator.Dto;
using Gotenberg.Sharp.API.Client;
using Gotenberg.Sharp.API.Client.Application.Builders;
using Gotenberg.Sharp.API.Client.Domain.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    internal class HtmlPdfDocumentGenerator<T>(IServiceProvider serviceProvider, HtmlDocumentGenerationTemplateDto template) : IDocumentGenerator<T>
    {
        private readonly string EMPTY_HTML = "<html></html>";

        private GotenbergSharpClient _gotenbergClient = serviceProvider.GetRequiredService<GotenbergSharpClient>();

        public async Task<byte[]> GenerateDocumentAsync(T input)
        {
            string dataJson = JsonSerializer.Serialize(input, new JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            string htmlContent = $"<script>const data = {dataJson};</script>{template.Html}";

            HtmlRequestBuilder builder = new HtmlRequestBuilder()
                .AddDocument(doc => doc
                    .SetBody(string.IsNullOrEmpty(htmlContent) ? EMPTY_HTML : htmlContent)
                    .SetFooter(string.IsNullOrEmpty(template.FooterHtml) ? EMPTY_HTML : template.FooterHtml)
                    .SetHeader(string.IsNullOrEmpty(template.HeaderHtml) ? EMPTY_HTML : template.HeaderHtml))
                .WithPageProperties(pp => pp.SetPaperSize(PaperSizes.A4));

            Stream result = await _gotenbergClient.HtmlToPdfAsync(builder);

            using MemoryStream ms = new();
            await result.CopyToAsync(ms);
            return ms.ToArray();
        }

        public DocumentGenerationTemplateDto Template => template;

        static Type IDocumentGenerator.TemplateType => typeof(HtmlDocumentGenerationTemplate);
    }
}
