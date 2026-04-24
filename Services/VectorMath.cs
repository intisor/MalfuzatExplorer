namespace MalfuzatExplorer.Services
{
    public static class VectorMath
    {
        // Computes how similar two arrays of numbers are (from -1.0 to 1.0)
        // 1.0 = Perfect exact match
        // 0.0 = Completely unrelated
        public static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must be the exact same length!");

            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = (float)Math.Sqrt(magnitudeA);
            magnitudeB = (float)Math.Sqrt(magnitudeB);

            // Avoid dividing by zero just in case!
            if (magnitudeA == 0 || magnitudeB == 0) return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }
}
