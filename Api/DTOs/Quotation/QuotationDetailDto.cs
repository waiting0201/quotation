namespace QuotationApi.DTOs.Quotation;

/// <summary>
/// 報價單詳情回應 DTO
/// 用於 GET /api/quotations/{id}、POST /api/quotations、PUT /api/quotations/{id}
/// 包含完整標頭資訊以及巢狀的明細列表與內容列表
/// </summary>
public class QuotationDetailDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? CustomerDetailId { get; set; }
    public DateTime? QuotationDate { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Payment { get; set; }
    public string? Remark { get; set; }

    /// <summary>稅別：0=稅外加, 1=稅內含, 2=免稅</summary>
    public short? TaxType { get; set; }

    /// <summary>折扣百分比（0-100 整數，0=無折扣）</summary>
    public int? Discount { get; set; }

    /// <summary>折扣金額 = round(未稅小計 × Discount / 100)，由後端計算</summary>
    public int? DiscountAmount { get; set; }

    /// <summary>稅額</summary>
    public int? Tax { get; set; }

    /// <summary>含稅合計金額</summary>
    public int? Total { get; set; }

    /// <summary>工作天數</summary>
    public int? Workdays { get; set; }

    /// <summary>狀態：0=已報價, 1=已簽約, 2=已結案, 3=已取消</summary>
    public short? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    /// <summary>報價明細列表（依 freq 升冪排序）</summary>
    public List<QuotationDetailItemDto> Details { get; set; } = new();

    /// <summary>報價內容列表（依 freq 升冪排序）</summary>
    public List<QuotationContentItemDto> Contents { get; set; } = new();
}

/// <summary>
/// 報價明細回應 DTO（一般項目，含數量與單價）
/// </summary>
public class QuotationDetailItemDto
{
    public Guid ItemDetailId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>數量</summary>
    public int? Quantity { get; set; }

    /// <summary>單價（整數，單位：元）</summary>
    public int? Price { get; set; }

    /// <summary>小計 = Quantity * Price</summary>
    public int? Total { get; set; }

    /// <summary>排序序號（1-based）</summary>
    public int? Freq { get; set; }
}

/// <summary>
/// 報價內容回應 DTO（文字內容項目，含備註與整筆金額）
/// </summary>
public class QuotationContentItemDto
{
    public Guid ItemContentId { get; set; }
    public string? Title { get; set; }
    public string? Remark { get; set; }

    /// <summary>整筆金額（整數，單位：元）</summary>
    public int? Price { get; set; }

    /// <summary>排序序號（1-based）</summary>
    public int? Freq { get; set; }
}
