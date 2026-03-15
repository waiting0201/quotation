using QuotationApi.Router;

namespace QuotationApi.Middleware;

/// <summary>
/// 建立並執行 middleware 責任鏈（Chain of Responsibility）。
///
/// ╔══════════════════════════════════════════════════════════════════════╗
/// ║  ⚠ 設計決策：此類別必須註冊為 Singleton                              ║
/// ║                                                                    ║
/// ║  Middleware 清單在應用程式啟動後不會改變，Pipeline 只需組裝一次。       ║
/// ║  所有 middleware 實例必須是執行緒安全的（無狀態或僅依賴 Singleton）。   ║
/// ║                                                                    ║
/// ║  若 middleware 需要 Scoped 依賴（如 DbContext），應在 InvokeAsync     ║
/// ║  中透過 HttpContext.RequestServices 取得，而非建構子注入。            ║
/// ║                                                                    ║
/// ║  ❌ 錯誤：AddScoped&lt;MiddlewarePipeline&gt;() — 每次請求重建管線        ║
/// ║  ✅ 正確：AddSingleton&lt;MiddlewarePipeline&gt;() — 管線只組裝一次      ║
/// ╚══════════════════════════════════════════════════════════════════════╝
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
