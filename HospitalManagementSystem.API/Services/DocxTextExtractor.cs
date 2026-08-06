using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace HospitalManagementSystem.API.Services
{
    /// <summary>Reads plain text out of a .docx file, one line per paragraph.</summary>
    public static class DocxTextExtractor
    {
        public static List<string> ExtractParagraphs(string filePath)
        {
            var paragraphs = new List<string>();

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return paragraphs;

            foreach (var p in body.Elements<Paragraph>())
            {
                var text = string.Concat(p.Descendants<Text>().Select(t => t.Text)).Trim();
                if (!string.IsNullOrEmpty(text))
                    paragraphs.Add(text);
            }

            return paragraphs;
        }

        public static string ExtractPlainText(string filePath)
        {
            return string.Join("\n", ExtractParagraphs(filePath));
        }
    }
}
