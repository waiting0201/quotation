using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Host;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 維護清單管理服務
/// - GetListAsync:  Dapper 查詢，支援按項目名稱關鍵字搜尋
/// - GetByIdAsync:  EF Core 查詢單筆詳情
/// - CreateAsync:   EF Core 新增（hostid 為 identity 自動遞增）
/// - UpdateAsync:   EF Core 更新
/// - DeleteAsync:   EF Core 刪除
/// </summary>
public class HostService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    public HostService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得維護清單，可選擇依項目名稱關鍵字搜尋。
    /// 使用 Dapper 直接執行 SQL，避免 EF Core 在簡單查詢上產生額外 round-trip。
    /// </summary>
    /// <param name="search">搜尋關鍵字（null 或空字串時回傳全部）</param>
    public async Task<List<HostListDto>> GetListAsync(string? search)
    {
        const string baseSql = """
            SELECT
                h.hostid      AS HostId,
                h.item        AS Item,
                h.url         AS Url,
                h.startdate   AS StartDate,
                h.expiredate  AS ExpireDate
            FROM hosts h
            """;

        string sql;
        object? param;

        if (!string.IsNullOrWhiteSpace(search))
        {
            // 使用參數化 LIKE 防範 SQL Injection
            sql = baseSql + " WHERE h.item LIKE @Search ORDER BY h.expiredate, h.item";
            param = new { Search = $"%{search.Trim()}%" };
        }
        else
        {
            sql = baseSql + " ORDER BY h.expiredate, h.item";
            param = null;
        }

        var results = await _dapper.QueryAsync<HostListDto>(sql, param);
        return results.AsList();
    }

    /// <summary>
    /// 取得單一維護項目詳情。
    /// </summary>
    /// <returns>找不到記錄時回傳 null</returns>
    public async Task<HostListDto?> GetByIdAsync(int id)
    {
        var host = await _db.Hosts
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Hostid == id);

        if (host == null)
            return null;

        return MapToDto(host);
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增維護項目。
    /// hostid 為 identity 自動遞增，不需手動指定。
    /// </summary>
    public async Task<HostListDto> CreateAsync(HostCreateUpdateDto dto)
    {
        var host = new Models.Host
        {
            // hostid 由資料庫自動產生，不設定
            Item        = dto.Item.Trim(),
            Url         = dto.Url?.Trim(),
            Startdate   = dto.StartDate,
            Expiredate  = dto.ExpireDate
        };

        _db.Hosts.Add(host);
        await _db.SaveChangesAsync();

        return MapToDto(host);
    }

    /// <summary>
    /// 更新維護項目。
    /// </summary>
    /// <returns>找不到記錄時回傳 null</returns>
    public async Task<HostListDto?> UpdateAsync(int id, HostCreateUpdateDto dto)
    {
        var host = await _db.Hosts.FirstOrDefaultAsync(h => h.Hostid == id);

        if (host == null)
            return null;

        host.Item       = dto.Item.Trim();
        host.Url        = dto.Url?.Trim();
        host.Startdate  = dto.StartDate;
        host.Expiredate = dto.ExpireDate;

        await _db.SaveChangesAsync();

        return MapToDto(host);
    }

    /// <summary>
    /// 刪除維護項目。
    /// </summary>
    /// <returns>
    ///   true  — 刪除成功
    ///   false — 找不到記錄
    /// </returns>
    public async Task<bool> DeleteAsync(int id)
    {
        var host = await _db.Hosts.FirstOrDefaultAsync(h => h.Hostid == id);

        if (host == null)
            return false;

        _db.Hosts.Remove(host);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    private static HostListDto MapToDto(Models.Host host) => new()
    {
        HostId     = host.Hostid,
        Item       = host.Item,
        Url        = host.Url,
        StartDate  = host.Startdate,
        ExpireDate = host.Expiredate
    };
}
