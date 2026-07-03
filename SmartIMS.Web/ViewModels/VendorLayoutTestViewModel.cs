using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewModels;

public sealed class VendorLayoutTestViewModel
{
    public required ListViewModel ListView { get; init; }
    public required IReadOnlyList<VendorLayoutTestItem> Vendors { get; init; }

    public static VendorLayoutTestViewModel Create()
    {
        var vendors = new List<VendorLayoutTestItem>
        {
            new(
                "V-001",
                "和德製網有限公司",
                "24581234",
                "和德製網",
                "原料供應商",
                "02-2399-1200",
                "02-2399-1201",
                "purchase@hode.example",
                "張志明",
                "林怡君",
                "月結30天",
                "華南銀行 台北分行",
                "008-123456789012",
                "台北市中正區重慶南路一段 10 號",
                "常用鋁材供應商。"),
            new(
                "V-002",
                "東成金屬材料股份有限公司",
                "53770019",
                "東成金屬",
                "金屬板材",
                "04-2258-3300",
                "04-2258-3301",
                "sales@dongcheng.example",
                "陳信宏",
                "王雅婷",
                "匯款",
                "第一銀行 台中分行",
                "007-556677889900",
                "台中市西屯區市政北二路 88 號",
                "下單前需確認當日報價。"),
            new(
                "V-003",
                "南港包裝材料行",
                "80126655",
                "南港包材",
                "包材耗材",
                "02-2786-5018",
                "02-2786-5019",
                "order@nangang-pack.example",
                "黃柏翰",
                "李佳蓉",
                "月結",
                "玉山銀行 南港分行",
                "808-991122334455",
                "台北市南港區園區街 3 號",
                "急件可當日配送。")
        };

        return new VendorLayoutTestViewModel
        {
            Vendors = vendors,
            ListView = new ListViewModel
            {
                ListKey = ListViewRegistry.VendorLayoutTestListKey,
                Title = "廠商清單",
                Columns = ListViewRegistry.CreateVendorLayoutTestColumns(),
                Rows = vendors.Select(CreateRow).ToList()
            }
        };
    }

    private static ListViewRow CreateRow(VendorLayoutTestItem vendor)
    {
        return new ListViewRow
        {
            Values = new Dictionary<string, string>
            {
                ["VendorCode"] = vendor.VendorCode,
                ["VendorFullName"] = vendor.VendorFullName,
                ["TaxId"] = vendor.TaxId,
                ["VendorShortName"] = vendor.VendorShortName,
                ["Category"] = vendor.Category,
                ["Phone"] = vendor.Phone,
                ["Fax"] = vendor.Fax,
                ["Email"] = vendor.Email,
                ["Owner"] = vendor.Owner,
                ["Contact"] = vendor.Contact,
                ["PaymentMethod"] = vendor.PaymentMethod,
                ["BankName"] = vendor.BankName,
                ["BankAccount"] = vendor.BankAccount,
                ["Address"] = vendor.Address,
                ["Note"] = vendor.Note
            }
        };
    }
}

public sealed record VendorLayoutTestItem(
    string VendorCode,
    string VendorFullName,
    string TaxId,
    string VendorShortName,
    string Category,
    string Phone,
    string Fax,
    string Email,
    string Owner,
    string Contact,
    string PaymentMethod,
    string BankName,
    string BankAccount,
    string Address,
    string Note);
