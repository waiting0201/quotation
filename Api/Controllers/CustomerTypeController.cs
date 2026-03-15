using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Customer;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 客戶分類 Controller
///
/// GET    /api/customer-types       — 所有分類列表（含客戶數量）
/// POST   /api/customer-types       — 新增分類
/// GET    /api/customer-types/{id}  — 取得單一分類
/// PUT    /api/customer-types/{id}  — 更新分類
/// DELETE /api/customer-types/{id}  — 刪除分類（有客戶時拒絕）
/// </summary>
public class CustomerTypeController
{
    private readonly CustomerTypeService _customerTypeService;

    public CustomerTypeController(CustomerTypeService customerTypeService)
    {
        _customerTypeService = customerTypeService;
    }

    // ── GET /api/customer-types ──────────────────────────────────────────────

    /// <summary>
    /// 取得客戶分類清單（分頁），含各分類的客戶數量。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100 ? ps : 20;

        var result = await _customerTypeService.GetListAsync(page, pageSize);
        return context.OkPaged(result);
    }

    // ── GET /api/customer-types/{id} ─────────────────────────────────────────

    /// <summary>
    /// 取得單一客戶分類詳情。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶分類 ID。");

        var item = await _customerTypeService.GetByIdAsync(id);
        if (item == null)
            return context.NotFound($"找不到 ID 為 {id} 的客戶分類。");

        return context.Ok(item);
    }

    // ── POST /api/customer-types ─────────────────────────────────────────────

    /// <summary>
    /// 新增客戶分類。
    /// Body: { "title": "分類名稱" }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        CustomerTypeCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CustomerTypeCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var created = await _customerTypeService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/customer-types/{id} ─────────────────────────────────────────

    /// <summary>
    /// 更新客戶分類名稱。
    /// Body: { "title": "新名稱" }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶分類 ID。");

        CustomerTypeCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CustomerTypeCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _customerTypeService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 {id} 的客戶分類。");

        return context.Ok(updated);
    }

    // ── DELETE /api/customer-types/{id} ──────────────────────────────────────

    /// <summary>
    /// 刪除客戶分類。
    /// 若分類下仍有客戶，回傳 409 Conflict。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶分類 ID。");

        var (found, error) = await _customerTypeService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 {id} 的客戶分類。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立/更新 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(CustomerTypeCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "分類名稱不能為空白。";

        if (dto.Title.Length > 50)
            return "分類名稱不能超過 50 個字元。";

        return null;
    }
}
