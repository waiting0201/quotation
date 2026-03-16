using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Lookup;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 付款條件管理服務
///
/// - GetListAsync:    Dapper 查詢列表（分頁）
/// - GetAllAsync:     Dapper 查詢全部（供下拉選單使用）
/// - GetByIdAsync:    Dapper 查詢單筆
/// - CreateAsync:     EF Core 新增（paymentid 為 identity 自動遞增）
/// - UpdateAsync:     EF Core 更新
/// - DeleteAsync:     EF Core 刪除（payments 無外鍵約束，直接刪除）
/// </summary>
public class PaymentService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    public PaymentService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得付款條件清單（分頁）。
    /// </summary>
    public async Task<PaginatedResponse<PaymentListDto>> GetListAsync(int page, int pageSize)
    {
        var param = new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        const string countSql = "SELECT COUNT(*) FROM payments";
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql);

        const string dataSql = """
            SELECT
                paymentid AS PaymentId,
                remark    AS Remark
            FROM payments
            ORDER BY paymentid
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<PaymentListDto>(dataSql, param);
        return PaginatedResponse<PaymentListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得全部付款條件（不分頁，供下拉選單使用）。
    /// </summary>
    public async Task<IReadOnlyList<PaymentListDto>> GetAllAsync()
    {
        const string sql = """
            SELECT
                paymentid AS PaymentId,
                remark    AS Remark
            FROM payments
            ORDER BY paymentid
            """;

        var items = await _dapper.QueryAsync<PaymentListDto>(sql);
        return items.AsList();
    }

    /// <summary>
    /// 取得單一付款條件詳情。
    /// </summary>
    public async Task<PaymentListDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                paymentid AS PaymentId,
                remark    AS Remark
            FROM payments
            WHERE paymentid = @Id
            """;

        return await _dapper.QueryFirstOrDefaultAsync<PaymentListDto>(sql, new { Id = id });
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增付款條件。paymentid 由資料庫 identity 自動產生。
    /// </summary>
    public async Task<PaymentListDto> CreateAsync(PaymentCreateUpdateDto dto)
    {
        var entity = new Payment
        {
            Remark = dto.Remark?.Trim()
        };

        _db.Payments.Add(entity);
        await _db.SaveChangesAsync();

        return new PaymentListDto
        {
            PaymentId = entity.Paymentid,
            Remark    = entity.Remark
        };
    }

    /// <summary>
    /// 更新付款條件。
    /// </summary>
    public async Task<PaymentListDto?> UpdateAsync(int id, PaymentCreateUpdateDto dto)
    {
        var entity = await _db.Payments
            .FirstOrDefaultAsync(p => p.Paymentid == id);

        if (entity == null)
            return null;

        entity.Remark = dto.Remark?.Trim();
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除付款條件。payments 無外鍵約束，直接刪除。
    /// </summary>
    public async Task<(bool Found, string? Error)> DeleteAsync(int id)
    {
        var entity = await _db.Payments
            .FirstOrDefaultAsync(p => p.Paymentid == id);

        if (entity == null)
            return (false, null);

        _db.Payments.Remove(entity);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
