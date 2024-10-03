using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using MalfuzatExplorer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MalfuzatExplorer.Controllers
{
    public class HomeController : Controller
    {
        private string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Malfuzat", "Malfuzat-1.pdf");
       

        public IActionResult Index()
        {
            var model = new MalfuzatModel();
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Search(MalfuzatModel model)
        {
            if (string.IsNullOrEmpty(model.Query))
            {
                ModelState.AddModelError("", "Please enter a valid Search query.");
                return View("Index", model);
            }

            List<string> results = await SearchPdfForQueryAsync(model.Query);
            if (results.Count == 0)
            {
                model.Results = new List<string> { $"No results found for '{model.Query}'." };
            }
            else
            {
                model.Results = results;
            }

            for (int i = 0; i < results.Count; i++)
            {
                results[i] = await SpecialLanguageAsync(results[i], model.Query);
            }

            model.Results = results;
            return View("Index", model);
        }
        
        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<List<string>> SearchPdfForQueryAsync(string query)
        {
            List<string> results = new List<string>();
            try
            {
                if (!System.IO.File.Exists(pdfPath))
                {
                    throw new FileNotFoundException("PDF file not found.");
                }

                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfDocument document = new PdfDocument(reader))
                {
          

                    for (int i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                            string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
                            if (pageText.Contains(query, StringComparison.OrdinalIgnoreCase))
                            {
                                string result =  $"Found on leaf {i}: " + await GetContextAroundQueryAsync(pageText, query);
                               
                                   results.Add(result);
                            }
                    }
                }
            }
            catch (Exception ex) 
            {
                results.Add("An error occurred while searching the PDF.");
            }

            return results;
        }

        public async Task<string> GetContextAroundQueryAsync(string content, string query)
        {
            query = query.Trim();
            string[] words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Find the index of the query word in the words array
            int queryIndex = Array.FindIndex(words, w => w.Contains(query, StringComparison.OrdinalIgnoreCase));
            if (queryIndex < 0)
                return "Query not found";
            int start = Math.Max(0, queryIndex - 100);
            int end = Math.Min(words.Length, queryIndex + 100 + query.Length);
            string result = string.Join(" ", words.Skip(start).Take(end - start));
            //result = result.Replace("\n", "<br/>").Replace("\r", "");
            result = await HighlightQueryAsync(result, query);
            return result;
        }


        private async Task<string> HighlightQueryAsync(string text, string query)
        {
            return await Task.Run(() =>
                Regex.Replace(text, Regex.Escape(query), $"<mark>{query}</mark>", RegexOptions.IgnoreCase)
            );
        }


        public async Task<string> SpecialLanguageAsync(string result, string query)
        {
            result = await HighlightQueryAsync(result, query);
            string pattern = @"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+";
            string highlightedResult = Regex.Replace(result, pattern, match =>
            {
                return $"<span class=\"special\" dir=\"rtl\">{match.Value}</span>";
            });

            return highlightedResult;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
