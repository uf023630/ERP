namespace SmartIMS.Web.Models;

public sealed record SystemSkin(string SkinKey)
{
    public const string DefaultSkinKey = "classic-gray";

    public static IReadOnlyList<SystemSkinOption> AvailableSkins { get; } =
    [
        new("classic-gray", "經典灰階", "熟悉的內部作業系統介面，低干擾、穩定耐看。"),
        new("steel-blue", "鋼藍效率", "冷靜藍灰與高對比表格，適合大量資料掃描。"),
        new("forest-ops", "倉儲綠", "低飽和綠色營運感，適合庫存與現場作業。"),
        new("amber-ledger", "琥珀帳務", "溫和財務色調，適合採購、銷售與帳務流程。"),
        new("charcoal-pro", "深色專業", "深色高對比介面，適合長時間工作與低光環境。")
    ];

    public static SystemSkin Create(string? skinKey)
    {
        var option = AvailableSkins.FirstOrDefault(skin => string.Equals(skin.Key, skinKey, StringComparison.OrdinalIgnoreCase));
        return new(option?.Key ?? DefaultSkinKey);
    }

    public static bool IsValidSkinKey(string? skinKey)
    {
        return AvailableSkins.Any(skin => string.Equals(skin.Key, skinKey, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record SystemSkinOption(string Key, string Name, string Description);
