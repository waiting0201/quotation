using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Invoice;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 發票管理 Controller
///
/// GET    /api/invoices                              — 發票列表（支援 ?search= 依發票編號/客戶名稱搜尋）
/// POST   /api/invoices                              — 新增發票
/// GET    /api/invoices/{id}                         — 取得單一發票詳情（含明細）
/// PUT    /api/invoices/{id}                         — 更新發票
/// DELETE /api/invoices/{id}                         — 刪除發票（已關聯收款時回傳 409）
/// GET    /api/invoices/{id}/pdf                     — 匯出請款單 PDF（QuestPDF）
/// GET    /api/invoices/quotations/{customerId}      — 取得客戶的報價單（供明細下拉選單）
/// </summary>
public class InvoiceController
{
    private readonly InvoiceService _invoiceService;
    private readonly InvoicePdfService _pdfService;

    public InvoiceController(InvoiceService invoiceService, InvoicePdfService pdfService)
    {
        _invoiceService = invoiceService;
        _pdfService     = pdfService;
    }

    // ── GET /api/invoices ─────────────────────────────────────────────────

    /// <summary>
    /// 取得發票清單（分頁），可選填 ?page=&pageSize=&search= 依發票編號或客戶名稱過濾。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100 ? ps : 20;
        var search   = context.Request.Query["search"].FirstOrDefault();

        var result = await _invoiceService.GetListAsync(page, pageSize, search);
        return context.OkPaged(result);
    }

    // ── GET /api/invoices/{id} ────────────────────────────────────────────

    /// <summary>
    /// 取得單一發票詳情，含明細列表（含關聯報價單資訊）。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的發票 ID。");

        var detail = await _invoiceService.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的發票。");

        return context.Ok(detail);
    }

    // ── POST /api/invoices ────────────────────────────────────────────────

    /// <summary>
    /// 新增發票。
    /// Body: { customerId?, requestDate?, remark?, status?, details[] }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        InvoiceCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<InvoiceCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var userId = GetCurrentUserId(context);
        var created = await _invoiceService.CreateAsync(dto, userId);
        return context.Created(created);
    }

    // ── PUT /api/invoices/{id} ────────────────────────────────────────────

    /// <summary>
    /// 更新發票標頭與明細。
    /// Body: { customerId?, requestDate?, remark?, status?, details[] }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的發票 ID。");

        InvoiceCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<InvoiceCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _invoiceService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的發票。");

        return context.Ok(updated);
    }

    // ── DELETE /api/invoices/{id} ─────────────────────────────────────────

    /// <summary>
    /// 刪除發票。若發票已關聯收款記錄，回傳 409 Conflict。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的發票 ID。");

        var (found, error) = await _invoiceService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 '{id}' 的發票。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── GET /api/invoices/{id}/pdf ────────────────────────────────────────

    /// <summary>
    /// 匯出請款單 PDF。
    /// 使用 QuestPDF 動態產生 A4 請款單，包含明細表格與公司/銀行資訊。
    /// </summary>
    public async Task<IActionResult> GetPdf(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的請款 ID。");

        var pdfBytes = await _pdfService.GeneratePdfAsync(id);
        if (pdfBytes == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的請款。");

        return context.File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
    }

    // ── GET /api/invoices/quotations/{customerId} ─────────────────────────

    /// <summary>
    /// 取得指定客戶的報價單清單，供建立/編輯發票明細時作為下拉選單。
    /// 回傳未取消（status &lt;&gt; 3）的報價單，依建立日期 DESC 排序。
    /// </summary>
    public async Task<IActionResult> GetCustomerQuotations(RouteContext context, int customerId)
    {
        if (customerId <= 0)
            return context.BadRequest("無效的客戶 ID。");

        var quotations = await _invoiceService.GetCustomerQuotationsAsync(customerId);
        return context.Ok(quotations);
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立/更新 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(InvoiceCreateUpdateDto dto)
    {
        if (dto.Status.HasValue && dto.Status is < 0 or > 3)
            return "發票狀態值無效，允許範圍：0=已開, 1=已寄出, 2=已入帳, 3=作廢。";

        if (dto.Remark != null && dto.Remark.Length > 500)
            return "備註不能超過 500 個字元。";

        foreach (var detail in dto.Details)
        {
            if (detail.InvoiceType.HasValue && detail.InvoiceType is < 0 or > 1)
                return "發票類型值無效，允許範圍：0=二聯, 1=三聯。";

            if (detail.Price.HasValue && detail.Price < 0)
                return "發票金額不能為負數。";

            if (detail.InvoiceNumber != null && detail.InvoiceNumber.Length > 10)
                return "發票號碼不能超過 10 個字元（例：AB12345678）。";

            if (detail.Remark != null && detail.Remark.Length > 250)
                return "明細備註不能超過 250 個字元。";
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
