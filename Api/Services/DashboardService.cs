using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Dashboard;
using QuotationApi.Models;

namespace QuotationApi.Services;

public class DashboardService
{
    private readonly QuotationDbContext _db;

    public DashboardService(QuotationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));

        var monthStart = new DateTime(now.Year, now.Month, 1);

        // ── Stats ────────────────────────────────────────────
        var quotedCount = await _db.Items.CountAsync(i => i.Status == 0);
        var signedCount = await _db.Items.CountAsync(i => i.Status == 1);

        var issuedCount = await _db.Invoices.CountAsync(i => i.Status == 0);
        var sentCount = await _db.Invoices.CountAsync(i => i.Status == 1);

        var totalCustomers = await _db.Customers.CountAsync();
        var newCustomersThisMonth = await _db.Items
            .Where(i => i.Createdate >= monthStart)
            .Select(i => i.Customerid)
            .Distinct()
            .CountAsync();

        var totalIncome = await _db.Incomes.SumAsync(i => (long)(i.Amount ?? 0));
        var totalIncomeRecords = await _db.Incomes.CountAsync();

        // ── Recent Quotations ────────────────────────────────
        var recentQuotations = await _db.Items
            .AsNoTracking()
            .Include(i => i.Customer)
            .OrderByDescending(i => i.Createdate)
            .Take(5)
            .Select(i => new RecentQuotationDto
            {
                Code = i.Itemcode ?? "",
                Customer = i.Customer != null ? i.Customer.Name ?? "" : "",
                Amount = i.Total ?? 0,
                Status = i.Status ?? 0,
                Date = i.Createdate != null
                    ? i.Createdate.Value.ToString("MM/dd")
                    : ""
            })
            .ToListAsync();

        // ── Monthly Trend (last 7 months) ────────────────────
        var sevenMonthsAgo = monthStart.AddMonths(-6);
        var monthlyRaw = await _db.Items
            .AsNoTracking()
            .Where(i => i.Createdate >= sevenMonthsAgo)
            .GroupBy(i => new { i.Createdate!.Value.Year, i.Createdate!.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Amount = g.Sum(x => (long)(x.Total ?? 0))
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        var monthlyTrend = monthlyRaw.Select(m => new MonthlyTrendDto
        {
            Label = $"{m.Month}月",
            Count = m.Count,
            Amount = m.Amount
        }).ToList();

        // ── Invoice Status Counts ────────────────────────────
        var invoiceGroups = await _db.Invoices
            .AsNoTracking()
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key ?? 0, Count = g.Count() })
            .ToListAsync();

        string[] invoiceLabels = ["已開", "已寄出", "已入帳", "作廢"];
        var invoiceStatusCounts = invoiceGroups
            .Select(g => new StatusCountDto
            {
                Status = g.Status,
                Label = g.Status >= 0 && g.Status < invoiceLabels.Length
                    ? invoiceLabels[g.Status]
                    : $"狀態{g.Status}",
                Count = g.Count
            })
            .OrderBy(s => s.Status)
            .ToList();

        // ── Calendar Events (signed/closed quotations with dates) ──
        var calendarEvents = await _db.Items
            .AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => (i.Status == 1 || i.Status == 2)
                && i.Signdate != null && i.Deadline != null)
            .OrderByDescending(i => i.Signdate)
            .Take(20)
            .Select(i => new CalendarEventDto
            {
                Customer = i.Customer != null ? i.Customer.Name ?? "" : "",
                Name = i.Name ?? "",
                StartDate = i.Signdate!.Value.ToString("yyyy-MM-dd"),
                EndDate = i.Deadline!.Value.ToString("yyyy-MM-dd"),
                Status = i.Status ?? 0
            })
            .ToListAsync();

        return new DashboardDto
        {
            Stats = new DashboardStatsDto
            {
                ActiveQuotations = quotedCount + signedCount,
                QuotedCount = quotedCount,
                SignedCount = signedCount,
                PendingInvoices = issuedCount + sentCount,
                IssuedCount = issuedCount,
                SentCount = sentCount,
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                TotalIncome = totalIncome,
                TotalIncomeRecords = totalIncomeRecords
            },
            RecentQuotations = recentQuotations,
            MonthlyTrend = monthlyTrend,
            InvoiceStatusCounts = invoiceStatusCounts,
            CalendarEvents = calendarEvents
        };
    }
}
