namespace QuotationApi.DTOs.Invoice;

/// <summary>
/// 報價單下拉選單 DTO
/// 用於 GET /api/invoices/quotations/{customerId}
/// 供建立/編輯發票時選擇對應的報價單
/// </summary>
public class QuotationLookupDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>稅別：0=稅外加, 1=稅內含, 2=免稅</summary>
    public short? TaxType { get; set; }

    public int? Total { get; set; }
}
