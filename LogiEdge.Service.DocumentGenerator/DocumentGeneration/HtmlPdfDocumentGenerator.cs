using LogiEdge.Service.DocumentGenerator.Data;
using System.Text.Json;
using Gotenberg.Sharp.API.Client;
using Gotenberg.Sharp.API.Client.Application.Builders;
using Gotenberg.Sharp.API.Client.Domain.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    internal class HtmlPdfDocumentGenerator<T>(IServiceProvider serviceProvider, HtmlDocumentGenerationTemplate template) : IDocumentGenerator<T>
    {
        private GotenbergSharpClient _gotenbergClient = serviceProvider.GetRequiredService<GotenbergSharpClient>();

        public async Task<byte[]> GenerateDocumentAsync(T input)
        {
            string dataJson = JsonSerializer.Serialize(input, new JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            string htmlContent = $"<script>const data = {dataJson};</script>{template.Html}";

            var builder = new HtmlRequestBuilder()
                .AddDocument(doc => doc.SetBody(htmlContent))
                .WithPageProperties(pp => pp.SetPaperSize(PaperSizes.A4));

            var result = await _gotenbergClient.HtmlToPdfAsync(builder);

            using MemoryStream ms = new();
            await result.CopyToAsync(ms);
            return ms.ToArray();
        }

        public DocumentGenerationTemplate Template => template;

        static Type IDocumentGenerator.TemplateType => typeof(HtmlDocumentGenerationTemplate);
    }
}
