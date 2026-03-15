using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Settings;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 群組管理 Controller
///
/// GET    /api/groups          — 群組列表（含使用者人數）
/// POST   /api/groups          — 新增群組
/// GET    /api/groups/{id}     — 群組詳情（含權限矩陣）
/// PUT    /api/groups/{id}     — 更新群組
/// DELETE /api/groups/{id}     — 刪除群組
/// </summary>
public class GroupController
{
    private readonly GroupService _groupService;

    public GroupController(GroupService groupService)
    {
        _groupService = groupService;
    }

    // ── GET /api/groups ──────────────────────────────────────────────────────

    /// <summary>
    /// 取得所有群組清單，附帶各群組的使用者人數統計。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var list = await _groupService.GetListAsync();
        return context.Ok(list);
    }

    // ── GET /api/groups/{id} ─────────────────────────────────────────────────

    /// <summary>
    /// 取得單一群組詳情，含完整的 grouplim 權限矩陣。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的群組 ID 格式。");

        var detail = await _groupService.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"Group '{id}' not found.");

        return context.Ok(detail);
    }

    // ── POST /api/groups ─────────────────────────────────────────────────────

    /// <summary>
    /// 新增群組。
    /// Body: { title, permissions: [{ limId, isQuery, isInsert, isUpdate, isDelete }] }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        GroupCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<GroupCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
            return context.BadRequest("群組名稱不能為空白。");

        if (dto.Title.Length > 50)
            return context.BadRequest("群組名稱不能超過 50 個字元。");

        var created = await _groupService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/groups/{id} ─────────────────────────────────────────────────

    /// <summary>
    /// 更新群組標題與權限矩陣（完整取代語意）。
    /// Body: { title, permissions: [{ limId, isQuery, isInsert, isUpdate, isDelete }] }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的群組 ID 格式。");

        GroupCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<GroupCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
            return context.BadRequest("群組名稱不能為空白。");

        if (dto.Title.Length > 50)
            return context.BadRequest("群組名稱不能超過 50 個字元。");

        var updated = await _groupService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"Group '{id}' not found.");

        return context.Ok(updated);
    }

    // ── DELETE /api/groups/{id} ───────────────────────────────────────────────

    /// <summary>
    /// 刪除群組。
    /// 若群組底下仍有使用者，回傳 409 Conflict，不允許刪除。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的群組 ID 格式。");

        var (found, errorMessage) = await _groupService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"Group '{id}' not found.");

        if (errorMessage != null)
            return context.Conflict(errorMessage);

        return context.NoContent();
    }
}
