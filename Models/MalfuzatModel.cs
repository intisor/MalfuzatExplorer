namespace MalfuzatExplorer.Models
{
    public class MalfuzatModel
    {
        public string? Query { get; set; }
        public List<string> Results { get; set; } = new List<string>();
        public int PageNumber { get; set; }

        // ── Semantic search additions ────────────────────────────────────
        // True when the user wants semantic (vector) search instead of keyword
        public bool SemanticMode { get; set; }

        // Set by the controller so the view knows whether to show the "indexing" banner
        public bool IndexReady { get; set; }

        // Semantic search returns richer objects (volume, page, snippet)
        // Keyword search keeps using the existing string Results list
        public List<SemanticResult> SemanticResults { get; set; } = [];
    }

    /// <summary>
    /// Represents one result from the semantic/vector search.
    /// Richer than a plain string — carries volume name and page number.
    /// </summary>
    public class SemanticResult
    {
        public string Volume { get; init; } = "";
        public int Page { get; init; }
        public string Snippet { get; init; } = "";
    }
}
