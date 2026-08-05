using System.ComponentModel.DataAnnotations;

namespace SmartIMS.Web.ViewModels;

public static class QuotationStatuses
{
    public const string Quoting = "Quoting";
    public const string Converted = "Converted";
    public const string Voided = "Voided";

    public static string GetLabel(string status) => status switch
    {
        Converted => "轉銷售單",
        Voided => "已作廢",
        _ => "報價中"
    };
}

public static class QuotationTaxTypes
{
    public const string Inclusive = "Inclusive";
    public const string Exclusive = "Exclusive";

    public static string GetLabel(string taxType) =>
        taxType == Inclusive ? "含稅" : "稅外加";
}

public sealed class QuotationListViewModel
{
    public IReadOnlyList<QuotationListItem> Quotations { get; set; } = [];

    public long? SelectedQuotationId { get; set; }

    public QuotationDetailViewModel? SelectedQuotation { get; set; }
}

public sealed record QuotationListItem(
    long SalesQuoteId,
    string QuoteNo,
    DateTime QuoteDate,
    DateTime ExpireDate,
    string CustomerCode,
    string CustomerName,
    string TaxType,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    string? SalesOrderNo);

public sealed class QuotationDetailViewModel
{
    public long SalesQuoteId { get; set; }

    public string QuoteNo { get; set; } = "";

    public DateTime QuoteDate { get; set; }

    public DateTime ExpireDate { get; set; }

    public string CustomerCode { get; set; } = "";

    public string CustomerName { get; set; } = "";

    public string? CustomerTaxId { get; set; }

    public string? CustomerContactName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerAddress { get; set; }

    public string TaxType { get; set; } = QuotationTaxTypes.Exclusive;

    public decimal TaxRate { get; set; }

    public bool IsCustomerSuppliedMaterial { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = QuotationStatuses.Quoting;

    public string? SalesOrderNo { get; set; }

    public string? Remark { get; set; }

    public IReadOnlyList<QuotationDetailLine> Lines { get; set; } = [];
}

public sealed record QuotationDetailLine(
    int LineNumber,
    long? ProductId,
    string ItemSource,
    string? ProductCode,
    string ProductName,
    string? Specification,
    string? Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineAmount,
    string? Remark);

public sealed class QuotationEditViewModel
{
    public long? SalesQuoteId { get; set; }

    public string? QuoteNo { get; set; }

    [Required(ErrorMessage = "請選擇客戶")]
    [Display(Name = "客戶")]
    public long? CustomerId { get; set; }

    [Required(ErrorMessage = "請輸入報價日期")]
    [DataType(DataType.Date)]
    public DateTime QuoteDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "請輸入有效日期")]
    [DataType(DataType.Date)]
    public DateTime ExpireDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    public string TaxType { get; set; } = QuotationTaxTypes.Exclusive;

    [Range(typeof(decimal), "0", "100", ErrorMessage = "稅率必須介於 0 到 100")]
    public decimal TaxRate { get; set; } = 5m;

    public bool IsCustomerSuppliedMaterial { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = QuotationStatuses.Quoting;

    public string? SalesOrderNo { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public List<QuotationLineInputModel> Lines { get; set; } = [];

    public IReadOnlyList<QuotationCustomerOption> Customers { get; set; } = [];

    public IReadOnlyList<QuotationProductOption> Products { get; set; } = [];

    public bool IsEditable => Status == QuotationStatuses.Quoting;
}

public sealed class QuotationLineInputModel
{
    public long? SalesQuoteItemId { get; set; }

    public long? ProductId { get; set; }

    public string ItemSource { get; set; } = "Manual";

    [StringLength(50)]
    public string? ProductCode { get; set; }

    [Required(ErrorMessage = "請輸入商品名稱")]
    [StringLength(200)]
    public string ProductName { get; set; } = "";

    [StringLength(500)]
    public string? Specification { get; set; }

    [StringLength(20)]
    public string? Unit { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999", ErrorMessage = "數量必須大於 0")]
    public decimal Quantity { get; set; } = 1m;

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "單價不可小於 0")]
    public decimal UnitPrice { get; set; }

    public decimal LineAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

public sealed record QuotationCustomerOption(
    long CustomerId,
    string CustomerCode,
    string CustomerName,
    string? TaxId,
    string? ContactName,
    string? Phone,
    string? Address,
    string? TaxType);

public sealed record QuotationProductOption(
    long ProductId,
    string ProductCode,
    string ProductName,
    string? Specification,
    string? Unit,
    decimal UnitPrice);

public sealed record QuotationConversionResult(
    long SalesOrderId,
    string SalesOrderNo);
