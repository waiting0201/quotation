namespace QuotationApi.DTOs.Income;

/// <summary>
/// 收款列表項目 DTO
/// HasInvoices：該收款是否有關聯的發票（若有則前端隱藏刪除按鈕）
/// </summary>
public class IncomeListDto
{
    public Guid     IncomeId     { get; set; }
    public string   IncomeCode   { get; set; } = "";
    public int?     CustomerId   { get; set; }
    public string   CustomerName { get; set; } = "";
    public int?     Amount       { get; set; }
    public int?     Fee          { get; set; }
    public DateTime? IncomeDate  { get; set; }
    public string?  Remark       { get; set; }
    public DateTime? CreateDate  { get; set; }

    /// <summary>
    /// 是否有關聯發票（invoices.incomeid IS NOT NULL）；
    /// 若為 true，前端應隱藏刪除按鈕、後端 DELETE 會回傳 409。
    /// </summary>
    public bool HasInvoices { get; set; }
}
