using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using QuotationApi.Middleware;
using QuotationApi.Router;

namespace QuotationApi.Functions;

/// <summary>
/// 單一入口 Azure Function：
/// 所有 /api/{*route} 請求都路由到此，再由 middleware pipeline + RouteHandler 分發。
/// 使用 catch-all route 參數 {*route} 以支援任意深度路徑。
/// </summary>
public class ApiFunction
{
    private readonly MiddlewarePipeline _pipeline;
    private readonly RouteHandler _routeHandler;
    private readonly ILogger<ApiFunction> _logger;

    public ApiFunction(
        MiddlewarePipeline pipeline,
        RouteHandler routeHandler,
        ILogger<ApiFunction> logger)
    {
        _pipeline = pipeline;
        _routeHandler = routeHandler;
        _logger = logger;
    }

    [Function("Api")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options",
            Route = "api/{*route}")]
        HttpRequest req,
        string? route)
    {
        // 將 null 的 route 正規化（對應 /api/ 的請求）
        route ??= string.Empty;

        _logger.LogInformation("→ {Method} /api/{Route}", req.Method, route);

        var context = new RouteContext(req, route);

        // 取得當前請求的 scoped IServiceProvider，用於延遲解析 Controller。
        // ⚠ 必須使用 HttpContext.RequestServices（scoped），不可使用 root provider，
        //   否則 DbContext 等 scoped 服務會跨請求共用，導致資料不一致。
        var scopedProvider = req.HttpContext.RequestServices;

        // 執行 middleware pipeline，最後呼叫 RouteHandler
        await _pipeline.ExecuteAsync(context, async () =>
        {
            // Terminal handler：middleware 全部通過後才執行路由分發
            // OPTIONS 已在 CorsMiddleware 短路，這裡不會收到
            context.Result = await _routeHandler.HandleAsync(context, scopedProvider);
        });

        // 若 middleware 短路（如 CORS preflight 回 204），Result 可能為 null
        // 此時回應已直接寫入 HttpResponse，回傳 EmptyResult 避免 double-write
        return context.Result ?? new EmptyResult();
    }
}
