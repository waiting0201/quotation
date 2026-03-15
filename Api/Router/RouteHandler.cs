using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Common;

namespace QuotationApi.Router;

/// <summary>
/// 解析請求路由、比對路由表、提取路徑參數、呼叫 controller handler。
///
/// ╔══════════════════════════════════════════════════════════════════════╗
/// ║  ⚠ 設計決策：此類別必須註冊為 Singleton                              ║
/// ║                                                                    ║
/// ║  RouteHandler 在建構時預編譯所有路由 Regex（PrecompilePatterns），     ║
/// ║  這些 Regex 不會改變，應該只編譯一次。                                ║
/// ║                                                                    ║
/// ║  請求時透過 IServiceProvider 延遲解析 Controller，                   ║
/// ║  IServiceProvider 由 ApiFunction 傳入（每次請求的 scoped provider）。 ║
/// ║  RouteHandler 本身不持有任何 Scoped 依賴，可安全作為 Singleton。       ║
/// ╚══════════════════════════════════════════════════════════════════════╝
/// </summary>
public class RouteHandler
{
    private readonly RouteTable _routeTable;

    // 將路由樣式（如 "customers/{id}/details"）轉換為 Regex 並快取
    private readonly Dictionary<string, (Regex Regex, List<string> ParamNames)> _compiledPatterns = new();

    public RouteHandler(RouteTable routeTable)
    {
        _routeTable = routeTable;
        PrecompilePatterns();
    }

    /// <summary>
    /// 依 HTTP method + route 找到對應 handler 並執行；
    /// 找不到時回傳 404，方法不符時回傳 405。
    /// </summary>
    /// <param name="context">請求上下文</param>
    /// <param name="serviceProvider">
    /// 當前請求的 scoped IServiceProvider，用於延遲解析 Controller。
    /// 必須來自當前 HTTP 請求的 scope，不可使用 root provider。
    /// </param>
    public async Task<IActionResult> HandleAsync(RouteContext context, IServiceProvider serviceProvider)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var route = context.Route.Trim('/');

        // 先找出所有樣式匹配的路由（無論 method）
        var matchedByPattern = new List<(RouteDefinition Def, Match Match, List<string> ParamNames)>();

        foreach (var def in _routeTable.Routes)
        {
            if (!_compiledPatterns.TryGetValue(def.Pattern, out var compiled))
                continue;

            var match = compiled.Regex.Match(route);
            if (match.Success)
                matchedByPattern.Add((def, match, compiled.ParamNames));
        }

        if (matchedByPattern.Count == 0)
        {
            return new NotFoundObjectResult(
                ApiErrorResponse.Create("NOT_FOUND", $"Route '{route}' not found."));
        }

        // 再從中找方法吻合的
        var matched = matchedByPattern.FirstOrDefault(x => x.Def.Method == method);
        if (matched == default)
        {
            var allowedMethods = matchedByPattern.Select(x => x.Def.Method).Distinct();
            return new ObjectResult(
                ApiErrorResponse.Create("METHOD_NOT_ALLOWED",
                    $"Method {method} not allowed. Allowed: {string.Join(", ", allowedMethods)}"))
            {
                StatusCode = 405
            };
        }

        // 提取路徑參數
        for (int i = 0; i < matched.ParamNames.Count; i++)
        {
            var paramName = matched.ParamNames[i];
            var paramValue = matched.Match.Groups[paramName].Value;
            context.PathParams[paramName] = paramValue;
        }

        // 透過 HandlerFactory 延遲解析 Controller 並執行 handler
        return await matched.Def.HandlerFactory(serviceProvider, context);
    }

    // ── 私有方法 ────────────────────────────────────────────────────────────

    private void PrecompilePatterns()
    {
        foreach (var def in _routeTable.Routes)
        {
            if (_compiledPatterns.ContainsKey(def.Pattern))
                continue;

            var compiled = CompilePattern(def.Pattern);
            _compiledPatterns[def.Pattern] = compiled;
        }
    }

    /// <summary>
    /// 將路由樣式轉換為 Regex 並提取參數名稱列表
    /// 例如："customers/{id}/orders/{orderId}"
    ///   -> Regex: ^customers/(?&lt;id&gt;[^/]+)/orders/(?&lt;orderId&gt;[^/]+)$
    ///   -> ParamNames: ["id", "orderId"]
    /// </summary>
    private static (Regex Regex, List<string> ParamNames) CompilePattern(string pattern)
    {
        var paramNames = new List<string>();

        // 先從原始樣式中提取 {paramName} 並替換為暫存標記，
        // 再對其餘文字做 Regex.Escape，最後將標記替換為具名 capture group。
        // 這樣可避免 .NET 7+ 中 Regex.Escape 不再轉義 { } 的問題。
        var placeholders = new List<string>();
        var withPlaceholders = Regex.Replace(pattern, @"\{(\w+)\}", m =>
        {
            var name = m.Groups[1].Value;
            paramNames.Add(name);
            var placeholder = $"__PARAM_{placeholders.Count}__";
            placeholders.Add(name);
            return placeholder;
        });

        var escaped = Regex.Escape(withPlaceholders);

        for (int i = 0; i < placeholders.Count; i++)
        {
            escaped = escaped.Replace($"__PARAM_{i}__", $"(?<{placeholders[i]}>[^/]+)");
        }

        var regex = new Regex($"^{escaped}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return (regex, paramNames);
    }
}
