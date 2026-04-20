using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MalfuzatExplorer.Services
{
    public class MalfuzatIndexerService
    {
        private readonly PdfIndexer _pdfIndexer;
        private readonly GeminiEmbeddingService _geminiService;

        public MalfuzatIndexerService(PdfIndexer pdfIndexer, GeminiEmbeddingService geminiService)
        {
            _pdfIndexer = pdfIndexer;
            _geminiService = geminiService;
        }

        public async Task<List<ChunkResult>> ProcessAndEmbedBookAsync(string pdfPath, string volumeName)
        {
            var chunks = _pdfIndexer.ExtractAndChunkPdf(pdfPath, volumeName);
            Console.WriteLine($"[Indexer] Processing {chunks.Count} chunks for {volumeName}...");

            const int batchSize = 50; // We'll do 50 at a time to be safe
            for (int i = 0; i < chunks.Count; i += batchSize)
            {
                var batch = chunks.Skip(i).Take(batchSize).ToList();
                var texts = batch.Select(c => c.Text).ToList();

                try
                {
                    Console.WriteLine($"[Indexer] Embedding batch {i / batchSize + 1} of {Math.Ceiling((double)chunks.Count / batchSize)}...");
                    var vectors = await _geminiService.BatchEmbedAsync(texts);

                    for (int j = 0; j < batch.Count; j++)
                    {
                        batch[j].Vector = vectors[j];
                    }

                    // RATE LIMITING: Wait 1 second between batches
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Batch failed: {ex.Message}");
                }
            }

            return chunks;
        }
    }
}
