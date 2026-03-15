namespace QuotationApi.DTOs.Host;

/// <summary>
/// 維護清單新增/更新 DTO
/// 用於 POST /api/hosts 與 PUT /api/hosts/{id}
///
/// 注意：itemid 欄位不使用，忽略不傳。
/// </summary>
public class HostCreateUpdateDto
{
    /// <summary>維護項目名稱（必填，最多 200 字元）</summary>
    public string Item { get; set; } = null!;

    /// <summary>網站網址（選填，最多 500 字元）</summary>
    public string? Url { get; set; }

    /// <summary>開始日期（選填）</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>到期日期（選填）</summary>
    public DateTime? ExpireDate { get; set; }
}
