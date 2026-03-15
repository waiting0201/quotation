using System.ComponentModel.DataAnnotations;

namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 新增使用者請求 DTO
/// 用於 POST /api/users
/// Permissions 為完整取代語意（replace-all），不是增量更新
/// </summary>
public class UserCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// 明文密碼，至少 4 個字元；後端以 SHA1+salt 雜湊後儲存
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = null!;

    public Guid? GroupId { get; set; }

    public bool Status { get; set; } = true;

    /// <summary>
    /// 使用者個人權限矩陣，對應 userlim 資料表
    /// 傳入的清單會完整取代該使用者既有的所有權限記錄
    /// </summary>
    public List<UserPermissionDto> Permissions { get; set; } = new();
}
