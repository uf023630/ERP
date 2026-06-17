namespace SmartIMS.Web.Models;

public sealed class ListViewModel
{
    public required string ListKey { get; init; }
    public required string Title { get; init; }
    public IReadOnlyList<ListViewColumn> Columns { get; init; } = [];
    public IReadOnlyList<ListViewRow> Rows { get; init; } = [];
}

public sealed class ListViewColumn
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public bool DefaultVisible { get; init; } = true;
    public int DefaultWidth { get; init; } = 140;
    public int MinWidth { get; init; } = 80;
    public bool Visible { get; set; } = true;
    public int Width { get; set; }
    public int Order { get; set; }
}

public sealed class ListViewRow
{
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();

    public string GetValue(string columnKey)
    {
        return Values.TryGetValue(columnKey, out var value) ? value : "";
    }
}

public sealed class ListViewSettingsDocument
{
    public IReadOnlyList<ListViewColumnSetting> Columns { get; init; } = [];
}

public sealed class ListViewColumnSetting
{
    public required string Key { get; init; }
    public bool Visible { get; init; }
    public int Width { get; init; }
    public int Order { get; init; }
}

public sealed class ListViewSettingsSaveRequest
{
    public string ListKey { get; init; } = "";
    public IReadOnlyList<ListViewColumnSetting> Columns { get; init; } = [];
}
