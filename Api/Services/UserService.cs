using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Settings;
using QuotationApi.Helpers;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 使用者管理服務
/// - GetListAsync:       Dapper 聯查，一次取得使用者清單含所屬群組名稱
/// - GetByIdAsync:       EF Core eager loading 取得詳情含個人權限矩陣
/// - CreateAsync:        EF Core transaction 維護 user + userlim 一致性
/// - UpdateAsync:        EF Core transaction，完整取代 userlim 權限矩陣
/// - ChangePasswordAsync: EF Core 更新密碼（SHA1+salt 雜湊）
/// - DeleteAsync:        先刪 userlim（FK 約束），再刪 user
/// </summary>
public class UserService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    // 台北時區，用於 updatetime 欄位
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public UserService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得所有使用者清單，LEFT JOIN group 取得群組名稱。
    /// 使用 Dapper 避免 EF Core 在簡單聯查上產生不必要的 round-trip。
    /// </summary>
    public async Task<List<UserListDto>> GetListAsync()
    {
        const string sql = """
            SELECT
                u.userid      AS UserId,
                u.name        AS Name,
                u.email       AS Email,
                u.groupid     AS GroupId,
                g.title       AS GroupTitle,
                CAST(ISNULL(u.status, 0) AS BIT) AS Status,
                u.updatetime  AS UpdateTime
            FROM [user] u
            LEFT JOIN [group] g ON g.groupid = u.groupid
            ORDER BY u.name
            """;

        var results = await _dapper.QueryAsync<UserListDto>(sql);
        return results.AsList();
    }

    /// <summary>
    /// 取得單一使用者詳情，含完整的 userlim 個人權限矩陣。
    /// </summary>
    /// <returns>找不到使用者時回傳 null</returns>
    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Userlims)
            .FirstOrDefaultAsync(u => u.Userid == id);

        if (user == null)
            return null;

        return new UserDetailDto
        {
            UserId     = user.Userid,
            Name       = user.Name ?? string.Empty,
            Email      = user.Email ?? string.Empty,
            GroupId    = user.Groupid,
            Status     = user.Status ?? false,
            UpdateTime = user.Updatetime,
            Permissions = user.Userlims.Select(ul => new UserPermissionDto
            {
                LimId    = ul.Limid,
                IsQuery  = ul.Isquery,
                IsInsert = ul.Isinsert,
                IsUpdate = ul.Isupdate,
                IsDelete = ul.Isdelete
            }).ToList()
        };
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增使用者並批次寫入初始 userlim 權限矩陣。
    /// 先檢查 Email 是否重複（case insensitive）。
    /// 使用交易確保 user + userlim 兩張表的原子性。
    /// </summary>
    /// <exception cref="InvalidOperationException">Email 已被使用時拋出</exception>
    public async Task<UserDetailDto> CreateAsync(UserCreateDto dto)
    {
        // 檢查 Email 唯一性（case insensitive）
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email != null &&
                           u.Email.ToLower() == dto.Email.ToLower().Trim());

        if (emailExists)
            throw new InvalidOperationException($"Email '{dto.Email.Trim()}' 已被其他使用者使用。");

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        var user = new User
        {
            Userid     = Guid.NewGuid(),
            Name       = dto.Name.Trim(),
            Email      = dto.Email.ToLower().Trim(),
            Password   = PasswordHelper.HashPassword(dto.Password),
            Groupid    = dto.GroupId,
            Status     = dto.Status,
            Updatetime = now
        };

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            _db.Users.Add(user);

            foreach (var perm in dto.Permissions)
            {
                _db.Userlims.Add(new Userlim
                {
                    Userid   = user.Userid,
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

        return new UserDetailDto
        {
            UserId      = user.Userid,
            Name        = user.Name,
            Email       = user.Email,
            GroupId     = user.Groupid,
            Status      = user.Status ?? false,
            UpdateTime  = user.Updatetime,
            Permissions = dto.Permissions
        };
    }

    /// <summary>
    /// 更新使用者基本資料，並以「完整取代」語意更新 userlim 權限矩陣。
    /// 先刪除既有的所有 userlim 記錄，再批次新增，避免複雜的差異比對。
    /// </summary>
    /// <returns>找不到使用者時回傳 null</returns>
    /// <exception cref="InvalidOperationException">Email 已被其他使用者使用時拋出</exception>
    public async Task<UserDetailDto?> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Userlims)
            .FirstOrDefaultAsync(u => u.Userid == id);

        if (user == null)
            return null;

        // 檢查 Email 唯一性，排除自身
        var emailExists = await _db.Users
            .AnyAsync(u => u.Userid != id &&
                           u.Email != null &&
                           u.Email.ToLower() == dto.Email.ToLower().Trim());

        if (emailExists)
            throw new InvalidOperationException($"Email '{dto.Email.Trim()}' 已被其他使用者使用。");

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // 更新使用者欄位
            user.Name       = dto.Name.Trim();
            user.Email      = dto.Email.ToLower().Trim();
            user.Groupid    = dto.GroupId;
            user.Status     = dto.Status;
            user.Updatetime = now;

            // 完整取代 userlim：先刪除再新增，分兩次 SaveChanges 避免 change tracker key 衝突
            _db.Userlims.RemoveRange(user.Userlims);
            await _db.SaveChangesAsync();

            foreach (var perm in dto.Permissions)
            {
                _db.Userlims.Add(new Userlim
                {
                    Userid   = user.Userid,
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

        return new UserDetailDto
        {
            UserId      = user.Userid,
            Name        = user.Name ?? string.Empty,
            Email       = user.Email ?? string.Empty,
            GroupId     = user.Groupid,
            Status      = user.Status ?? false,
            UpdateTime  = user.Updatetime,
            Permissions = dto.Permissions
        };
    }

    /// <summary>
    /// 變更使用者密碼。
    /// 新密碼以 SHA1+salt 雜湊後儲存，並更新 updatetime。
    /// </summary>
    /// <returns>找到使用者並更新成功回傳 true；找不到回傳 false</returns>
    public async Task<bool> ChangePasswordAsync(Guid id, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Userid == id);

        if (user == null)
            return false;

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        user.Password   = PasswordHelper.HashPassword(newPassword);
        user.Updatetime = now;

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 刪除使用者。
    /// 先刪 userlim（FK 約束），再刪 user，使用交易確保原子性。
    /// </summary>
    /// <returns>
    ///   true  — 刪除成功
    ///   false — 找不到使用者
    /// </returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.Userlims)
            .FirstOrDefaultAsync(u => u.Userid == id);

        if (user == null)
            return false;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // 先刪 userlim（FK 約束），再刪 user
            _db.Userlims.RemoveRange(user.Userlims);
            _db.Users.Remove(user);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return true;
    }
}
