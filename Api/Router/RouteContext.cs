using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Auth;
using QuotationApi.DTOs.Common;

namespace QuotationApi.Router;

/// <summary>
/// 每個請求的上下文物件，在 middleware pipeline 及 controller 間共享
/// </summary>
public class RouteContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public HttpRequest Request { get; }

    /// <summary>
    /// 去除前綴後的路由字串，例如 "auth/login" 或 "customers/123"
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// 從路由樣式中擷取的路徑參數，例如 { "id": "123" }
    /// </summary>
    public Dictionary<string, string> PathParams { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 由 JwtAuthMiddleware 設定，驗證通過後代入
    /// </summary>
    public ClaimsPrincipal? CurrentUser { get; set; }

    /// <summary>
    /// 由 middleware 設定，代表此次請求已被處理（短路）
    /// </summary>
    public IActionResult? Result { get; set; }

    public RouteContext(HttpRequest request, string route)
    {
        Request = request;
        Route = route;
    }

    // ── 回應輔助方法 ──────────────────────────────────────────────────────────

    public IActionResult Ok<T>(T data)
        => new OkObjectResult(ApiResponse<T>.Success(data));

    public IActionResult OkPaged<T>(PaginatedResponse<T> paged)
        => new OkObjectResult(paged);

    public IActionResult Created<T>(T data)
        => new ObjectResult(ApiResponse<T>.Success(data)) { StatusCode = 201 };

    public IActionResult NoContent()
        => new NoContentResult();

    public IActionResult BadRequest(string message, string? details = null)
        => new BadRequestObjectResult(ApiErrorResponse.Create("BAD_REQUEST", message, details));

    public IActionResult NotFound(string message = "Resource not found")
        => new NotFoundObjectResult(ApiErrorResponse.Create("NOT_FOUND", message));

    public IActionResult Unauthorized(string message = "Unauthorized")
        => new UnauthorizedObjectResult(ApiErrorResponse.Create("UNAUTHORIZED", message));

    public IActionResult Forbidden(string message = "Forbidden")
        => new ObjectResult(ApiErrorResponse.Create("FORBIDDEN", message)) { StatusCode = 403 };

    public IActionResult Conflict(string message)
        => new ConflictObjectResult(ApiErrorResponse.Create("CONFLICT", message));

    public IActionResult InternalServerError(string message = "Internal server error")
        => new ObjectResult(ApiErrorResponse.Create("INTERNAL_ERROR", message)) { StatusCode = 500 };

    // ── 請求輔助方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 將 request body 反序列化為指定型別；失敗拋 ArgumentException
    /// </summary>
    public async Task<T> ReadBodyAsync<T>()
    {
        try
        {
            var result = await JsonSerializer.DeserializeAsync<T>(Request.Body, JsonOptions);
            if (result == null)
                throw new ArgumentException("Request body is empty or invalid.");
            return result;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}");
        }
    }

    /// <summary>
    /// 嘗試讀取 body，失敗時回傳 null（不拋例外）
    /// </summary>
    public async Task<T?> TryReadBodyAsync<T>() where T : class
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(Request.Body, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 從 PathParams 取得路徑參數，若不存在則拋 KeyNotFoundException
    /// </summary>
    public string RequirePathParam(string key)
    {
        if (PathParams.TryGetValue(key, out var value))
            return value;
        throw new KeyNotFoundException($"Path parameter '{key}' not found.");
    }
}
