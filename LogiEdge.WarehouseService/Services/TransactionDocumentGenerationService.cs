using LogiEdge.BaseService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Services
{
    public class TransactionDocumentGenerationService
    {
        private readonly List<DocumentGenerationTemplate> documentGenerationTemplates = [];
    
        public TransactionDocumentGenerationService(SettingsService settingsService)
        {

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
        public abstract Type InputType { get; }
    }
}
