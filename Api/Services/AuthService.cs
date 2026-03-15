using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuotationApi.DTOs.Auth;
using QuotationApi.Helpers;
using QuotationApi.Models;

namespace QuotationApi.Services;

public class AuthService
{
    // 管理員帳號（硬編碼，不走資料庫）
    private const string AdminEmail = "admin@weypro.com.tw";
    private const string AdminPassword = "B22H8Se1";
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly QuotationDbContext _db;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(QuotationDbContext db, JwtHelper jwtHelper, ILogger<AuthService> logger)
    {
        _db = db;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    /// <summary>
    /// 驗證帳密並回傳 LoginResponse（含 JWT token 及 user 資訊）；
    /// 驗證失敗回傳 null。
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        UserDto? userDto;

        // 優先檢查管理員帳號（不走 DB，避免被 DB 不可用影響）
        if (email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (!password.Equals(AdminPassword, StringComparison.Ordinal))
            {
                _logger.LogWarning("Admin login failed for {Email}", email);
                return null;
            }

            userDto = new UserDto
            {
                Userid = AdminUserId.ToString(),
                Email = AdminEmail,
                Name = "Administrator",
                Groupid = null,
                Permissions = [] // admin 不限制任何權限，前端可特殊處理
            };
        }
        else
        {
            // 一般使用者：查 DB + SHA1 比對
            var hashedPassword = PasswordHelper.HashPassword(password);

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Email == email && u.Status == true)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found or inactive for {Email}", email);
                return null;
            }

            // 使用 constant-time 比較防 timing attack
            if (!PasswordHelper.VerifyPassword(password, user.Password ?? string.Empty))
            {
                _logger.LogWarning("Login failed: wrong password for {Email}", email);
                return null;
            }

            var permissions = await GetPermissionsAsync(user.Userid, user.Groupid);

            userDto = new UserDto
            {
                Userid = user.Userid.ToString(),
                Email = user.Email ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Groupid = user.Groupid?.ToString(),
                Permissions = permissions
            };
        }

        var token = _jwtHelper.GenerateToken(userDto);

        return new LoginResponse
        {
            Token = token,
            User = userDto
        };
    }

    /// <summary>
    /// 依 userid 取得 UserDto（含權限）；找不到回傳 null
    /// </summary>
    public async Task<UserDto?> GetUserInfoAsync(Guid userid)
    {
        // admin 特殊處理
        if (userid == AdminUserId)
        {
            return new UserDto
            {
                Userid = AdminUserId.ToString(),
                Email = AdminEmail,
                Name = "Administrator",
                Groupid = null,
                Permissions = []
            };
        }

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Userid == userid)
            .FirstOrDefaultAsync();

        if (user == null) return null;

        var permissions = await GetPermissionsAsync(user.Userid, user.Groupid);

        return new UserDto
        {
            Userid = user.Userid.ToString(),
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Groupid = user.Groupid?.ToString(),
            Permissions = permissions
        };
    }

    // ── Private ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 合併 userlim（個人）與 grouplim（群組）的權限，
    /// 個人設定優先覆蓋群組設定。
    /// </summary>
    private async Task<List<PermissionDto>> GetPermissionsAsync(Guid userid, Guid? groupid)
    {
        // 一次撈出所有 lim 定義（通常資料量小，可快取）
        var allLims = await _db.Lims
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Limid);

        // 群組權限（基礎）
        var groupPerms = new Dictionary<int, PermissionDto>();
        if (groupid.HasValue)
        {
            var grouplims = await _db.Grouplims
                .AsNoTracking()
                .Where(gl => gl.Groupid == groupid.Value)
                .ToListAsync();

            foreach (var gl in grouplims)
            {
                if (!allLims.TryGetValue(gl.Limid, out var lim)) continue;
                groupPerms[gl.Limid] = new PermissionDto
                {
                    Limid = gl.Limid,
                    Key = lim.Key ?? string.Empty,
                    Value = lim.Value,
                    IsQuery = gl.Isquery,
                    IsInsert = gl.Isinsert,
                    IsUpdate = gl.Isupdate,
                    IsDelete = gl.Isdelete
                };
            }
        }

        // 個人權限（覆蓋群組）
        var userLims = await _db.Userlims
            .AsNoTracking()
            .Where(ul => ul.Userid == userid)
            .ToListAsync();

        foreach (var ul in userLims)
        {
            if (!allLims.TryGetValue(ul.Limid, out var lim)) continue;
            // 個人設定直接覆蓋（無論群組是否有設定）
            groupPerms[ul.Limid] = new PermissionDto
            {
                Limid = ul.Limid,
                Key = lim.Key ?? string.Empty,
                Value = lim.Value,
                IsQuery = ul.Isquery,
                IsInsert = ul.Isinsert,
                IsUpdate = ul.Isupdate,
                IsDelete = ul.Isdelete
            };
        }

        return [.. groupPerms.Values.OrderBy(p => p.Limid)];
    }
}
