using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Lookup;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 國家管理服務
///
/// - GetListAsync:  Dapper 查詢列表，含各國家的客戶數量（避免 N+1）
/// - GetByIdAsync:  Dapper 查詢單筆
/// - CreateAsync:   EF Core 新增（countryid 為 identity 自動遞增）
/// - UpdateAsync:   EF Core 更新
/// - DeleteAsync:   先確認無關聯客戶，再 EF Core 刪除
/// </summary>
public class CountryService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    public CountryService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得國家清單（分頁），每筆附帶該國家下的客戶數量。
    /// </summary>
    public async Task<PaginatedResponse<CountryListDto>> GetListAsync(int page, int pageSize)
    {
        var param = new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        const string countSql = "SELECT COUNT(*) FROM country";
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql);

        const string dataSql = """
            SELECT
                co.countryid        AS CountryId,
                co.title            AS Title,
                COUNT(c.customerid) AS CustomerCount
            FROM country co
            LEFT JOIN customers c ON c.countryid = co.countryid
            GROUP BY co.countryid, co.title
            ORDER BY co.title
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<CountryListDto>(dataSql, param);
        return PaginatedResponse<CountryListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得單一國家詳情（含客戶數量）。
    /// </summary>
    public async Task<CountryListDto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                co.countryid        AS CountryId,
                co.title            AS Title,
                COUNT(c.customerid) AS CustomerCount
            FROM country co
            LEFT JOIN customers c ON c.countryid = co.countryid
            WHERE co.countryid = @Id
            GROUP BY co.countryid, co.title
            """;

        return await _dapper.QueryFirstOrDefaultAsync<CountryListDto>(sql, new { Id = id });
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增國家。countryid 由資料庫 identity 自動產生。
    /// </summary>
    public async Task<CountryListDto> CreateAsync(CountryCreateUpdateDto dto)
    {
        var entity = new Country
        {
            Title = dto.Title.Trim()
        };

        _db.Countries.Add(entity);
        await _db.SaveChangesAsync();

        return new CountryListDto
        {
            CountryId     = entity.Countryid,
            Title         = entity.Title ?? string.Empty,
            CustomerCount = 0
        };
    }

    /// <summary>
    /// 更新國家名稱。
    /// </summary>
    public async Task<CountryListDto?> UpdateAsync(int id, CountryCreateUpdateDto dto)
    {
        var entity = await _db.Countries
            .FirstOrDefaultAsync(co => co.Countryid == id);

        if (entity == null)
            return null;

        entity.Title = dto.Title.Trim();
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除國家。若國家下仍有客戶，回傳業務錯誤訊息。
    /// </summary>
    public async Task<(bool Found, string? Error)> DeleteAsync(int id)
    {
        var entity = await _db.Countries
            .Include(co => co.Customers)
            .FirstOrDefaultAsync(co => co.Countryid == id);

        if (entity == null)
            return (false, null);

        if (entity.Customers.Count > 0)
            return (true, "無法刪除，此國家下仍有客戶。");

        _db.Countries.Remove(entity);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
