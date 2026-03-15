using System.ComponentModel.DataAnnotations;

namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 新增/更新群組請求 DTO
/// 用於 POST /api/groups 與 PUT /api/groups/{id}
/// Permissions 為完整取代語意（replace-all），不是增量更新
/// </summary>
public class GroupCreateUpdateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// 權限矩陣，對應 grouplim 資料表
    /// 傳入的清單會完整取代該群組既有的所有權限記錄
    /// </summary>
    public List<GroupPermissionDto> Permissions { get; set; } = new();
}
