namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 群組詳情 DTO
/// 用於 GET /api/groups/{id}，包含群組資訊與完整權限矩陣
/// </summary>
public class GroupDetailDto
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
    public List<GroupPermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// 單一權限項目 DTO
/// 對應 grouplim 資料表一筆記錄
/// </summary>
public class GroupPermissionDto
{
    public int LimId { get; set; }
    public bool IsQuery { get; set; }
    public bool IsInsert { get; set; }
    public bool IsUpdate { get; set; }
    public bool IsDelete { get; set; }
}
