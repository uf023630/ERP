using System.ComponentModel.DataAnnotations;

namespace SmartIMS.Web.ViewModels;

public static class SalesOrderStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Voided = "Voided";

    public static string GetLabel(string status) => status switch
    {
        Shipped => "已出貨",
        Voided => "已作廢",
        Confirmed => "已確認",
        _ => status
    };
}

public static class SalesOrderTaxTypes
{
    public const string None = "None";
    public const string Inclusive = "Inclusive";
    public const string Exclusive = "Exclusive";

    public static string GetLabel(string taxType) => taxType switch
    {
        None => "不計稅",
        Inclusive => "含稅",
        _ => "稅外加"
    };
}

public sealed class SalesOrderListViewModel
{
    public IReadOnlyList<SalesOrderListItem> SalesOrders { get; set; } = [];

    public long? SelectedSalesOrderId { get; set; }

    public SalesOrderDetailViewModel? SelectedSalesOrder { get; set; }
}

public sealed record SalesOrderListItem(
    long SalesOrderId,
    string OrderNo,
    DateTime OrderDate,
    DateTime RequiredDate,
    string CustomerCode,
    string CustomerName,
    string TaxType,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? SourceQuoteNo,
    string ProductNames);

public sealed class SalesOrderDetailViewModel
{
    public bool HasShippedItems { get; set; }
    public long SalesOrderId { get; set; }

    public string OrderNo { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public DateTime RequiredDate { get; set; }

    public string CustomerCode { get; set; } = "";

    public string CustomerName { get; set; } = "";

    public string? CustomerTaxId { get; set; }

    public string? CustomerContactName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerAddress { get; set; }

    public string TaxType { get; set; } = SalesOrderTaxTypes.Exclusive;

    public decimal TaxRate { get; set; }

    public bool IsCustomerSuppliedMaterial { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = SalesOrderStatuses.Confirmed;

    public string? SourceQuoteNo { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? PdfFileName { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? PdfStoragePath { get; set; }

    public string? Remark { get; set; }

    public IReadOnlyList<SalesOrderDetailLine> Lines { get; set; } = [];
}

public sealed record SalesOrderDetailLine(
    int LineNumber,
    long? ProductId,
    string ItemSource,
    string? ProductCode,
    string ProductName,
    string? Thickness,
    string? Width,
    string? Length,
    string? Specification,
    string? Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineAmount,
    string? Remark);

public sealed class SalesOrderEditViewModel
{
    public long? SalesOrderId { get; set; }

    public string? OrderNo { get; set; }

    [Required(ErrorMessage = "請選擇客戶")]
    [Display(Name = "客戶")]
    public long? CustomerId { get; set; }

    [Required(ErrorMessage = "請輸入訂單日期")]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "請輸入預交日期")]
    [DataType(DataType.Date)]
    public DateTime RequiredDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    public string TaxType { get; set; } = SalesOrderTaxTypes.Exclusive;

    [Range(typeof(decimal), "0", "100", ErrorMessage = "稅率必須介於 0 到 100")]
    public decimal TaxRate { get; set; } = 5m;

    public bool IsCustomerSuppliedMaterial { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = SalesOrderStatuses.Confirmed;

    public string? SourceQuoteNo { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? PdfFileName { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? PdfStoragePath { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public List<SalesOrderLineInputModel> Lines { get; set; } = [];

    public IReadOnlyList<SalesOrderCustomerOption> Customers { get; set; } = [];

    public IReadOnlyList<SalesOrderProductOption> Products { get; set; } = [];

    public bool HasShippedItems { get; set; }
    public bool IsEditable => Status == SalesOrderStatuses.Confirmed && !HasShippedItems;
}

public sealed class SalesOrderLineInputModel
{
    public long? SalesOrderItemId { get; set; }

    public long? ProductId { get; set; }

    public string ItemSource { get; set; } = "Manual";

    [StringLength(50)]
    public string? ProductCode { get; set; }

    [Required(ErrorMessage = "請輸入商品名稱")]
    [StringLength(200)]
    public string ProductName { get; set; } = "";

    [StringLength(50)]
    public string? Thickness { get; set; }

    [StringLength(50)]
    public string? Width { get; set; }

    [StringLength(50)]
    public string? Length { get; set; }

    [StringLength(500)]
    public string? Specification { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999", ErrorMessage = "數量必須至少為 0.0001")]
    public decimal Quantity { get; set; } = 1m;

    [Display(Name = "單價")]
    [Range(typeof(decimal), "-999999999999", "999999999999", ErrorMessage = "單價必須介於 -999999999999 至 999999999999")]
    public decimal UnitPrice { get; set; }

    public decimal LineAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

public sealed record SalesOrderCustomerOption(
    long CustomerId,
    string CustomerCode,
    string CustomerName,
    string? TaxId,
    string? ContactName,
    string? Phone,
    string? Address,
    string? TaxType);

public sealed record SalesOrderProductOption(
    long ProductId,
    string ProductCode,
    string ProductName,
    string? Specification,
    string? Unit,
    decimal UnitPrice,
    string? Thickness,
    string? Width,
    string? Length);
