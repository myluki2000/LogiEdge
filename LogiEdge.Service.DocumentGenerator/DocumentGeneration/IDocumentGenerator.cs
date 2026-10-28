using LogiEdge.Service.DocumentGenerator.Data;
using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.Service.DocumentGenerator.Dto;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    public interface IDocumentGenerator<TInput>: IDocumentGenerator
    {
        public Task<byte[]> GenerateDocumentAsync(TInput input, string? userInputJson);

        Type IDocumentGenerator.InputType => typeof(TInput);
    }

    public interface IDocumentGenerator
    {
        public abstract DocumentGenerationTemplateDto Template { get; }
        public abstract Type InputType { get; }
        public static virtual Type TemplateType => 
            throw new NotImplementedException("IDocumentGenerator.TemplateType needs to be implemented by all implementing classes!");
    }
}
