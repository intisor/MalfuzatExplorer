using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace MalfuzatExplorer.Services
{
    public class PdfIndexer
    {
        // 1. A method to read the PDF
        public List<ChunkResult> ExtractAndChunkPdf(string filePath, string volumeName)
        {
            var chunks = new List<ChunkResult>();

            using var pdfDoc = new PdfDocument(new PdfReader(filePath));
            int numPages = pdfDoc.GetNumberOfPages();

            // Loop through every single page
            for (int i = 1; i <= numPages; i++)
            {
                var page = pdfDoc.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page);

                if (string.IsNullOrWhiteSpace(text)) continue;

                // Added logging so we can see the extraction happening!
                Console.WriteLine($"[PdfIndexer] Page {i} extracted. Content starting with: \"{(text.Length > 40 ? text.Substring(0, 40) : text)}...\"");

                // 2. Pass the page's text into our chunking machine
                var pageChunks = ChunkText(text, volumeName, i);
                chunks.AddRange(pageChunks);
            }

            return chunks;
        }

        // 3. The Overlapping Chunking Machine
        private List<ChunkResult> ChunkText(string rawText, string volume, int pageNum)
        {
            var results = new List<ChunkResult>();
            int chunkSize = 400; // Characters per chunk
            int overlap = 150;   // The amount of characters to overlap
            int step = chunkSize - overlap; // How far to move our window forward (250)

            for (int i = 0; i < rawText.Length; i += step)
            {
                // Grab our slice. If we are near the end, just grab whatever is left.
                int remainingLength = rawText.Length - i;
                int currentChunkSize = Math.Min(chunkSize, remainingLength);
                
                string chunkText = rawText.Substring(i, currentChunkSize).Trim();

                // Only save chunks that actually have meaningful content
                if (chunkText.Length > 20)
                {
                    results.Add(new ChunkResult
                    {
                        Volume = volume,
                        Page = pageNum,
                        Text = chunkText
                    });
                }
            }

            return results;
        }
    }
}
