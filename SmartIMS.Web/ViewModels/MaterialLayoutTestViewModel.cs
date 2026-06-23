using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewModels;

public sealed class MaterialLayoutTestViewModel
{
    public required ListViewModel ListView { get; init; }
    public required IReadOnlyList<ProductMaterialItem> Materials { get; init; }

    public static MaterialLayoutTestViewModel Create(IReadOnlyList<ProductMaterialItem> materials)
    {
        return new MaterialLayoutTestViewModel
        {
            Materials = materials,
            ListView = new ListViewModel
            {
                ListKey = ListViewRegistry.MaterialLayoutTestListKey,
                Title = "商品材質清單",
                Columns = ListViewRegistry.CreateMaterialLayoutTestColumns(),
                Rows = materials.Select(CreateRow).ToList()
            }
        };
    }

    private static ListViewRow CreateRow(ProductMaterialItem material)
    {
        return new ListViewRow
        {
            Values = new Dictionary<string, string>
            {
                ["MaterialCode"] = material.MaterialCode,
                ["MaterialName"] = material.MaterialName
            }
        };
    }
}

public sealed record ProductMaterialItem(
    long ProductMaterialId,
    string MaterialCode,
    string MaterialName);

public sealed class ProductMaterialSaveRequest
{
    public long? ProductMaterialId { get; init; }
    public string MaterialCode { get; init; } = "";
    public string MaterialName { get; init; } = "";
}

public sealed class ProductMaterialDeleteRequest
{
    public long ProductMaterialId { get; init; }
}
