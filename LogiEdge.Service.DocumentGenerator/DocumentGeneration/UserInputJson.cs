using System.Text;
using System.Text.Json;

namespace LogiEdge.Service.DocumentGenerator.DocumentGeneration
{
    public static class UserInputJson
    {
        public const int MaxByteLength = 1024 * 1024; // 1 MB cap for collected form values

        /// <summary>Throws ArgumentException if json is not null and is not a JSON object or exceeds MaxByteLength UTF-8 bytes.</summary>
        public static void Validate(string? json)
        {
            if (json is null) return;

            if (Encoding.UTF8.GetByteCount(json) > MaxByteLength)
                throw new ArgumentException($"userInput JSON must not exceed {MaxByteLength} UTF-8 bytes.", nameof(json));

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("userInput JSON must be a JSON object, not an array or scalar.", nameof(json));
            }
            catch (JsonException exception)
            {
                throw new ArgumentException($"userInput JSON is not valid JSON: {exception.Message}", nameof(json), exception);
            }
        }

        /// <summary>Escapes json for safe injection into a &lt;script&gt; block (the </script> hazard).</summary>
        public static string EscapeForScriptInjection(string json) => json.Replace("<", "\u003c");
    }
}
