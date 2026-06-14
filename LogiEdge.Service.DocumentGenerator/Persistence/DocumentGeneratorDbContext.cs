using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.Service.DocumentGenerator.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Service.DocumentGenerator.Persistence
{
    internal class DocumentGeneratorDbContext(DbContextOptions<DocumentGeneratorDbContext> options) : DbContext(options) 
    {
        public DbSet<DocumentGenerationTemplate> DocumentTemplates { get; set; }
        public DbSet<HtmlDocumentGenerationTemplate> HtmlTemplates { get; set; }
    }
}
