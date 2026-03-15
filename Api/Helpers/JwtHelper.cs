using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuotationApi.DTOs.Auth;

namespace QuotationApi.Helpers;

public class JwtHelper
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtHelper(IConfiguration configuration)
    {
        // 優先讀環境變數，fallback 到 dev 預設值
        _secret = configuration["JwtSecret"]
                  ?? configuration["Values:JwtSecret"]
                  ?? "weypro-quotation-system-jwt-secret-key-2024";
        _issuer = "quotation.weypro.com";
        _audience = "quotation.weypro.com";
    }

    /// <summary>
    /// 產生 JWT Token，有效期 24 小時（Asia/Taipei 時區）
    /// </summary>
    public string GenerateToken(UserDto user)
    {
        var taiwanZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Taipei Standard Time" : "Asia/Taipei");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanZone);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Userid),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim("groupid", user.Groupid ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now, taiwanZone.GetUtcOffset(now)).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            // 以 UTC 設定過期時間，底層 JWT library 統一以 UTC 處理
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 驗證 Token 並回傳 ClaimsPrincipal；驗證失敗回傳 null
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = handler.ValidateToken(token, validationParams, out _);
            return principal;
        }
        catch
        {
            // 任何驗證失敗都視為無效 token，不對外拋出例外
            return null;
        }
    }

    /// <summary>
    /// 從 ClaimsPrincipal 提取 userid（sub claim）
    /// </summary>
    public static string? GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

    /// <summary>
    /// 從 ClaimsPrincipal 提取 email
    /// </summary>
    public static string? GetEmail(ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtRegisteredClaimNames.Email);

    /// <summary>
    /// 從 ClaimsPrincipal 提取 name
    /// </summary>
    public static string? GetName(ClaimsPrincipal principal)
        => principal.FindFirstValue("name");
}
