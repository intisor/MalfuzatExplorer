namespace MalfuzatExplorer.Services;

/// <summary>
/// Represents a paragraph-sized chunk of text extracted from a PDF volume,
/// along with the embedding vector Gemini produced for it.
/// 
/// WHY A RECORD? Records are immutable value types — perfect for data
/// that is written once at index time and only read after that.
/// </summary>
public sealed record ChunkResult(
    string Volume,      // e.g. "Malfuzat-3"
    int Page,        // 1-based page number inside the PDF
    string Text,        // the raw chunk text (~300 words)
    float[] Embedding   // 768-dimensional vector from Gemini
);
