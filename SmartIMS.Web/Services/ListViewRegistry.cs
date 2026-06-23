using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public static class ListViewRegistry
{
    public const string ComponentTestListKey = "component-test-listview";
    public const string MaterialLayoutTestListKey = "material-layout-test-listview";

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

    private sealed record ListViewColumnDefinition(string Key, string Title, int DefaultWidth, int MinWidth);
}
