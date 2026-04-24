using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MalfuzatExplorer.Services
{
    public class MalfuzatSearchService
    {
        private readonly GeminiEmbeddingService _geminiService;
        
        // This is our "Database" for now (In memory for this demo)
        private List<ChunkResult> _knowledgeBase = new();

        public MalfuzatSearchService(GeminiEmbeddingService geminiService)
        {
            _geminiService = geminiService;
        }

        // 1. A way to "Load" the embedded books into memory
        public void Initialise(List<ChunkResult> embeddedChunks)
        {
            _knowledgeBase = embeddedChunks;
        }

        public bool IsLoaded => _knowledgeBase.Any();

        // 2. The Magic Search Method
        public async Task<List<(ChunkResult Chunk, float Score)>> SearchAsync(string query, int topK = 5)
        {
            if (!_knowledgeBase.Any()) return new List<(ChunkResult, float)>();

            // A. Turn the user's question into a Vector (taskType = RETRIEVAL_QUERY)
            var queryVector = await _geminiService.EmbedAsync(query, "RETRIEVAL_QUERY");

            // B. Compare the query against EVERYTHING in our knowledge base
            var results = new List<(ChunkResult, float)>();

            foreach (var chunk in _knowledgeBase)
            {
                // Skip chunks that don't have vectors (e.g. if indexing failed for them)
                if (chunk.Vector == null || chunk.Vector.Length != queryVector.Length)
                    continue;

                // The Cosine Similarity math we learned in Phase 1!
                float similarity = VectorMath.CosineSimilarity(queryVector, chunk.Vector);
                results.Add((chunk, similarity));
            }

            // C. Sort by the highest score and take the top 5
            return results
                .OrderByDescending(r => r.Item2)
                .Take(topK)
                .ToList();
        }
    }
}
