using System.IO.Compression;
using System.Text;

namespace LogiEdge.Service.DocumentGenerator
{
    public static class TemplateZip
    {
        public const string BodyFileName = "body.html";
        public const string HeaderFileName = "header.html";
        public const string FooterFileName = "footer.html";
        public const string InputFormFileName = "input-form.html";

        public const long MaxUncompressedBytes = 10 * 1024 * 1024; // 10 MB cap for the sum of uncompressed entry lengths

        /// <summary>Builds the zip. headerHtml/footerHtml/inputFormHtml are included only when non-whitespace.</summary>
        public static byte[] CreateZip(string html, string? headerHtml, string? footerHtml, string? inputFormHtml)
        {
            using MemoryStream stream = new();
            using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, BodyFileName, html);
                AddEntry(archive, HeaderFileName, headerHtml);
                AddEntry(archive, FooterFileName, footerHtml);
                AddEntry(archive, InputFormFileName, inputFormHtml);
            }

            return stream.ToArray();
        }

        /// <summary>Parses a template zip. Entry names matched case-insensitively. Throws ArgumentException with a clear message if the data is not a valid zip, body.html is missing or empty, or total uncompressed size exceeds MaxUncompressedBytes.</summary>
        public static (string Html, string HeaderHtml, string FooterHtml, string? InputFormHtml) ParseZip(byte[] data)
        {
            using MemoryStream stream = new(data);
            ZipArchive archive;
            try
            {
                archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            }
            catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
            {
                throw new ArgumentException("The data is not a valid zip file.", nameof(data), exception);
            }

            using (archive)
            {
                long totalUncompressedBytes = archive.Entries.Sum(entry => entry.Length);
                if (totalUncompressedBytes > MaxUncompressedBytes)
                    throw new ArgumentException($"Total uncompressed size of the zip ({totalUncompressedBytes} bytes) must not exceed {MaxUncompressedBytes} bytes.", nameof(data));

                ZipArchiveEntry? bodyEntry = FindEntry(archive, BodyFileName);
                if (bodyEntry is null)
                    throw new ArgumentException($"The zip does not contain a required '{BodyFileName}' entry.", nameof(data));
                if (bodyEntry.Length == 0)
                    throw new ArgumentException($"The required '{BodyFileName}' entry is empty.", nameof(data));

                string html = ReadEntry(bodyEntry);
                string headerHtml = ReadEntry(FindEntry(archive, HeaderFileName));
                string footerHtml = ReadEntry(FindEntry(archive, FooterFileName));
                string? inputFormHtml = ReadEntryOrNull(FindEntry(archive, InputFormFileName));

                return (html, headerHtml, footerHtml, inputFormHtml);
            }
        }

        private static ZipArchiveEntry? FindEntry(ZipArchive archive, string fileName) =>
            archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, fileName, StringComparison.OrdinalIgnoreCase));

        private static void AddEntry(ZipArchive archive, string fileName, string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            ZipArchiveEntry entry = archive.CreateEntry(fileName);
            using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }

        private static string ReadEntry(ZipArchiveEntry? entry) =>
            entry is null || entry.Length == 0 ? string.Empty : ReadEntryContent(entry);

        private static string? ReadEntryOrNull(ZipArchiveEntry? entry) =>
            entry is null || entry.Length == 0 ? null : ReadEntryContent(entry);

        private static string ReadEntryContent(ZipArchiveEntry entry)
        {
            using StreamReader reader = new(entry.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
