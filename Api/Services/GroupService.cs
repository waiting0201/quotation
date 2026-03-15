using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Settings;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 群組管理服務
/// - GetListAsync: Dapper 聯查，一次取得群組清單含使用者人數
/// - GetByIdAsync: EF Core eager loading 取得詳情含權限矩陣
/// - CreateAsync / UpdateAsync: EF Core transaction 維護 group + grouplim 一致性
/// - DeleteAsync: 先檢查是否有使用者隸屬，才允許刪除
/// </summary>
public class GroupService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    public GroupService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得所有群組清單，附帶各群組的使用者人數。
    /// 使用 Dapper 避免 EF Core 在簡單聚合查詢上產生不必要的 round-trip。
    /// </summary>
    public async Task<List<GroupListDto>> GetListAsync()
    {
        const string sql = """
            SELECT
                g.groupid  AS GroupId,
                g.title    AS Title,
                COUNT(u.userid) AS UserCount
            FROM [group] g
            LEFT JOIN [user] u ON u.groupid = g.groupid
            GROUP BY g.groupid, g.title
            ORDER BY g.title
            """;

        var results = await _dapper.QueryAsync<GroupListDto>(sql);
        return results.AsList();
    }

    /// <summary>
    /// 取得單一群組詳情，含完整的 grouplim 權限矩陣。
    /// </summary>
    /// <returns>找不到群組時回傳 null</returns>
    public async Task<GroupDetailDto?> GetByIdAsync(Guid id)
    {
        var group = await _db.Groups
            .AsNoTracking()
            .Include(g => g.Grouplims)
            .FirstOrDefaultAsync(g => g.Groupid == id);

        if (group == null)
            return null;

        return new GroupDetailDto
        {
            GroupId = group.Groupid,
            Title = group.Title ?? string.Empty,
            Permissions = group.Grouplims.Select(gl => new GroupPermissionDto
            {
                LimId    = gl.Limid,
                IsQuery  = gl.Isquery,
                IsInsert = gl.Isinsert,
                IsUpdate = gl.Isupdate,
                IsDelete = gl.Isdelete
            }).ToList()
        };
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增群組並批次寫入初始權限矩陣。
    /// 使用交易確保 group + grouplim 兩張表的原子性。
    /// </summary>
    public async Task<GroupDetailDto> CreateAsync(GroupCreateUpdateDto dto)
    {
        var group = new Group
        {
            Groupid = Guid.NewGuid(),
            Title   = dto.Title.Trim()
        };

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            _db.Groups.Add(group);

            foreach (var perm in dto.Permissions)
            {
                _db.Grouplims.Add(new Grouplim
                {
                    Groupid  = group.Groupid,
                    Limid    = perm.LimId,
                    Isquery  = perm.IsQuery,
                    Isinsert = perm.IsInsert,
                    Isupdate = perm.IsUpdate,
                    Isdelete = perm.IsDelete
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new GroupDetailDto
        {
            GroupId     = group.Groupid,
            Title       = group.Title,
            Permissions = dto.Permissions
        };
    }

    /// <summary>
    /// 更新群組標題，並以「完整取代」語意更新 grouplim 權限矩陣。
    /// 先刪除既有的所有 grouplim 記錄，再批次新增，避免複雜的差異比對。
    /// </summary>
    /// <returns>找不到群組時回傳 null</returns>
    public async Task<GroupDetailDto?> UpdateAsync(Guid id, GroupCreateUpdateDto dto)
    {
        var group = await _db.Groups
            .Include(g => g.Grouplims)
            .FirstOrDefaultAsync(g => g.Groupid == id);

        if (group == null)
            return null;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // 更新群組標題
            group.Title = dto.Title.Trim();

            // 完整取代 grouplim：先刪除再新增，分兩次 SaveChanges 避免 change tracker key 衝突
            _db.Grouplims.RemoveRange(group.Grouplims);
            await _db.SaveChangesAsync();

            foreach (var perm in dto.Permissions)
            {
                _db.Grouplims.Add(new Grouplim
                {
                    Groupid  = group.Groupid,
                    Limid    = perm.LimId,
                    Isquery  = perm.IsQuery,
                    Isinsert = perm.IsInsert,
                    Isupdate = perm.IsUpdate,
                    Isdelete = perm.IsDelete
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new GroupDetailDto
        {
            GroupId     = group.Groupid,
            Title       = group.Title ?? string.Empty,
            Permissions = dto.Permissions
        };
    }

    /// <summary>
    /// 刪除群組。
    /// 若有使用者仍隸屬該群組，拒絕刪除並回傳錯誤訊息，
    /// 防止孤兒使用者（user.groupid 指向已刪除的群組）。
    /// </summary>
    /// <returns>
    ///   (true, null)  — 刪除成功
    ///   (false, null) — 找不到群組
    ///   (false, msg)  — 存在使用者，拒絕刪除（msg 為錯誤訊息）
    /// </returns>
    public async Task<(bool Found, string? ErrorMessage)> DeleteAsync(Guid id)
    {
        var group = await _db.Groups
            .Include(g => g.Grouplims)
            .FirstOrDefaultAsync(g => g.Groupid == id);

        if (group == null)
            return (false, null);

        // 安全檢查：不允許刪除仍有使用者的群組
        var userCount = await _db.Users.CountAsync(u => u.Groupid == id);
        if (userCount > 0)
            return (true, $"此群組仍有 {userCount} 位使用者，請先將使用者移至其他群組後再刪除。");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // 先刪 grouplim（FK 約束），再刪 group
            _db.Grouplims.RemoveRange(group.Grouplims);
            _db.Groups.Remove(group);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return (true, null);
    }
}
