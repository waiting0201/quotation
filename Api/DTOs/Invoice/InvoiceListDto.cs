namespace QuotationApi.DTOs.Invoice;

/// <summary>
/// 發票列表項目 DTO
/// 用於 GET /api/invoices
/// </summary>
public class InvoiceListDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime? RequestDate { get; set; }

    /// <summary>稅別：0=稅外加, 1=稅內含, 2=免稅</summary>
    public int? Tax { get; set; }

    public int? Total { get; set; }

    /// <summary>發票狀態：0=已開, 1=已寄出, 2=已入帳, 3=作廢</summary>
    public short? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    /// <summary>是否已關聯收款（有關聯收款時不可刪除）</summary>
    public bool HasIncomes { get; set; }
}
