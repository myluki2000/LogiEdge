using LogiEdge.Service.DocumentGenerator.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    public interface IDocumentGenerator<TInput>: IDocumentGenerator
    {
        public byte[] GenerateDocument(TInput input);

        Type IDocumentGenerator.InputType => typeof(TInput);
    }

    public interface IDocumentGenerator
    {
        public abstract DocumentGenerationTemplate Template { get; }
        public abstract Type InputType { get; }
        public static virtual Type TemplateType => 
            throw new NotImplementedException("IDocumentGenerator.TemplateType needs to be implemented by all implementing classes!");
    }
}
