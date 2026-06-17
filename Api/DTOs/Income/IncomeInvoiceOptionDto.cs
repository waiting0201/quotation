namespace QuotationApi.DTOs.Income;

/// <summary>
/// 入帳可選請款單（發票）選項 DTO。
/// 用於「新增入帳」時，列出某客戶尚未關聯任何入帳（incomeid IS NULL）的發票供勾選核銷。
/// </summary>
public class IncomeInvoiceOptionDto
{
    public Guid      InvoiceId   { get; set; }
    public string    InvoiceCode { get; set; } = "";
    public DateTime? RequestDate { get; set; }
    public int?      Tax         { get; set; }
    public int?      Total       { get; set; }
    public short?    Status      { get; set; }
    public DateTime? CreateDate  { get; set; }
}
