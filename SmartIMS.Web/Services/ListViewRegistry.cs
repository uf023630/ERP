using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public static class ListViewRegistry
{
    public const string ComponentTestListKey = "component-test-listview";
    public const string MaterialLayoutTestListKey = "material-layout-test-listview";
    public const string VendorLayoutTestListKey = "vendor-layout-test-listview";

    public static IReadOnlyList<string> GetAllowedColumnKeys(string listKey)
    {
        if (string.Equals(listKey, ComponentTestListKey, StringComparison.OrdinalIgnoreCase))
        {
            return ComponentTestColumns.Select(column => column.Key).ToList();
        }

        if (string.Equals(listKey, MaterialLayoutTestListKey, StringComparison.OrdinalIgnoreCase))
        {
            return MaterialLayoutTestColumns.Select(column => column.Key).ToList();
        }

        if (string.Equals(listKey, VendorLayoutTestListKey, StringComparison.OrdinalIgnoreCase))
        {
            return VendorLayoutTestColumns.Select(column => column.Key).ToList();
        }

        return [];
    }

    public static IReadOnlyList<ListViewColumn> CreateComponentTestColumns()
    {
        return ComponentTestColumns
            .Select((column, index) => new ListViewColumn
            {
                Key = column.Key,
                Title = column.Title,
                DefaultVisible = true,
                DefaultWidth = column.DefaultWidth,
                MinWidth = column.MinWidth,
                Visible = true,
                Width = column.DefaultWidth,
                Order = index
            })
            .ToList();
    }

    public static IReadOnlyList<ListViewColumn> CreateMaterialLayoutTestColumns()
    {
        return MaterialLayoutTestColumns
            .Select((column, index) => new ListViewColumn
            {
                Key = column.Key,
                Title = column.Title,
                DefaultVisible = true,
                DefaultWidth = column.DefaultWidth,
                MinWidth = column.MinWidth,
                Visible = true,
                Width = column.DefaultWidth,
                Order = index
            })
            .ToList();
    }

    public static IReadOnlyList<ListViewColumn> CreateVendorLayoutTestColumns()
    {
        return VendorLayoutTestColumns
            .Select((column, index) => new ListViewColumn
            {
                Key = column.Key,
                Title = column.Title,
                DefaultVisible = true,
                DefaultWidth = column.DefaultWidth,
                MinWidth = column.MinWidth,
                Visible = true,
                Width = column.DefaultWidth,
                Order = index
            })
            .ToList();
    }

    private static readonly IReadOnlyList<ListViewColumnDefinition> ComponentTestColumns =
    [
        new("Code", "編號", 140, 80),
        new("Name", "名稱", 160, 80),
        new("Description", "說明", 360, 120),
        new("Status", "狀態", 120, 80)
    ];

    private static readonly IReadOnlyList<ListViewColumnDefinition> MaterialLayoutTestColumns =
    [
        new("MaterialCode", "材質編碼", 180, 100),
        new("MaterialName", "材質名稱", 260, 120)
    ];

    private static readonly IReadOnlyList<ListViewColumnDefinition> VendorLayoutTestColumns =
    [
        new("VendorCode", "廠商編號", 130, 90),
        new("VendorFullName", "廠商全名", 240, 140),
        new("TaxId", "統一編號", 120, 90),
        new("VendorShortName", "廠商簡稱", 150, 100),
        new("Category", "分類", 110, 80),
        new("Phone", "電話", 130, 90),
        new("Fax", "傳真", 130, 90),
        new("Email", "E_MAIL", 190, 120),
        new("Owner", "負責人", 120, 80),
        new("Contact", "聯絡人", 120, 80),
        new("PaymentMethod", "付款方式", 120, 90),
        new("BankName", "滙款銀行", 160, 100),
        new("BankAccount", "滙款帳號", 180, 120),
        new("Address", "地址", 280, 160),
        new("Note", "備註", 240, 120)
    ];

    private sealed record ListViewColumnDefinition(string Key, string Title, int DefaultWidth, int MinWidth);
}
