namespace MalfuzatExplorer.Models
{
    public class MalfuzatModel
    {
        public string Query {  get; set; }
        public List<string> Results {get; set; } = new List<string>();
        public int PageNumber { get; set; }
    }
}
