using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Customer;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 客戶分類管理服務
///
/// - GetAllAsync:   Dapper 查詢列表，含各分類的客戶數量（避免 N+1）
/// - GetByIdAsync:  EF Core 查詢單筆
/// - CreateAsync:   EF Core 新增（customertypeid 為 identity 自動遞增）
/// - UpdateAsync:   EF Core 更新
/// - DeleteAsync:   先確認無關聯客戶，再 EF Core 刪除
/// </summary>
public class CustomerTypeService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    public CustomerTypeService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得客戶分類清單（分頁），每筆附帶該分類下的客戶數量。
    /// 使用 Dapper 搭配 COUNT + OFFSET/FETCH 完成，避免 EF Core 多次 round-trip 或 N+1 問題。
    /// </summary>
    public async Task<PaginatedResponse<CustomerTypeListDto>> GetListAsync(int page, int pageSize)
    {
        var param = new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        const string countSql = "SELECT COUNT(*) FROM customertypes ct";
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql);

        // LEFT JOIN 確保即使分類下沒有客戶也會回傳，COUNT 統計實際客戶數
        const string dataSql = """
            SELECT
                ct.customertypeid   AS CustomerTypeId,
                ct.title            AS Title,
                COUNT(c.customerid) AS CustomerCount
            FROM customertypes ct
            LEFT JOIN customers c ON c.customertypeid = ct.customertypeid
            GROUP BY ct.customertypeid, ct.title
            ORDER BY ct.title
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<CustomerTypeListDto>(dataSql, param);
        return PaginatedResponse<CustomerTypeListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得單一客戶分類詳情（含客戶數量）。
    /// </summary>
    /// <returns>找不到記錄時回傳 null</returns>
    public async Task<CustomerTypeListDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                ct.customertypeid AS CustomerTypeId,
                ct.title          AS Title,
                COUNT(c.customerid) AS CustomerCount
            FROM customertypes ct
            LEFT JOIN customers c ON c.customertypeid = ct.customertypeid
            WHERE ct.customertypeid = @Id
            GROUP BY ct.customertypeid, ct.title
            """;

        var result = await _dapper.QueryFirstOrDefaultAsync<CustomerTypeListDto>(sql, new { Id = id });
        return result;
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增客戶分類。
    /// customertypeid 由資料庫 identity 自動產生。
    /// </summary>
    public async Task<CustomerTypeListDto> CreateAsync(CustomerTypeCreateUpdateDto dto)
    {
        var entity = new Customertype
        {
            Title = dto.Title.Trim()
        };

        _db.Customertypes.Add(entity);
        await _db.SaveChangesAsync();

        return new CustomerTypeListDto
        {
            CustomerTypeId = entity.Customertypeid,
            Title          = entity.Title ?? string.Empty,
            CustomerCount  = 0  // 剛建立，尚無客戶
        };
    }

    /// <summary>
    /// 更新客戶分類名稱。
    /// </summary>
    /// <returns>找不到記錄時回傳 null</returns>
    public async Task<CustomerTypeListDto?> UpdateAsync(int id, CustomerTypeCreateUpdateDto dto)
    {
        var entity = await _db.Customertypes
            .FirstOrDefaultAsync(ct => ct.Customertypeid == id);

        if (entity == null)
            return null;

        entity.Title = dto.Title.Trim();
        await _db.SaveChangesAsync();

        // 更新後重新查詢以取得最新客戶數量
        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除客戶分類。
    /// 若分類下仍有客戶，回傳業務錯誤訊息（不拋例外）。
    /// </summary>
    /// <returns>
    ///   (true,  null)    — 刪除成功
    ///   (false, null)    — 找不到記錄
    ///   (false, message) — 有關聯客戶，無法刪除
    /// </returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(int id)
    {
        var entity = await _db.Customertypes
            .Include(ct => ct.Customers)
            .FirstOrDefaultAsync(ct => ct.Customertypeid == id);

        if (entity == null)
            return (false, null);

        // 保護性刪除：有客戶使用此分類時拒絕刪除
        if (entity.Customers.Count > 0)
            return (true, "無法刪除，此分類下仍有客戶。");

        _db.Customertypes.Remove(entity);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
