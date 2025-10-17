using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static LogiEdge.WarehouseService.Data.ItemSchemaProperty;

namespace LogiEdge.WarehouseService.Data
{
    [Owned]
    public class ItemSchemaProperty
    {
        [MaxLength(64)]
        [Key]
        public required string Name { get; set; } = string.Empty;
        [MaxLength(256)]
        public required SupportedTypes Type { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsUnique { get; set; } = false;

        public enum SupportedTypes
        {
            String,
            Int32,
            Single,
            Boolean,
            DateTime,
        }
    }

    public static class SupportedTypesExtensionMethods
    {
        public static Type ToType(this SupportedTypes type)
        {
            return type switch
            {
                SupportedTypes.String => typeof(string),
                SupportedTypes.Int32 => typeof(int),
                SupportedTypes.Single => typeof(float),
                SupportedTypes.Boolean => typeof(bool),
                SupportedTypes.DateTime => typeof(DateTime),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
