namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 群組列表項目 DTO
/// 用於 GET /api/groups，包含群組基本資訊與使用者人數統計
/// </summary>
public class GroupListDto
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
    public int UserCount { get; set; }
}
