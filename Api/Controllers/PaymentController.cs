using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Lookup;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 付款條件 Controller
///
/// GET    /api/payments       — 付款條件列表（分頁）
/// POST   /api/payments       — 新增付款條件
/// GET    /api/payments/{id}  — 取得單一付款條件
/// PUT    /api/payments/{id}  — 更新付款條件
/// DELETE /api/payments/{id}  — 刪除付款條件
/// </summary>
public class PaymentController
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // ── GET /api/payments ──────────────────────────────────────────────────

    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100 ? ps : 20;

        var result = await _paymentService.GetListAsync(page, pageSize);
        return context.OkPaged(result);
    }

    // ── GET /api/payments/{id} ─────────────────────────────────────────────

    public async Task<IActionResult> GetById(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的付款條件 ID。");

        var item = await _paymentService.GetByIdAsync(id);
        if (item == null)
            return context.NotFound($"找不到 ID 為 {id} 的付款條件。");

        return context.Ok(item);
    }

    // ── POST /api/payments ─────────────────────────────────────────────────

    public async Task<IActionResult> Create(RouteContext context)
    {
        PaymentCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<PaymentCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var created = await _paymentService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/payments/{id} ─────────────────────────────────────────────

    public async Task<IActionResult> Update(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的付款條件 ID。");

        PaymentCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<PaymentCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _paymentService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 {id} 的付款條件。");

        return context.Ok(updated);
    }

    // ── DELETE /api/payments/{id} ──────────────────────────────────────────

    public async Task<IActionResult> Delete(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的付款條件 ID。");

        var (found, error) = await _paymentService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 {id} 的付款條件。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ────────────────────────────────────────────────────────────

    private static string? ValidateDto(PaymentCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Remark))
            return "付款條件內容不能為空白。";

        if (dto.Remark.Length > 500)
            return "付款條件內容不能超過 500 個字元。";

        return null;
    }
}
