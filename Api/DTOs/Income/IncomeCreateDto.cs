namespace QuotationApi.DTOs.Income;

/// <summary>
/// 新增收款的請求 DTO
/// 收款編碼由後端自動產生（INC{yyyyMMdd}{NNN}），不由前端傳入。
/// </summary>
public class IncomeCreateDto
{
    /// <summary>關聯客戶 ID（必填）</summary>
    public int CustomerId { get; set; }

    /// <summary>收款金額（元）</summary>
    public int? Amount { get; set; }

    /// <summary>手續費（元）</summary>
    public int? Fee { get; set; }

    /// <summary>收款日期</summary>
    public DateTime? IncomeDate { get; set; }

    /// <summary>備註</summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 本次入帳要關聯（核銷）的請款單（發票）ID 清單。
    /// 建立入帳後會將這些發票的 incomeid 指向新入帳；
    /// 僅接受屬於同一客戶且尚未關聯其他入帳的發票。
    /// </summary>
    public List<Guid> InvoiceIds { get; set; } = new();
}
