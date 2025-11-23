using HtmlAgilityPack;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Texts;
using System.Drawing;
using System.Text.RegularExpressions;
using WorkWithDocuments.Services.Interfaces;

namespace WorkWithDocuments.Services
{
    public class FileProcessingService : IFileProcessingService
    {
        public byte[] ProcessFile(Stream inputStream, string keywords, string fileExtension)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentException("Keywords cannot be empty");

            var keywordList = keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(k => k.Trim())
                                      .ToArray();

            return fileExtension.ToLower() switch
            {
                ".pdf" => ProcessPdf(inputStream, keywordList),
                ".html" => ProcessHtml(inputStream, keywordList),
                _ => throw new NotSupportedException("Format not supported")
            };
        }

        private byte[] ProcessPdf(Stream stream, string[] keywords)
        {
            
            PdfDocument doc = new PdfDocument();
            doc.LoadFromStream(stream);

            foreach (PdfPageBase page in doc.Pages)
            {
                PdfTextFinder finder = new PdfTextFinder(page);

                finder.Options.Parameter = TextFindParameter.IgnoreCase;

                foreach (var word in keywords)
                {
                    List<PdfTextFragment> results = finder.Find(word);

                    foreach (PdfTextFragment text in results)
                    {
                        text.HighLight(Color.Yellow);
                    }
                }
            }

            using (MemoryStream outputStream = new MemoryStream())
            {
                doc.SaveToStream(outputStream);
                doc.Close();
                return outputStream.ToArray();
            }
        }

        private byte[] ProcessHtml(Stream stream, string[] keywords)
        {
            var doc = new HtmlDocument();
            doc.Load(stream);

            foreach (var word in keywords)
            {
                var textNodes = doc.DocumentNode.SelectNodes("//text()");

                if (textNodes != null)
                {
                    foreach (var node in textNodes)
                    {
                        if (node.InnerText.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            string pattern = Regex.Escape(word);
                            string replacement = $"<span style=\"background-color: yellow;\">{word}</span>";

                            var newNodeHtml = Regex.Replace(node.InnerText, pattern, replacement, RegexOptions.IgnoreCase);
                            var newNode = HtmlNode.CreateNode(newNodeHtml);
                            node.ParentNode.ReplaceChild(newNode, node);
                        }
                    }
                }
            }

            using (MemoryStream outputStream = new MemoryStream())
            {
                doc.Save(outputStream);
                return outputStream.ToArray();
            }
        }
    }
}