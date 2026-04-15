namespace MalfuzatExplorer.Services;

/// <summary>
/// Singleton in-memory vector store.
/// At startup: receives all embedded PDF chunks and stores them.
/// At search time: computes cosine similarity against the user's query vector
///                 and returns the top-N most semantically similar chunks.
/// </summary>
public sealed class VectorIndexService
{
    // ── Immutable snapshot: both fields travel together as one atomic reference ──
    private sealed record IndexState(IReadOnlyList<ChunkResult> Chunks, bool IsReady);
    private volatile IndexState _state = new([], false);

    public bool IsReady => _state.IsReady;
    public int Count  => _state.Chunks.Count;

    /// <summary>
    /// Called once at startup by PdfIndexingHostedService after all embeddings
    /// have been generated. Replaces the index atomically.
    /// </summary>
    public void Build(IReadOnlyList<ChunkResult> chunks)
    {
        // Single reference assignment — atomic in .NET. Readers will see
        // either the old state or the fully-built new state; never a mix.
        _state = new IndexState(chunks, IsReady: true);
    }

    /// <summary>
    /// Finds the top-N chunks most semantically similar to the given query embedding.
    /// 
    /// HOW IT WORKS:
    ///   1. For every stored chunk, compute cosine similarity between its embedding
    ///      and the query embedding. Score ranges from -1 to 1 (higher = more similar).
    ///   2. Sort all chunks by score descending.
    ///   3. Return the top N.
    /// </summary>
    public IReadOnlyList<ChunkResult> Search(float[] queryEmbedding, int topN = 10)
    {
        // Capture once so the state cannot change mid-search
        var state = _state;
        if (!state.IsReady || state.Chunks.Count == 0) return [];

        return state.Chunks
            .Select(chunk => (chunk, score: CosineSimilarity(queryEmbedding, chunk.Embedding)))
            .OrderByDescending(x => x.score)
            .Take(topN)
            .Select(x => x.chunk)
            .ToList();
    }

    /// <summary>
    /// Cosine similarity between two float vectors.
    /// 
    /// FORMULA: dot(A, B) / (|A| * |B|)
    /// 
    /// Because Gemini embeddings are unit-normalized, |A| and |B| are both ≈ 1,
    /// so this effectively reduces to just the dot product — but we compute the
    /// full formula for correctness with any embedding size.
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
