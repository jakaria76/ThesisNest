using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace ThesisNest.Services
{
    public class FileTextExtractor : IFileTextExtractor
    {
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (ext == ".pdf")
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                var sb = new StringBuilder();
                using (var pdf = PdfDocument.Open(ms))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }

                return sb.ToString();
            }
            else if (ext == ".docx")
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;
                using var wordDoc = WordprocessingDocument.Open(ms, false);
                var body = wordDoc.MainDocumentPart?.Document.Body;
                return body?.InnerText ?? "";
            }
            else // .txt বা fallback
            {
                using var sr = new StreamReader(file.OpenReadStream());
                return await sr.ReadToEndAsync();
            }
        }
    }
}
