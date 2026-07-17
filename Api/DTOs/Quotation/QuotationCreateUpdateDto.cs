namespace QuotationApi.DTOs.Quotation;

/// <summary>
/// 建立/更新報價單的請求 DTO
/// 用於 POST /api/quotations 及 PUT /api/quotations/{id}
/// </summary>
public class QuotationCreateUpdateDto
{
    /// <summary>客戶 ID（int identity，必填）</summary>
    public int? CustomerId { get; set; }

    /// <summary>聯絡人 ID（uniqueidentifier，選填）</summary>
    public Guid? CustomerDetailId { get; set; }

    /// <summary>報價單名稱/說明</summary>
    public string? Name { get; set; }

    /// <summary>報價日期（台北時區，格式 yyyy-MM-dd）</summary>
    public DateTime? QuotationDate { get; set; }

    /// <summary>效期日期（選填）</summary>
    public DateTime? ExpireDate { get; set; }

    /// <summary>稅別：0=稅外加, 1=稅內含, 2=免稅</summary>
    public short? TaxType { get; set; }

    /// <summary>折扣百分比（0-100 整數，選填，預設 0=無折扣）</summary>
    public int? Discount { get; set; }

    /// <summary>付款條件（選填）</summary>
    public string? Payment { get; set; }

    /// <summary>備註（選填，上限 500 字）</summary>
    public string? Remark { get; set; }

    /// <summary>工作天數（選填）</summary>
    public int? Workdays { get; set; }

    /// <summary>狀態：0=已報價, 1=已簽約, 2=已結案, 3=已取消</summary>
    public short? Status { get; set; }

    /// <summary>報價明細列表（一般項目，含數量與單價）</summary>
    public List<QuotationDetailInputDto> Details { get; set; } = new();

    /// <summary>報價內容列表（文字內容項目，含整筆金額）</summary>
    public List<QuotationContentInputDto> Contents { get; set; } = new();
}

/// <summary>
/// 報價明細輸入 DTO（建立/更新用）
/// </summary>
public class QuotationDetailInputDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Quantity { get; set; }
    public int? Price { get; set; }

    /// <summary>排序序號；由前端傳入，若未提供則後端依陣列順序自動遞增</summary>
    public int? Freq { get; set; }
}

/// <summary>
/// 報價內容輸入 DTO（建立/更新用）
/// </summary>
public class QuotationContentInputDto
{
    public string? Title { get; set; }
    public string? Remark { get; set; }
    public int? Price { get; set; }

    /// <summary>排序序號；由前端傳入，若未提供則後端依陣列順序自動遞增</summary>
    public int? Freq { get; set; }
}
