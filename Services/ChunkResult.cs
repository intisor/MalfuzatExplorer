namespace MalfuzatExplorer.Services
{
    public class ChunkResult
    {
        // Example: "Malfuzat-1.pdf"
        public string Volume { get; set; } = string.Empty; 

        // The page number this text was found on
        public int Page { get; set; } 

        // The actual extracted piece of text
        public string Text { get; set; } = string.Empty; 

        // The 256-dimensional mathematical representation of the text
        public float[] Vector { get; set; } = Array.Empty<float>();
    }
}
