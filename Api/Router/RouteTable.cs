using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using QuotationApi.Controllers;

namespace QuotationApi.Router;

/// <summary>
/// 路由定義項目
/// </summary>
/// <remarks>
/// HandlerFactory 接收 IServiceProvider 以延遲解析 Controller，
/// 確保 RouteTable 可以註冊為 Singleton 而不捕捉 Scoped 實例。
/// </remarks>
public record RouteDefinition(
    string Method,
    string Pattern,
    Func<IServiceProvider, RouteContext, Task<IActionResult>> HandlerFactory);

/// <summary>
/// 集中管理所有路由規則；新增 controller 時在 RegisterRoutes() 中登記即可。
///
/// ╔══════════════════════════════════════════════════════════════════════╗
/// ║  ⚠ 設計決策：此類別必須註冊為 Singleton                              ║
/// ║                                                                    ║
/// ║  RouteTable 的內容在應用程式生命週期中不會改變，                       ║
/// ║  若註冊為 Scoped，每個 HTTP 請求都會重建路由表並解析所有 Controller，   ║
/// ║  造成大量不必要的 DI 解析開銷。                                       ║
/// ║                                                                    ║
/// ║  路由定義使用 HandlerFactory（Func&lt;IServiceProvider, ...&gt;），          ║
/// ║  在請求時才從 DI 容器取出需要的 Controller，避免 Singleton 持有         ║
/// ║  Scoped 實例（Captive Dependency 反模式）。                           ║
/// ║                                                                    ║
/// ║  ❌ 錯誤：AddScoped&lt;RouteTable&gt;() — 每次請求解析全部 Controller      ║
/// ║  ✅ 正確：AddSingleton&lt;RouteTable&gt;() — 路由表只建一次               ║
/// ╚══════════════════════════════════════════════════════════════════════╝
/// </summary>
public class RouteTable
{
    private readonly List<RouteDefinition> _routes = new();

    public IReadOnlyList<RouteDefinition> Routes => _routes;

    /// <summary>
    /// 建構子不再注入任何 Controller（避免綁定 Scoped 生命週期）。
    /// 路由定義透過 HandlerFactory lambda 延遲解析 Controller。
    /// </summary>
    public RouteTable()
    {
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        // ── Auth ────────────────────────────────────────────────────────────
        Register<AuthController>("POST", "auth/login", (c, ctx) => c.Login(ctx));
        Register<AuthController>("GET",  "auth/me",    (c, ctx) => c.Me(ctx));

        // ── Dashboard ───────────────────────────────────────────────────────
        Register<DashboardController>("GET", "dashboard", (c, ctx) => c.GetDashboard(ctx));

        // ── Groups ──────────────────────────────────────────────────────────
        Register<GroupController>("GET",    "groups",      (c, ctx) => c.GetList(ctx));
        Register<GroupController>("POST",   "groups",      (c, ctx) => c.Create(ctx));
        Register<GroupController>("GET",    "groups/{id}", (c, ctx) => c.GetById(ctx, ParseGuid(ctx, "id")));
        Register<GroupController>("PUT",    "groups/{id}", (c, ctx) => c.Update(ctx, ParseGuid(ctx, "id")));
        Register<GroupController>("DELETE", "groups/{id}", (c, ctx) => c.Delete(ctx, ParseGuid(ctx, "id")));

        // ── Users ─────────────────────────────────────────────────────────────
        Register<UserController>("GET",    "users",                (c, ctx) => c.GetList(ctx));
        Register<UserController>("POST",   "users",                (c, ctx) => c.Create(ctx));
        Register<UserController>("GET",    "users/{id}",           (c, ctx) => c.GetById(ctx, ParseGuid(ctx, "id")));
        Register<UserController>("PUT",    "users/{id}",           (c, ctx) => c.Update(ctx, ParseGuid(ctx, "id")));
        Register<UserController>("PUT",    "users/{id}/password",  (c, ctx) => c.ChangePassword(ctx, ParseGuid(ctx, "id")));
        Register<UserController>("DELETE", "users/{id}",           (c, ctx) => c.Delete(ctx, ParseGuid(ctx, "id")));

        // ── Hosts ─────────────────────────────────────────────────────────────
        Register<HostController>("GET",    "hosts",      (c, ctx) => c.GetList(ctx));
        Register<HostController>("POST",   "hosts",      (c, ctx) => c.Create(ctx));
        Register<HostController>("GET",    "hosts/{id}", (c, ctx) => c.GetById(ctx, ParseInt(ctx, "id")));
        Register<HostController>("PUT",    "hosts/{id}", (c, ctx) => c.Update(ctx, ParseInt(ctx, "id")));
        Register<HostController>("DELETE", "hosts/{id}", (c, ctx) => c.Delete(ctx, ParseInt(ctx, "id")));

        // ── Lookups ───────────────────────────────────────────────────────────
        Register<LookupController>("GET", "lookups/permissions", (c, ctx) => c.GetPermissions(ctx));
    }

    // ── 輔助方法 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 泛型路由註冊：將 Controller 類型與 handler 綁定，
    /// 實際的 Controller 實例在請求時才從 IServiceProvider 解析。
    /// </summary>
    private void Register<TController>(
        string method,
        string pattern,
        Func<TController, RouteContext, Task<IActionResult>> handler)
        where TController : notnull
    {
        _routes.Add(new RouteDefinition(
            method.ToUpperInvariant(),
            pattern,
            (sp, ctx) =>
            {
                // ⚠ 這裡從 IServiceProvider 延遲取得 Controller，
                //   確保每次請求使用的是該 scope 內的實例。
                var controller = (TController)sp.GetRequiredService(typeof(TController));
                return handler(controller, ctx);
            }));
    }

    /// <summary>
    /// 從路由路徑參數解析 Guid。
    /// 若解析失敗則回傳 Guid.Empty，Controller 層判斷後回傳 400/404 即可。
    /// </summary>
    private static Guid ParseGuid(RouteContext ctx, string paramName)
    {
        if (ctx.PathParams.TryGetValue(paramName, out var value) &&
            Guid.TryParse(value, out var guid))
            return guid;

        return Guid.Empty;
    }

    /// <summary>
    /// 從路由路徑參數解析 int（用於 identity 型主鍵，如 hostid）。
    /// 若解析失敗則回傳 0，Controller 層判斷後回傳 400/404 即可。
    /// </summary>
    private static int ParseInt(RouteContext ctx, string paramName)
    {
        if (ctx.PathParams.TryGetValue(paramName, out var value) &&
            int.TryParse(value, out var id))
            return id;

        return 0;
    }
}
