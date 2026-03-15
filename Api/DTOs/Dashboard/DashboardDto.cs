using System.Text.Json.Serialization;

namespace QuotationApi.DTOs.Dashboard;

public class DashboardDto
{
    [JsonPropertyName("stats")]
    public DashboardStatsDto Stats { get; set; } = new();

    [JsonPropertyName("recentQuotations")]
    public List<RecentQuotationDto> RecentQuotations { get; set; } = [];

    [JsonPropertyName("monthlyTrend")]
    public List<MonthlyTrendDto> MonthlyTrend { get; set; } = [];

    [JsonPropertyName("invoiceStatusCounts")]
    public List<StatusCountDto> InvoiceStatusCounts { get; set; } = [];

    [JsonPropertyName("calendarEvents")]
    public List<CalendarEventDto> CalendarEvents { get; set; } = [];
}

public class DashboardStatsDto
{
    [JsonPropertyName("activeQuotations")]
    public int ActiveQuotations { get; set; }

    [JsonPropertyName("quotedCount")]
    public int QuotedCount { get; set; }

    [JsonPropertyName("signedCount")]
    public int SignedCount { get; set; }

    [JsonPropertyName("pendingInvoices")]
    public int PendingInvoices { get; set; }

    [JsonPropertyName("issuedCount")]
    public int IssuedCount { get; set; }

    [JsonPropertyName("sentCount")]
    public int SentCount { get; set; }

    [JsonPropertyName("totalCustomers")]
    public int TotalCustomers { get; set; }

    [JsonPropertyName("newCustomersThisMonth")]
    public int NewCustomersThisMonth { get; set; }

    [JsonPropertyName("totalIncome")]
    public long TotalIncome { get; set; }

    [JsonPropertyName("totalIncomeRecords")]
    public int TotalIncomeRecords { get; set; }
}

public class RecentQuotationDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("customer")]
    public string Customer { get; set; } = "";

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";
}

public class MonthlyTrendDto
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }
}

public class StatusCountDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class CalendarEventDto
{
    [JsonPropertyName("customer")]
    public string Customer { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = "";

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = "";

    [JsonPropertyName("status")]
    public int Status { get; set; }
}
