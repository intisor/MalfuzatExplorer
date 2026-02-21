using MalfuzatExplorer.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MalfuzatExplorer.Models
{
    public class MalfuzatModel
    {
        [Required]
        [MaxLength(200)]
        public string Query { get; set; } = string.Empty;

        public List<SearchResultDto> Results { get; set; } = new();

        public int PageNumber { get; set; } = 1;

        public int TotalResults { get; set; }
    }
}
