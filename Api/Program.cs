using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuotationApi.Controllers;
using QuotationApi.Helpers;
using QuotationApi.Middleware;
using QuotationApi.Models;
using QuotationApi.Router;
using QuotationApi.Services;

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
        // 網路不穩時自動重試，最多 5 次，每次間隔 30 秒
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Dapper：以 transient 方式注入 IDbConnection
// 每次取用時建立新連線，由呼叫方負責 using/dispose
builder.Services.AddTransient<IDbConnection>(_ =>
    new SqlConnection(connectionString));

// ── Helpers ───────────────────────────────────────────────────────────────

builder.Services.AddSingleton<JwtHelper>();

// ── Services ──────────────────────────────────────────────────────────────

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<UserService>();

// ── Controllers ───────────────────────────────────────────────────────────

builder.Services.AddScoped<AuthController>();
builder.Services.AddScoped<DashboardController>();
builder.Services.AddScoped<GroupController>();
builder.Services.AddScoped<LookupController>();
builder.Services.AddScoped<UserController>();

// ── Router ────────────────────────────────────────────────────────────────

// RouteTable 依賴 controllers，需要 scoped 或讓 DI 解析
// 這裡以 scoped 處理，讓每個請求都取得新的 RouteTable 實例
builder.Services.AddScoped<RouteTable>();
builder.Services.AddScoped<RouteHandler>();

// ── Middleware ────────────────────────────────────────────────────────────

// 各 middleware 以 scoped 注入（跟隨請求生命週期）
builder.Services.AddScoped<CorsMiddleware>();
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddScoped<JwtAuthMiddleware>();

// MiddlewarePipeline 本身也是 scoped，每個請求組裝一次管線
builder.Services.AddScoped<MiddlewarePipeline>(sp =>
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
