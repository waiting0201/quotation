using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuotationApi.DTOs.Common;
using QuotationApi.Helpers;
using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// JWT 認證中介層：
/// - 從 Authorization header 提取 Bearer token
/// - 驗證 token 有效性並設定 context.CurrentUser
/// - 公開路由（public routes）略過驗證
/// </summary>
public class JwtAuthMiddleware : IMiddleware
{
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<JwtAuthMiddleware> _logger;

    /// <summary>
    /// 不需要 JWT 驗證的公開路由（前綴比對）
    /// </summary>
    private static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "auth/login"
    };

    public JwtAuthMiddleware(JwtHelper jwtHelper, ILogger<JwtAuthMiddleware> logger)
    {
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    public async Task InvokeAsync(RouteContext context, Func<Task> next)
    {
        var route = context.Route.Trim('/');

        // 公開路由直接放行
        if (IsPublicRoute(route))
        {
            await next();
            return;
        }

        // 提取 Authorization header
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(
                ApiErrorResponse.Create("UNAUTHORIZED", "Missing or invalid Authorization header."));
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var principal = _jwtHelper.ValidateToken(token);

        if (principal == null)
        {
            _logger.LogWarning("Invalid JWT token for route {Route}", route);
            context.Result = new UnauthorizedObjectResult(
                ApiErrorResponse.Create("UNAUTHORIZED", "Token is invalid or expired."));
            return;
        }

        // 設定已驗證的 principal，供後續 middleware 及 controller 使用
        context.CurrentUser = principal;

        await next();
    }

    private static bool IsPublicRoute(string route)
    {
        foreach (var publicRoute in PublicRoutes)
        {
            if (route.Equals(publicRoute, StringComparison.OrdinalIgnoreCase) ||
                route.StartsWith(publicRoute + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
