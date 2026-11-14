using System.ComponentModel.DataAnnotations;
namespace SmartIMS.Web.ViewModels;
public sealed class CustomerItem
{
    public long? CustomerId { get; set; }
    [Display(Name = "客戶編號")]
    [StringLength(50)]
    public string CustomerCode { get; set; } = "";
    [Display(Name = "公司名稱")]
    [Required(ErrorMessage = "公司名稱不可空白。")]
    [StringLength(200)]
    public string CustomerName { get; set; } = "";
    [StringLength(100)]
    public string Contact { get; set; } = "";
    [StringLength(50)]
    public string Phone { get; set; } = "";
    [StringLength(50)]
    public string Fax { get; set; } = "";
    [EmailAddress]
    [StringLength(254)]
    public string? Email
    {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    private string? _email;
    [StringLength(100)]
    public string Line { get; set; } = "";
    [StringLength(20)]
    public string TaxId { get; set; } = "";
    [StringLength(20)]
    public string ZipCode { get; set; } = "";
    [StringLength(500)]
    public string CompanyAddress { get; set; } = "";
    [StringLength(500)]
    public string DeliveryAddress { get; set; } = "";
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "月結";
    [StringLength(100)]
    public string BankName { get; set; } = "";
    [StringLength(100)]
    public string BankAccount { get; set; } = "";
    [StringLength(2000)]
    public string Note { get; set; } = "";
}
