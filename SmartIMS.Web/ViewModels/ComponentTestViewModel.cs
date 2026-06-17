using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewModels;

public sealed class ComponentTestViewModel
{
    public string SampleText { get; set; } = "元件測試文字";

    public required ListViewModel ListView { get; init; }

    public static ComponentTestViewModel Create()
    {
        return new ComponentTestViewModel
        {
            ListView = new ListViewModel
            {
                ListKey = ListViewRegistry.ComponentTestListKey,
                Title = "ListView 模擬資料",
                Columns = ListViewRegistry.CreateComponentTestColumns(),
                Rows =
                [
                    CreateRow("CMP-001", "Textbox", "輸入文字資料", "啟用"),
                    CreateRow("CMP-002", "Button", "執行操作按鈕", "啟用"),
                    CreateRow("CMP-003", "ListView", "顯示模擬清單資料", "啟用")
                ]
            }
        };
    }

    private static ListViewRow CreateRow(string code, string name, string description, string status)
    {
        return new ListViewRow
        {
            Values = new Dictionary<string, string>
            {
                ["Code"] = code,
                ["Name"] = name,
                ["Description"] = description,
                ["Status"] = status
            }
        };
    }
}
