using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;
using QuotationApi.Controllers;
using QuotationApi.Helpers;
using QuotationApi.Middleware;
using QuotationApi.Models;
using QuotationApi.Router;
using QuotationApi.Services;

// QuestPDF Community License（開源專案免費使用）
QuestPDF.Settings.License = LicenseType.Community;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// ── Application Insights ──────────────────────────────────────────────────
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// ── Database ──────────────────────────────────────────────────────────────

var connectionString =
    builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["Values:ConnectionStrings:DefaultConnection"]
    ?? "Server=(local);Database=quotation;User Id=sa;Password=twvsjp0205;TrustServerCertificate=true";

builder.Services.AddDbContext<QuotationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // 網路不穩時自動重試（Azure SQL 瞬斷保護）
        // ⚠ maxRetryDelay 設 5 秒即可，30 秒對本機開發太久且會讓首次連線逾時感覺更嚴重
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

// Dapper：以 transient 方式注入 IDbConnection
// 每次取用時建立新連線，由呼叫方負責 using/dispose
builder.Services.AddTransient<IDbConnection>(_ =>
    new SqlConnection(connectionString));

// ── Helpers ───────────────────────────────────────────────────────────────

builder.Services.AddSingleton<JwtHelper>();

// ── Services ──────────────────────────────────────────────────────────────
// Services 依賴 DbContext（Scoped），所以必須也是 Scoped

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CountryService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CustomerTypeService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<HostService>();
builder.Services.AddScoped<IncomeService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<UserService>();

// ── Controllers ───────────────────────────────────────────────────────────
// Controllers 依賴 Service（Scoped），所以必須也是 Scoped

builder.Services.AddScoped<AuthController>();
builder.Services.AddScoped<CountryController>();
builder.Services.AddScoped<CustomerController>();
builder.Services.AddScoped<CustomerTypeController>();
builder.Services.AddScoped<DashboardController>();
builder.Services.AddScoped<GroupController>();
builder.Services.AddScoped<HostController>();
builder.Services.AddScoped<IncomeController>();
builder.Services.AddScoped<InvoiceController>();
builder.Services.AddScoped<LookupController>();
builder.Services.AddScoped<PaymentController>();
builder.Services.AddScoped<UserController>();

// ── Router ────────────────────────────────────────────────────────────────
//
// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  ⚠ DI 生命週期設計原則                                                 ║
// ║                                                                        ║
// ║  RouteTable + RouteHandler + MiddlewarePipeline 必須是 Singleton：      ║
// ║  - 路由表和正則表達式只需建構/編譯一次                                    ║
// ║  - Middleware 管線在應用程式生命週期中不會改變                             ║
// ║  - 避免每次 HTTP 請求都重建路由表、重新編譯 Regex、重新解析所有 Controller  ║
// ║                                                                        ║
// ║  Controller 實例透過 RouteTable 的 HandlerFactory 延遲解析，             ║
// ║  使用 HttpContext.RequestServices（scoped provider），                   ║
// ║  確保每次請求取得的是正確 scope 的 DbContext 等服務。                      ║
// ║                                                                        ║
// ║  ❌ 常見錯誤：把 RouteTable 改為 Scoped（因為它「用到」Controller）        ║
// ║     → 這會導致每次請求解析所有 Controller + Service，嚴重影響效能         ║
// ║  ✅ 正確做法：RouteTable 存 HandlerFactory，請求時才解析需要的 Controller  ║
// ╚══════════════════════════════════════════════════════════════════════════╝

builder.Services.AddSingleton<RouteTable>();
builder.Services.AddSingleton<RouteHandler>();

// ── Middleware ────────────────────────────────────────────────────────────
//
// 所有 middleware 必須是 thread-safe（無狀態或僅依賴 Singleton 服務）。
// 若未來新增的 middleware 需要 Scoped 依賴（如 DbContext），
// 應在 InvokeAsync 中透過 context.Request.HttpContext.RequestServices 取得，
// 而非透過建構子注入。

builder.Services.AddSingleton<CorsMiddleware>();
builder.Services.AddSingleton<ErrorHandlingMiddleware>();
builder.Services.AddSingleton<JwtAuthMiddleware>();

builder.Services.AddSingleton<MiddlewarePipeline>(sp =>
{
    var pipeline = new MiddlewarePipeline();
    pipeline
        .Use(sp.GetRequiredService<CorsMiddleware>())
        .Use(sp.GetRequiredService<ErrorHandlingMiddleware>())
        .Use(sp.GetRequiredService<JwtAuthMiddleware>());
    return pipeline;
});

// ── System.Text.Json camelCase ────────────────────────────────────────────

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
    options.SerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Build().Run();
