using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiEdge.WarehouseService.Data
{
    public class Item : IDisposable
    {
        public ItemId Id { get; set; }

        public JsonDocument? AdditionalProperties { get; set; }

        public string Comments { get; set; } = string.Empty;

        public required List<ItemState> ItemStates;

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }

        public static void CreateModel(ModelBuilder modelBuilder)
        {
            EntityTypeBuilder<Item> entityBuilder = modelBuilder.Entity<Item>();

            entityBuilder.OwnsOne(x => x.Id, p =>
            {
                PropertyInfo[] properties = p.OwnedEntityType.ClrType.GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    Type propertyType = property.PropertyType;

                    p.Property(propertyType, property.Name).HasColumnName(property.Name);

                    entityBuilder.Property(propertyType, "Key_" + property.Name).HasColumnName(property.Name);
                }

                entityBuilder.HasKey(properties.Select(x => "Key_" + x.Name).ToArray());
            });
        }
    }
}
