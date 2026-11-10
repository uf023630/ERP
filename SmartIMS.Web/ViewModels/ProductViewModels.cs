using System.ComponentModel.DataAnnotations;

namespace SmartIMS.Web.ViewModels;

public sealed class ProductPageViewModel
{
    public required IReadOnlyList<ProductItem> Products { get; init; }
    public required IReadOnlyList<ProductMaterialItem> Materials { get; init; }
}

public sealed class ProductItem
{
    public long? ProductId { get; set; }
    [Required, StringLength(50)]
    public string ProductCode { get; set; } = "";
    [Required, StringLength(200)]
    public string ProductName { get; set; } = "";
    public long? ProductMaterialId { get; set; }
    public string Material { get; set; } = "";
    [StringLength(50)]
    public string Thickness { get; set; } = "";
    [StringLength(50)]
    public string Length { get; set; } = "";
    [StringLength(50)]
    public string Width { get; set; } = "";
    [StringLength(500)]
    public string Spec { get; set; } = "";
    [StringLength(20)]
    public string Unit { get; set; } = "";
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }
    [StringLength(2000)]
    public string Note { get; set; } = "";
}

public sealed class ProductDeleteRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }
}
