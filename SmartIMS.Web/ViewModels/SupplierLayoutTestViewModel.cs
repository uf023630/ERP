using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewModels;

public sealed class SupplierLayoutTestViewModel
{
    public required ListViewModel ListView { get; init; }
    public required IReadOnlyList<SupplierLayoutTestItem> Suppliers { get; init; }

    public static SupplierLayoutTestViewModel Create()
    {
        var suppliers = new List<SupplierLayoutTestItem>
        {
            new("SUP-001", "宏達金屬材料有限公司", "宏達金屬", "24581234", "林志明", "02-2399-1200", "02-2399-1201", "台北市中正區重慶南路一段 10 號", "月結 30 天", "林怡君", "0912-345-678", "鋁材與不鏽鋼板主要供應商。"),
            new("SUP-002", "東海包材股份有限公司", "東海包材", "53770019", "陳柏翰", "04-2258-3300", "04-2258-3301", "台中市西屯區市政北二路 88 號", "匯款", "陳怡君", "0922-113-355", "交期需提前三天確認。"),
            new("SUP-003", "南港五金行", "南港五金", "80126655", "王建宏", "02-2786-5018", "02-2786-5019", "台北市南港區園區街 3 號", "現金", "王小姐", "0933-220-118", "急件可電話確認庫存。")
        };

        return new SupplierLayoutTestViewModel
        {
            Suppliers = suppliers,
            ListView = new ListViewModel
            {
                ListKey = ListViewRegistry.SupplierLayoutTestListKey,
                Title = "供應商清單",
                Columns = ListViewRegistry.CreateSupplierLayoutTestColumns(),
                Rows = suppliers.Select(CreateRow).ToList()
            }
        };
    }

    private static ListViewRow CreateRow(SupplierLayoutTestItem supplier)
    {
        return new ListViewRow
        {
            Values = new Dictionary<string, string>
            {
                ["SupplierCode"] = supplier.SupplierCode,
                ["SupplierName"] = supplier.SupplierName,
                ["ShortName"] = supplier.ShortName,
                ["TaxId"] = supplier.TaxId,
                ["Owner"] = supplier.Owner,
                ["CompanyPhone"] = supplier.CompanyPhone,
                ["CompanyFax"] = supplier.CompanyFax,
                ["CompanyAddress"] = supplier.CompanyAddress,
                ["PaymentMethod"] = supplier.PaymentMethod,
                ["ContactName"] = supplier.ContactName,
                ["ContactPhone"] = supplier.ContactPhone,
                ["Note"] = supplier.Note
            }
        };
    }
}

public sealed record SupplierLayoutTestItem(
    string SupplierCode,
    string SupplierName,
    string ShortName,
    string TaxId,
    string Owner,
    string CompanyPhone,
    string CompanyFax,
    string CompanyAddress,
    string PaymentMethod,
    string ContactName,
    string ContactPhone,
    string Note);
