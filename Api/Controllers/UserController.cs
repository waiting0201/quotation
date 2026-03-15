using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Settings;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 使用者管理 Controller
///
/// GET    /api/users               — 使用者列表（含所屬群組名稱）
/// POST   /api/users               — 新增使用者
/// GET    /api/users/{id}          — 使用者詳情（含個人權限矩陣）
/// PUT    /api/users/{id}          — 更新使用者（不含密碼）
/// PUT    /api/users/{id}/password — 變更密碼
/// DELETE /api/users/{id}          — 刪除使用者
/// </summary>
public class UserController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    // ── GET /api/users ───────────────────────────────────────────────────────

    /// <summary>
    /// 取得所有使用者清單，附帶所屬群組名稱。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var list = await _userService.GetListAsync();
        return context.Ok(list);
    }

    // ── GET /api/users/{id} ──────────────────────────────────────────────────

    /// <summary>
    /// 取得單一使用者詳情，含完整的 userlim 個人權限矩陣。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的使用者 ID 格式。");

        var detail = await _userService.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"User '{id}' not found.");

        return context.Ok(detail);
    }

    // ── POST /api/users ──────────────────────────────────────────────────────

    /// <summary>
    /// 新增使用者。
    /// Body: { name, email, password, groupId, status, permissions: [...] }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        UserCreateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<UserCreateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            return context.BadRequest("使用者姓名不能為空白。");

        if (dto.Name.Length > 50)
            return context.BadRequest("使用者姓名不能超過 50 個字元。");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return context.BadRequest("電子郵件不能為空白。");

        if (dto.Email.Length > 100)
            return context.BadRequest("電子郵件不能超過 100 個字元。");

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return context.BadRequest("電子郵件格式不正確。");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return context.BadRequest("密碼不能為空白。");

        if (dto.Password.Length < 4)
            return context.BadRequest("密碼至少需要 4 個字元。");

        try
        {
            var created = await _userService.CreateAsync(dto);
            return context.Created(created);
        }
        catch (InvalidOperationException ex)
        {
            return context.Conflict(ex.Message);
        }
    }

    // ── PUT /api/users/{id} ──────────────────────────────────────────────────

    /// <summary>
    /// 更新使用者基本資料與權限矩陣（完整取代語意）。
    /// Body: { name, email, groupId, status, permissions: [...] }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的使用者 ID 格式。");

        UserUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<UserUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            return context.BadRequest("使用者姓名不能為空白。");

        if (dto.Name.Length > 50)
            return context.BadRequest("使用者姓名不能超過 50 個字元。");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return context.BadRequest("電子郵件不能為空白。");

        if (dto.Email.Length > 100)
            return context.BadRequest("電子郵件不能超過 100 個字元。");

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return context.BadRequest("電子郵件格式不正確。");

        try
        {
            var updated = await _userService.UpdateAsync(id, dto);
            if (updated == null)
                return context.NotFound($"User '{id}' not found.");

            return context.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return context.Conflict(ex.Message);
        }
    }

    // ── PUT /api/users/{id}/password ─────────────────────────────────────────

    /// <summary>
    /// 變更使用者密碼。
    /// Body: { newPassword }
    /// </summary>
    public async Task<IActionResult> ChangePassword(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的使用者 ID 格式。");

        UserPasswordChangeDto dto;
        try
        {
            dto = await context.ReadBodyAsync<UserPasswordChangeDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            return context.BadRequest("新密碼不能為空白。");

        if (dto.NewPassword.Length < 4)
            return context.BadRequest("新密碼至少需要 4 個字元。");

        var found = await _userService.ChangePasswordAsync(id, dto.NewPassword);
        if (!found)
            return context.NotFound($"User '{id}' not found.");

        return context.NoContent();
    }

    // ── DELETE /api/users/{id} ───────────────────────────────────────────────

    /// <summary>
    /// 刪除使用者（同時刪除其所有 userlim 個人權限記錄）。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的使用者 ID 格式。");

        var found = await _userService.DeleteAsync(id);
        if (!found)
            return context.NotFound($"User '{id}' not found.");

        return context.NoContent();
    }
}
