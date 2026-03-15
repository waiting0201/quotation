namespace QuotationApi.DTOs.Invoice;

/// <summary>
/// 建立/更新發票的請求 DTO
/// 用於 POST /api/invoices 及 PUT /api/invoices/{id}
/// </summary>
public class InvoiceCreateUpdateDto
{
    public int? CustomerId { get; set; }
    public DateTime? RequestDate { get; set; }
    public string? Remark { get; set; }

    /// <summary>發票狀態：0=已開, 1=已寄出, 2=已入帳, 3=作廢</summary>
    public short? Status { get; set; }

    public List<InvoiceDetailDto> Details { get; set; } = new();
}

/// <summary>
/// 發票明細 DTO（建立/更新用）
/// </summary>
public class InvoiceDetailDto
{
    /// <summary>現有明細的 ID；null 表示新增</summary>
    public Guid? InvoiceDetailId { get; set; }

    /// <summary>FK 關聯到 items（報價單）</summary>
    public Guid? ItemId { get; set; }

    /// <summary>發票類型：0=二聯, 1=三聯</summary>
    public short? InvoiceType { get; set; }

    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? Price { get; set; }
    public string? Remark { get; set; }
}
