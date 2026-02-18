using HtmlAgilityPack;
using Spire.Pdf;
using Spire.Pdf.General.Find;
using Spire.Pdf.Graphics;
using System.Drawing;
using System.Globalization;
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

            // Розбиваємо лише по комі.
            var keywordList = keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(k => k.Trim())
                                      .Where(k => !string.IsNullOrEmpty(k))
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

            PdfBrush brush = PdfBrushes.Red;

            // 1. ПІДГОТОВКА СЛІВ ДЛЯ ПОШУКУ
            // Замість пошуку цілих фраз, ми розбиваємо їх на слова-корені.
            // Це дозволяє знаходити слова навіть якщо вони розірвані перенесенням рядка.
            var searchTerms = new List<string>();

            foreach (var phrase in keywords)
            {
                // Розбиваємо фразу на окремі слова
                var words = phrase.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    string root = word.Trim();

                    // Застосовуємо ту саму логіку стемінгу (відрізання закінчень), що і в HTML
                    if (root.Length > 4)
                        root = root.Substring(0, root.Length - 2); // "Курсовий" -> "Курсов"
                    else if (root.Length > 3)
                        root = root.Substring(0, root.Length - 1); // "Мова" -> "Мов"

                    searchTerms.Add(root);
                }
            }

            // Прибираємо дублікати, щоб не шукати одне й те саме двічі
            var uniqueRoots = searchTerms.Distinct().ToList();

            foreach (PdfPageBase page in doc.Pages)
            {
                foreach (var term in uniqueRoots)
                {
                    // 2. ГЕНЕРАЦІЯ ВАРІАНТІВ РЕГІСТРУ
                    // Оскільки стара бібліотека погано ігнорує регістр для кирилиці,
                    // генеруємо варіанти вручну.
                    var lowerTerm = term.ToLower();
                    var variations = new List<string>
                    {
                        term,
                        lowerTerm,
                        term.ToUpper(),
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lowerTerm)
                    };

                    foreach (var variant in variations.Distinct())
                    {
                        try
                        {
                            // false, false = не точний збіг (шукає підрядок), не чутливий до регістру
                            // Це знайде "Курсов" всередині "Курсового"
                            var results = page.FindText(variant, false, false).Finds;

                            if (results == null) continue;

                            foreach (PdfTextFind find in results)
                            {
                                RectangleF rect = find.Bounds;

                                // Малюємо жирне підкреслення
                                float underlineHeight = 3.0f;
                                float yPosition = rect.Y + rect.Height - 3.0f;

                                page.Canvas.DrawRectangle(brush, rect.X, yPosition, rect.Width, underlineHeight);
                            }
                        }
                        catch
                        {
                            continue;
                        }
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

            // --- SMART REGEX ЛОГІКА ---
            var patternParts = keywords.Select(k =>
            {
                var words = k.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var wordPatterns = words.Select(word =>
                {
                    string root = word;
                    if (word.Length > 4) root = word.Substring(0, word.Length - 2);
                    else if (word.Length > 3) root = word.Substring(0, word.Length - 1);
                    return Regex.Escape(root) + @"\w*";
                });
                return string.Join(@"[\s\u00A0]+", wordPatterns);
            });

            string pattern = "(" + string.Join("|", patternParts) + ")";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var textNodes = doc.DocumentNode.SelectNodes("//text()");

            if (textNodes != null)
            {
                foreach (var node in textNodes)
                {
                    if (!regex.IsMatch(node.InnerText)) continue;
                    string newText = regex.Replace(node.InnerText, "<span style=\"background-color: yellow;\">$0</span>");
                    var newNode = HtmlNode.CreateNode(newText);
                    node.ParentNode.ReplaceChild(newNode, node);
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