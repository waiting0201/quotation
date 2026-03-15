namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 使用者列表項目 DTO
/// 用於 GET /api/users，包含使用者基本資訊與所屬群組名稱
/// </summary>
public class UserListDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Guid? GroupId { get; set; }
    public string? GroupTitle { get; set; }
    public bool Status { get; set; }
    public DateTime? UpdateTime { get; set; }
}
