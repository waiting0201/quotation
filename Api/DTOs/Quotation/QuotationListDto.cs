namespace QuotationApi.DTOs.Quotation;

/// <summary>
/// 報價單列表項目 DTO
/// 用於 GET /api/quotations
/// </summary>
public class QuotationListDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? QuotationDate { get; set; }

    /// <summary>稅別：0=稅外加, 1=稅內含, 2=免稅</summary>
    public short? TaxType { get; set; }

    /// <summary>稅額</summary>
    public int? Tax { get; set; }

    /// <summary>含稅合計金額</summary>
    public int? Total { get; set; }

    /// <summary>狀態：0=已報價, 1=已簽約, 2=已結案, 3=已取消</summary>
    public short? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    /// <summary>是否已關聯發票明細（有關聯時不可刪除）</summary>
    public bool HasInvoices { get; set; }
}
