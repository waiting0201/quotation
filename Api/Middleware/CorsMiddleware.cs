using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// 處理 CORS：允許 Angular 開發伺服器 (localhost:4200) 跨域請求。
/// 生產環境應從 config 讀取允許的 origin 清單。
///
/// 此 middleware 為無狀態（thread-safe），註冊為 Singleton。
/// </summary>
public class CorsMiddleware : IMiddleware
{
    private static readonly string[] AllowedOrigins =
    [
        "http://localhost:4200",
        "https://localhost:4200"
    ];

    public async Task InvokeAsync(RouteContext context, Func<Task> next)
    {
        var response = context.Request.HttpContext.Response;
        var origin = context.Request.Headers.Origin.ToString();

        if (AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            response.Headers["Access-Control-Allow-Origin"] = origin;
        }

        response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
        response.Headers["Access-Control-Allow-Credentials"] = "true";
        response.Headers["Access-Control-Max-Age"] = "86400"; // 24 小時快取 preflight

        // 處理 preflight 請求（OPTIONS），直接回 204 不進入後續 pipeline
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 204;
            return; // 短路，不呼叫 next
        }

        await next();
    }
}
