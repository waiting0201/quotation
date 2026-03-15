using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// 建立並執行 middleware 責任鏈（Chain of Responsibility）
/// </summary>
public class MiddlewarePipeline
{
    private readonly List<IMiddleware> _middlewares = new();

    public MiddlewarePipeline Use(IMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// 依序執行 middleware chain，最後呼叫 terminal handler
    /// </summary>
    public async Task ExecuteAsync(RouteContext context, Func<Task> terminal)
    {
        // 從最後一層往前建立巢狀 next 委派（洋蔥模型）
        Func<Task> next = terminal;

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var capturedNext = next;
            next = () => middleware.InvokeAsync(context, capturedNext);
        }

        await next();
    }
}
