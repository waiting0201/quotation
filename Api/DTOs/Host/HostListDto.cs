namespace QuotationApi.DTOs.Host;

/// <summary>
/// 維護清單列表項目 DTO
/// 用於 GET /api/hosts，包含維護項目基本資訊
/// </summary>
public class HostListDto
{
    public int HostId { get; set; }
    public string Item { get; set; } = null!;
    public string? Url { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpireDate { get; set; }
}
