using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Customer;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 客戶管理 Controller
///
/// GET    /api/customers       — 客戶列表（支援 ?search= 依名稱/編碼搜尋）
/// POST   /api/customers       — 新增客戶
/// GET    /api/customers/{id}  — 取得單一客戶詳情（含聯絡人）
/// PUT    /api/customers/{id}  — 更新客戶
/// DELETE /api/customers/{id}  — 刪除客戶（有報價單時回傳 409）
/// </summary>
public class CustomerController
{
    private readonly CustomerService _customerService;

    public CustomerController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    // ── GET /api/customers ────────────────────────────────────────────────

    /// <summary>
    /// 取得客戶清單（分頁），可選填 ?page=&pageSize=&search= 依名稱或編碼關鍵字過濾。
    /// </summary>
    public async Task<IActionResult> GetList(RouteContext context)
    {
        var page     = int.TryParse(context.Request.Query["page"].FirstOrDefault(), out var p) && p > 0 ? p : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) && ps > 0 && ps <= 9999 ? ps : 20;
        var search   = context.Request.Query["search"].FirstOrDefault();

        var result = await _customerService.GetListAsync(page, pageSize, search);
        return context.OkPaged(result);
    }

    // ── GET /api/customers/{id} ───────────────────────────────────────────

    /// <summary>
    /// 取得單一客戶詳情，含聯絡人列表。
    /// </summary>
    public async Task<IActionResult> GetById(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶 ID。");

        var detail = await _customerService.GetByIdAsync(id);
        if (detail == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的客戶。");

        return context.Ok(detail);
    }

    // ── POST /api/customers ───────────────────────────────────────────────

    /// <summary>
    /// 新增客戶。
    /// Body: { name, address?, customerTypeId?, countryId?, phone?, fax?, vatNumber?, contacts? }
    /// </summary>
    public async Task<IActionResult> Create(RouteContext context)
    {
        CustomerCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CustomerCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var created = await _customerService.CreateAsync(dto);
        return context.Created(created);
    }

    // ── PUT /api/customers/{id} ───────────────────────────────────────────

    /// <summary>
    /// 更新客戶資料與聯絡人。
    /// Body: { name, address?, customerTypeId?, countryId?, phone?, fax?, vatNumber?, contacts? }
    /// </summary>
    public async Task<IActionResult> Update(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶 ID。");

        CustomerCreateUpdateDto dto;
        try
        {
            dto = await context.ReadBodyAsync<CustomerCreateUpdateDto>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        var validationError = ValidateDto(dto);
        if (validationError != null)
            return context.BadRequest(validationError);

        var updated = await _customerService.UpdateAsync(id, dto);
        if (updated == null)
            return context.NotFound($"找不到 ID 為 '{id}' 的客戶。");

        return context.Ok(updated);
    }

    // ── DELETE /api/customers/{id} ────────────────────────────────────────

    /// <summary>
    /// 刪除客戶。若客戶已有關聯報價單，回傳 409 Conflict。
    /// </summary>
    public async Task<IActionResult> Delete(RouteContext context, int id)
    {
        if (id <= 0)
            return context.BadRequest("無效的客戶 ID。");

        var (found, error) = await _customerService.DeleteAsync(id);

        if (!found)
            return context.NotFound($"找不到 ID 為 '{id}' 的客戶。");

        if (error != null)
            return context.Conflict(error);

        return context.NoContent();
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 驗證建立/更新 DTO 的共用規則。
    /// 回傳錯誤訊息字串；驗證通過時回傳 null。
    /// </summary>
    private static string? ValidateDto(CustomerCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "客戶名稱不能為空白。";

        if (dto.Name.Length > 200)
            return "客戶名稱不能超過 200 個字元。";

        if (dto.Address != null && dto.Address.Length > 500)
            return "地址不能超過 500 個字元。";

        if (dto.Phone != null && dto.Phone.Length > 50)
            return "電話不能超過 50 個字元。";

        if (dto.Fax != null && dto.Fax.Length > 50)
            return "傳真不能超過 50 個字元。";

        if (dto.VatNumber != null && dto.VatNumber.Length > 50)
            return "統一編號不能超過 50 個字元。";

        return null;
    }
}
