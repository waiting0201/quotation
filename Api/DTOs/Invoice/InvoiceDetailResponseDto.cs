namespace QuotationApi.DTOs.Invoice;

/// <summary>
/// 發票詳情回應 DTO
/// 用於 GET /api/invoices/{id}、POST /api/invoices、PUT /api/invoices/{id}
/// </summary>
public class InvoiceDetailResponseDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = "";
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime? RequestDate { get; set; }
    public string? Remark { get; set; }

    /// <summary>稅額合計（所有明細稅額之和）</summary>
    public int? Tax { get; set; }

    /// <summary>含稅金額合計（所有明細 price 之和）</summary>
    public int? Total { get; set; }

    /// <summary>發票狀態：0=已開, 1=已寄出, 2=已入帳, 3=作廢</summary>
    public short? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public List<InvoiceDetailItemDto> Details { get; set; } = new();
}

/// <summary>
/// 發票明細回應 DTO（含關聯報價單資訊）
/// </summary>
public class InvoiceDetailItemDto
{
    public Guid InvoiceDetailId { get; set; }
    public Guid? ItemId { get; set; }

    /// <summary>來自 items 資料表的報價單編碼</summary>
    public string? ItemCode { get; set; }

    /// <summary>來自 items 資料表的報價單名稱</summary>
    public string? ItemName { get; set; }

    /// <summary>來自 items 資料表的稅別（0=稅外加, 1=稅內含, 2=免稅）</summary>
    public short? ItemTaxType { get; set; }

    /// <summary>發票類型：0=二聯, 1=三聯</summary>
    public short? InvoiceType { get; set; }

    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? Price { get; set; }
    public int? Tax { get; set; }
    public string? Remark { get; set; }

    /// <summary>明細排序序號（1-based）</summary>
    public int? Freq { get; set; }
}
