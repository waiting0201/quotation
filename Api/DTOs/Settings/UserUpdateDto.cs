using System.ComponentModel.DataAnnotations;

namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 更新使用者請求 DTO
/// 用於 PUT /api/users/{id}
/// 不含密碼欄位（密碼變更使用專用的 PUT /api/users/{id}/password）
/// Permissions 為完整取代語意（replace-all），不是增量更新
/// </summary>
public class UserUpdateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Email { get; set; } = null!;

    public Guid? GroupId { get; set; }

    public bool Status { get; set; }

    /// <summary>
    /// 使用者個人權限矩陣，對應 userlim 資料表
    /// 傳入的清單會完整取代該使用者既有的所有權限記錄
    /// </summary>
    public List<UserPermissionDto> Permissions { get; set; } = new();
}
