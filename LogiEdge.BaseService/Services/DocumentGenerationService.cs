using LogiEdge.BaseService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.BaseService.Services
{
    public class DocumentGenerationService
    {
        private readonly List<DocumentGenerationTemplate> documentGenerationTemplates = [];
    
        public DocumentGenerationService(SettingsService settingsService)
        {

        }

        public IEnumerable<DocumentGenerationTemplate<T>> GetTemplates<T>()
        {
            return documentGenerationTemplates.Where(t => t.InputType == typeof(T)).Cast<DocumentGenerationTemplate<T>>();
        }

        public void RegisterDocumentGenerationTemplate()
        {

        }
    }

    public class DocumentGenerationTemplate<T> : DocumentGenerationTemplate
    {
        public void GenerateDocument(T input)
        {
            
        }

        public override Type InputType => typeof(T);
    }

    public abstract class DocumentGenerationTemplate
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public abstract Type InputType { get; }
    }
}
