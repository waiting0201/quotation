namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 使用者詳情 DTO
/// 用於 GET /api/users/{id}，包含使用者資訊與完整 userlim 權限矩陣
/// </summary>
public class UserDetailDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Guid? GroupId { get; set; }
    public bool Status { get; set; }
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 使用者個人權限矩陣，對應 userlim 資料表
    /// </summary>
    public List<UserPermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// 單一使用者權限項目 DTO
/// 對應 userlim 資料表一筆記錄
/// </summary>
public class UserPermissionDto
{
    public int LimId { get; set; }
    public bool IsQuery { get; set; }
    public bool IsInsert { get; set; }
    public bool IsUpdate { get; set; }
    public bool IsDelete { get; set; }
}
