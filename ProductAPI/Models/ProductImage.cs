using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models;

public class ProductImage
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(255)] public string FileName { get; set; } = "";
    [MaxLength(255)] public string ImageUrl { get; set; } = "";
    [MaxLength(255)] public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
