using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewModels;

public sealed class MaterialLayoutTestViewModel
{
    public required ListViewModel ListView { get; init; }

    public static MaterialLayoutTestViewModel Create()
    {
        return new MaterialLayoutTestViewModel
        {
            ListView = new ListViewModel
            {
                ListKey = ListViewRegistry.MaterialLayoutTestListKey,
                Title = "商品材質清單",
                Columns = ListViewRegistry.CreateMaterialLayoutTestColumns(),
                Rows =
                [
                    CreateRow("MAT-001", "鋁合金"),
                    CreateRow("MAT-002", "不鏽鋼"),
                    CreateRow("MAT-003", "黑鐵"),
                    CreateRow("MAT-004", "銅")
                ]
            }
        };
    }

    private static ListViewRow CreateRow(string materialCode, string materialName)
    {
        return new ListViewRow
        {
            Values = new Dictionary<string, string>
            {
                ["MaterialCode"] = materialCode,
                ["MaterialName"] = materialName
            }
        };
    }
}
