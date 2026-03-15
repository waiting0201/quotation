using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuotationApi.DTOs.Common;
using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// 全域例外處理中介層：
/// - 所有未被處理的例外在此捕獲，回傳標準 JSON 錯誤格式
/// - 不對外暴露 stack trace 或內部細節
/// - 依例外類型回傳適當的 HTTP status code
/// </summary>
public class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(RouteContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (ArgumentException ex)
        {
            // 輸入驗證錯誤：400
            _logger.LogWarning(ex, "Validation error on route {Route}", context.Route);
            context.Result = new BadRequestObjectResult(
                ApiErrorResponse.Create("BAD_REQUEST", ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            // 權限不足：403
            _logger.LogWarning(ex, "Forbidden access on route {Route}", context.Route);
            context.Result = new ObjectResult(
                ApiErrorResponse.Create("FORBIDDEN", "You don't have permission to perform this action."))
            { StatusCode = 403 };
        }
        catch (KeyNotFoundException ex)
        {
            // 資源不存在：404
            _logger.LogWarning(ex, "Resource not found on route {Route}", context.Route);
            context.Result = new NotFoundObjectResult(
                ApiErrorResponse.Create("NOT_FOUND", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            // 業務邏輯違規：422
            _logger.LogWarning(ex, "Business rule violation on route {Route}", context.Route);
            context.Result = new ObjectResult(
                ApiErrorResponse.Create("UNPROCESSABLE", ex.Message))
            { StatusCode = 422 };
        }
        catch (Exception ex)
        {
            // 未預期的錯誤：500
            // 僅記錄詳細錯誤，不對外洩露
            _logger.LogError(ex, "Unhandled exception on route {Route}", context.Route);
            context.Result = new ObjectResult(
                ApiErrorResponse.Create("INTERNAL_ERROR", "An unexpected error occurred. Please try again later."))
            { StatusCode = 500 };
        }
    }
}
