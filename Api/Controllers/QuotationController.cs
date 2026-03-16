using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Quotation;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 報價單管理 Controller
///
/// GET    /api/quotations                  — 報價單列表（支援 ?search= 依編號/名稱/客戶名稱搜尋）
/// POST   /api/quotations                  — 新增報價單（含明細與內容）
/// GET    /api/quotations/{id}             — 取得單一報價單詳情（含明細與內容）
/// PUT    /api/quotations/{id}             — 更新報價單（整批取代明細與內容）
/// DELETE /api/quotations/{id}             — 刪除報價單（已關聯發票時回傳 409）
/// </summary>
public class QuotationController
{
    private readonly QuotationService _service;
    private readonly QuotationPdfService _pdfService;

    public QuotationController(QuotationService service, QuotationPdfService pdfService)
    {
        _service = service;
        _pdfService = pdfService;
    }

    // ── GET /api/quotations ────────────────────────────────────────────────

    /// <summary>
    /// 取得報價單清單（分頁），可選填 ?page=&pageSize=&search= 依編號/名稱/客戶名稱過濾。
    /// pageSize 上限 100，避免一次拉取過多資料。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100 ? ps : 20;
        var search   = context.Request.Query["search"].FirstOrDefault();

        var result = await _service.GetListAsync(page, pageSize, search);
        return context.OkPaged(result);
    }

    // ── GET /api/quotations/{id} ───────────────────────────────────────────

    /// <summary>
    /// 取得單一報價單詳情，含明細（itemdetails）與內容（itemcontents）列表。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的報價單 ID。");

        var detail = await _service.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的報價單。");

        return context.Ok(detail);
    }

    // ── GET /api/quotations/{id}/pdf ──────────────────────────────────────

    /// <summary>
    /// 匯出報價單 PDF。
    /// 使用 QuestPDF 動態產生 A4 報價單，包含客戶資訊、內容報價表格與架構說明。
    /// </summary>
    public async Task<IActionResult> GetPdf(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的報價單 ID。");

        var pdfBytes = await _pdfService.GeneratePdfAsync(id);
        if (pdfBytes == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的報價單。");

        return context.File(pdfBytes, "application/pdf", $"quotation-{id}.pdf");
    }

    // ── POST /api/quotations ───────────────────────────────────────────────

    /// <summary>
    /// 新增報價單。
    /// Body: { customerId, name?, quotationDate?, expireDate?, taxType, payment?, remark?, status?, details[], contents[] }
    /// 自動產生 QUO{yyyyMMdd}{NNN} 編碼，依 taxType 計算稅額。
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        QuotationCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<QuotationCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var userId  = GetCurrentUserId(context);
        var created = await _service.CreateAsync(dto, userId);
        return context.Created(created);
    }

    // ── PUT /api/quotations/{id} ───────────────────────────────────────────

    /// <summary>
    /// 更新報價單標頭、明細與內容。
    /// Body: { customerId?, name?, quotationDate?, expireDate?, taxType?, payment?, remark?, status?, details[], contents[] }
    /// 明細與內容採整批取代策略：刪除舊記錄後重新插入。
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的報價單 ID。");

        QuotationCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<QuotationCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _service.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的報價單。");

        return context.Ok(updated);
    }

    // ── DELETE /api/quotations/{id} ────────────────────────────────────────

    /// <summary>
    /// 刪除報價單。若報價單已關聯發票明細，回傳 409 Conflict，防止資料孤立。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的報價單 ID。");

        var (found, error) = await _service.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 '{id}' 的報價單。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立/更新 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(QuotationCreateUpdateDto dto)
    {
        if (dto.TaxType.HasValue && dto.TaxType is < 0 or > 2)
            return "稅別值無效，允許範圍：0=稅外加, 1=稅內含, 2=免稅。";

        if (dto.Status.HasValue && dto.Status is < 0 or > 3)
            return "報價單狀態值無效，允許範圍：0=已報價, 1=已簽約, 2=已結案, 3=已取消。";

        if (dto.Remark != null && dto.Remark.Length > 500)
            return "備註不能超過 500 個字元。";

        foreach (var detail in dto.Details)
        {
            if (detail.Quantity.HasValue && detail.Quantity < 0)
                return "明細數量不能為負數。";

            if (detail.Price.HasValue && detail.Price < 0)
                return "明細單價不能為負數。";
        }

        foreach (var content in dto.Contents)
        {
            if (content.Price.HasValue && content.Price < 0)
                return "內容金額不能為負數。";
        }

        return null;
    }

    /// <summary>
    /// 從 JWT Claims 中取得當前登入使用者的 UserId。
    /// 若 Token 未帶 UserId（理論上不應發生，因為 JwtAuthMiddleware 已驗證）則回傳 Guid.Empty。
    /// </summary>
    private static Guid GetCurrentUserId(RouteContext context)
    {
        var claim = context.CurrentUser?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? context.CurrentUser?.FindFirst("sub");

        return claim != null && Guid.TryParse(claim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }
}
