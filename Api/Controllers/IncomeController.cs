using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Income;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 收款管理 Controller
///
/// GET    /api/incomes       — 收款列表（支援 ?search= 依收款編號/客戶名稱搜尋）
/// POST   /api/incomes       — 新增收款
/// DELETE /api/incomes/{id}  — 刪除收款（有關聯發票時回傳 409）
/// </summary>
public class IncomeController
{
    private readonly IncomeService _incomeService;

    public IncomeController(IncomeService incomeService)
    {
        _incomeService = incomeService;
    }

    // ── GET /api/incomes ──────────────────────────────────────────────────

    /// <summary>
    /// 取得收款清單（分頁），可選填 ?page=&amp;pageSize=&amp;search= 依收款編號或客戶名稱過濾。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(),     out var p)  && p  > 0 && p  <= int.MaxValue ? p  : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 100          ? ps : 20;
        var search   = context.Request.Query["search"].FirstOrDefault();

        var result = await _incomeService.GetListAsync(page, pageSize, search);
        return context.OkPaged(result);
    }

    // ── GET /api/incomes/invoices/{customerId} ────────────────────────────

    /// <summary>
    /// 取得指定客戶可供入帳核銷的發票清單（incomeid IS NULL 且未作廢）。
    /// 供「新增入帳」選取客戶後，列出可勾選的請款單。
    /// </summary>
    public async Task<IActionResult> GetSelectableInvoices(RouteContext context, int customerId)
    {
        if (customerId <= 0)
            return context.BadRequest("客戶 ID 無效。");

        var invoices = await _incomeService.GetSelectableInvoicesAsync(customerId);
        return context.Ok(invoices);
    }

    // ── POST /api/incomes ─────────────────────────────────────────────────

    /// <summary>
    /// 新增收款記錄。
    /// Body: { customerId, amount?, fee?, incomeDate?, remark? }
    /// 收款編碼由後端自動產生（INC{yyyyMMdd}{NNN}）。
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        IncomeCreateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<IncomeCreateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var userId  = GetCurrentUserId(context);
        var created = await _incomeService.CreateAsync(dto, userId);
        return context.Created(created);
    }

    // ── DELETE /api/incomes/{id} ──────────────────────────────────────────

    /// <summary>
    /// 刪除收款記錄。若已有關聯發票，回傳 409 Conflict。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, Guid id)
    {
        if (id == Guid.Empty)
            return context.BadRequest("無效的收款 ID。");

        var (found, error) = await _incomeService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 '{id}' 的收款記錄。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(IncomeCreateDto dto)
    {
        if (dto.CustomerId <= 0)
            return "客戶 ID 無效，必須指定有效的客戶。";

        if (dto.Amount.HasValue && dto.Amount < 0)
            return "收款金額不能為負數。";

        if (dto.Fee.HasValue && dto.Fee < 0)
            return "手續費不能為負數。";

        if (dto.Remark != null && dto.Remark.Length > 500)
            return "備註不能超過 500 個字元。";

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
