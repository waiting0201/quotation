using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Host;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 維護清單 Controller
///
/// GET    /api/hosts          — 維護項目列表（支援 ?search= 關鍵字篩選）
/// POST   /api/hosts          — 新增維護項目
/// GET    /api/hosts/{id}     — 取得單一維護項目詳情
/// PUT    /api/hosts/{id}     — 更新維護項目
/// DELETE /api/hosts/{id}     — 刪除維護項目
/// </summary>
public class HostController
{
    private readonly HostService _hostService;

    public HostController(HostService hostService)
    {
        _hostService = hostService;
    }

    // ── GET /api/hosts ───────────────────────────────────────────────────────

    /// <summary>
    /// 取得所有維護項目清單，可選填 ?search= 依項目名稱關鍵字過濾。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var search = context.Request.Query["search"].FirstOrDefault();
        var list = await _hostService.GetListAsync(search);
        return context.Ok(list);
    }

    // ── GET /api/hosts/{id} ──────────────────────────────────────────────────

    /// <summary>
    /// 取得單一維護項目詳情。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的維護項目 ID。");

        var detail = await _hostService.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"Host '{id}' not found.");

        return context.Ok(detail);
    }

    // ── POST /api/hosts ──────────────────────────────────────────────────────

    /// <summary>
    /// 新增維護項目。
    /// Body: { item, url, startDate, expireDate }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        HostCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<HostCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var created = await _hostService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/hosts/{id} ──────────────────────────────────────────────────

    /// <summary>
    /// 更新維護項目。
    /// Body: { item, url, startDate, expireDate }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的維護項目 ID。");

        HostCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<HostCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _hostService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"Host '{id}' not found.");

        return context.Ok(updated);
    }

    // ── DELETE /api/hosts/{id} ───────────────────────────────────────────────

    /// <summary>
    /// 刪除維護項目。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的維護項目 ID。");

        var found = await _hostService.DeleteAsync(id);
        if (!found)
            return context.NotFound($"Host '{id}' not found.");

        return context.NoContent();
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立/更新 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(HostCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Item))
            return "維護項目名稱不能為空白。";

        if (dto.Item.Length > 200)
            return "維護項目名稱不能超過 200 個字元。";

        if (dto.Url != null && dto.Url.Length > 500)
            return "網站網址不能超過 500 個字元。";

        return null;
    }
}
