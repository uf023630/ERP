using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public static class ListViewRegistry
{
    public const string ComponentTestListKey = "component-test-listview";

    public static IReadOnlyList<string> GetAllowedColumnKeys(string listKey)
    {
        return string.Equals(listKey, ComponentTestListKey, StringComparison.OrdinalIgnoreCase)
            ? ComponentTestColumns.Select(column => column.Key).ToList()
            : [];
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

    private static readonly IReadOnlyList<ListViewColumnDefinition> ComponentTestColumns =
    [
        new("Code", "編號", 140, 80),
        new("Name", "名稱", 160, 80),
        new("Description", "說明", 360, 120),
        new("Status", "狀態", 120, 80)
    ];

    private sealed record ListViewColumnDefinition(string Key, string Title, int DefaultWidth, int MinWidth);
}
