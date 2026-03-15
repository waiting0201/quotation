using QuotationApi.Controllers;

namespace QuotationApi.Router;

/// <summary>
/// 路由定義項目
/// </summary>
public record RouteDefinition(
    string Method,
    string Pattern,
    Func<RouteContext, Task<Microsoft.AspNetCore.Mvc.IActionResult>> Handler);

/// <summary>
/// 集中管理所有路由規則；
/// 新增 controller 時在 RegisterRoutes() 中登記即可。
/// </summary>
public class RouteTable
{
    private readonly List<RouteDefinition> _routes = new();

    public IReadOnlyList<RouteDefinition> Routes => _routes;

    public RouteTable(
        AuthController authController,
        DashboardController dashboardController,
        GroupController groupController,
        LookupController lookupController,
        UserController userController)
    {
        RegisterRoutes(authController, dashboardController, groupController, lookupController, userController);
    }

    private void RegisterRoutes(
        AuthController authController,
        DashboardController dashboardController,
        GroupController groupController,
        LookupController lookupController,
        UserController userController)
    {
        // ── Auth ────────────────────────────────────────────────────────────
        Register("POST", "auth/login", authController.Login);
        Register("GET",  "auth/me",    authController.Me);

        // ── Dashboard ───────────────────────────────────────────────────────
        Register("GET", "dashboard", dashboardController.GetDashboard);

        // ── Groups ──────────────────────────────────────────────────────────
        Register("GET",    "groups",      groupController.GetList);
        Register("POST",   "groups",      groupController.Create);
        Register("GET",    "groups/{id}", ctx => groupController.GetById(ctx, ParseGuid(ctx, "id")));
        Register("PUT",    "groups/{id}", ctx => groupController.Update(ctx, ParseGuid(ctx, "id")));
        Register("DELETE", "groups/{id}", ctx => groupController.Delete(ctx, ParseGuid(ctx, "id")));

        // ── Users ─────────────────────────────────────────────────────────────
        Register("GET",    "users",                ctx => userController.GetList(ctx));
        Register("POST",   "users",                ctx => userController.Create(ctx));
        Register("GET",    "users/{id}",           ctx => userController.GetById(ctx, ParseGuid(ctx, "id")));
        Register("PUT",    "users/{id}",           ctx => userController.Update(ctx, ParseGuid(ctx, "id")));
        Register("PUT",    "users/{id}/password",  ctx => userController.ChangePassword(ctx, ParseGuid(ctx, "id")));
        Register("DELETE", "users/{id}",           ctx => userController.Delete(ctx, ParseGuid(ctx, "id")));

        // ── Lookups ───────────────────────────────────────────────────────────
        Register("GET", "lookups/permissions", lookupController.GetPermissions);
    }

    // ── 輔助方法 ────────────────────────────────────────────────────────────

    private void Register(
        string method,
        string pattern,
        Func<RouteContext, Task<Microsoft.AspNetCore.Mvc.IActionResult>> handler)
    {
        _routes.Add(new RouteDefinition(method.ToUpperInvariant(), pattern, handler));
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
}
