using System.ComponentModel.DataAnnotations;

namespace QuotationApi.DTOs.Settings;

/// <summary>
/// 變更使用者密碼請求 DTO
/// 用於 PUT /api/users/{id}/password
/// </summary>
public class UserPasswordChangeDto
{
    /// <summary>
    /// 新密碼明文，至少 4 個字元；後端以 SHA1+salt 雜湊後儲存
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 4)]
    public string NewPassword { get; set; } = null!;
}
