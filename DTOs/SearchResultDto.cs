namespace MalfuzatExplorer.DTOs
{
    /// <summary>
    /// Strongly-typed search result DTO.
    /// Inspired by AmsaAPI's DTOs/ pattern — replaces the raw List&lt;string&gt;
    /// that previously leaked presentation concerns into the search pipeline.
    /// </summary>
    public class SearchResultDto
    {
        /// <summary>Volume name, e.g. "Malfuzat-1"</summary>
        public string Volume { get; set; } = string.Empty;

        /// <summary>1-based page/leaf number within the volume</summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// HTML-safe snippet with &lt;mark&gt; highlights and RTL spans injected.
        /// Already HTML-encoded at construction time — safe for @Html.Raw().
        /// </summary>
        public string Snippet { get; set; } = string.Empty;
    }
}
