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
            // Extract everything
            var allChunks = _pdfIndexer.ExtractAndChunkPdf(pdfPath, volumeName);
            
            // TEST MODE: Grab only the first 10 chunks
            var chunks = allChunks.Take(10).ToList();
            
            Console.WriteLine($"[Indexer] TEST MODE: Processing {chunks.Count} chunks individually for {volumeName}...");

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                bool success = false;
                int retries = 0;

                while (!success && retries < 3)
                {
                    try
                    {
                        Console.WriteLine($"[Indexer] Embedding chunk {i + 1} of {chunks.Count}...");
                        
                        chunk.Vector = await _geminiService.EmbedAsync(chunk.Text, "RETRIEVAL_DOCUMENT");
                        
                        success = true;

                        if (i < chunks.Count - 1) 
                        {
                            await Task.Delay(4500); 
                        }
                    }
                    catch (Exception ex)
                    {
                        retries++;
                        Console.WriteLine($"[Error] Chunk {i + 1} failed. Details: {ex.Message}");
                        
                        if (retries >= 3)
                        {
                            Console.WriteLine($"[Fatal] Skipping chunk {i + 1} after 3 attempts.");
                        }
                        else
                        {
                            Console.WriteLine($"Retrying in {5 * retries} seconds... (Attempt {retries}/3)");
                            await Task.Delay(5000 * retries); 
                        }
                    }
                }
            }

            Console.WriteLine($"[Indexer] Finished embedding test batch for {volumeName}!");
            return chunks;
        }
    }
}
