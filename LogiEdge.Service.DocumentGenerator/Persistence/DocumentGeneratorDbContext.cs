using LogiEdge.Service.DocumentGenerator.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.Service.DocumentGenerator.Persistence
{
    internal class DocumentGeneratorDbContext(DbContextOptions<DocumentGeneratorDbContext> options) : LogiEdgeDbContext(options) 
    {
        public DbSet<DocumentGenerationTemplate> DocumentTemplates { get; set; }
        public DbSet<HtmlDocumentGenerationTemplate> HtmlTemplates { get; set; }
    }
}
