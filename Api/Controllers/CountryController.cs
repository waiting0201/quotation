using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Lookup;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 國家 Controller
///
/// GET    /api/countries       — 國家列表（含客戶數量）
/// POST   /api/countries       — 新增國家
/// GET    /api/countries/{id}  — 取得單一國家
/// PUT    /api/countries/{id}  — 更新國家
/// DELETE /api/countries/{id}  — 刪除國家（有客戶時拒絕）
/// </summary>
public class CountryController
{
    private readonly CountryService _countryService;

    public CountryController(CountryService countryService)
    {
        _countryService = countryService;
    }

    // ── GET /api/countries ─────────────────────────────────────────────────

    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100 ? ps : 20;

        var result = await _countryService.GetListAsync(page, pageSize);
        return context.OkPaged(result);
    }

    // ── GET /api/countries/{id} ────────────────────────────────────────────

    public async Task<IActionResult> GetById(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的國家 ID。");

        var item = await _countryService.GetByIdAsync(id);
        if (item == null)
            return context.NotFound($"找不到 ID 為 {id} 的國家。");

        return context.Ok(item);
    }

    // ── POST /api/countries ────────────────────────────────────────────────

    public async Task<IActionResult> Create(RouteContext context)
    {
        CountryCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CountryCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var created = await _countryService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/countries/{id} ────────────────────────────────────────────

    public async Task<IActionResult> Update(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的國家 ID。");

        CountryCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CountryCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _countryService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 {id} 的國家。");

        return context.Ok(updated);
    }

    // ── DELETE /api/countries/{id} ─────────────────────────────────────────

    public async Task<IActionResult> Delete(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的國家 ID。");

        var (found, error) = await _countryService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 {id} 的國家。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ────────────────────────────────────────────────────────────

    private static string? ValidateDto(CountryCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "國家名稱不能為空白。";

        if (dto.Title.Length > 50)
            return "國家名稱不能超過 50 個字元。";

        return null;
    }
}
