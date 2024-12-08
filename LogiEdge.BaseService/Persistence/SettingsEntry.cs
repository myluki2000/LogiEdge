using System.ComponentModel.DataAnnotations;

namespace LogiEdge.BaseService.Persistence
{
    public class SettingsEntry
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }

        public SettingsEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
