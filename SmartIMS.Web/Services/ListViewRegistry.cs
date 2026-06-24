using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public static class ListViewRegistry
{
    public const string ComponentTestListKey = "component-test-listview";
    public const string MaterialLayoutTestListKey = "material-layout-test-listview";
    public const string SupplierLayoutTestListKey = "supplier-layout-test-listview";

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

        if (string.Equals(listKey, SupplierLayoutTestListKey, StringComparison.OrdinalIgnoreCase))
        {
            return SupplierLayoutTestColumns.Select(column => column.Key).ToList();
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

    public static IReadOnlyList<ListViewColumn> CreateSupplierLayoutTestColumns()
    {
        return SupplierLayoutTestColumns
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

    private static readonly IReadOnlyList<ListViewColumnDefinition> SupplierLayoutTestColumns =
    [
        new("SupplierCode", "供應商編號", 150, 100),
        new("SupplierName", "供應商名稱", 220, 120),
        new("ShortName", "供應商簡稱", 150, 100),
        new("TaxId", "統一編號", 130, 90),
        new("Owner", "負責人", 120, 80),
        new("CompanyPhone", "公司電話", 140, 100),
        new("CompanyFax", "公司傳真", 140, 100),
        new("CompanyAddress", "公司地址", 280, 160),
        new("PaymentMethod", "付款方式", 130, 90),
        new("ContactName", "聯絡人", 120, 80),
        new("ContactPhone", "聯絡人電話", 150, 100),
        new("Note", "備註", 240, 120)
    ];

    private sealed record ListViewColumnDefinition(string Key, string Title, int DefaultWidth, int MinWidth);
}
