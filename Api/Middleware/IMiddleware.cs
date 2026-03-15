using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// 中介層介面：InvokeAsync 中呼叫 next() 繼續管線，
/// 或設定 context.Result 後不呼叫 next 來短路請求。
/// </summary>
public interface IMiddleware
{
    Task InvokeAsync(RouteContext context, Func<Task> next);
}
