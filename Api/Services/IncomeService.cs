using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Income;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 收款管理服務
/// - GetListAsync:  Dapper 查詢，JOIN customers，支援 incomecode/name 關鍵字搜尋
/// - CreateAsync:   EF Core 新增，自動產生 INC{yyyyMMdd}{NNN} 編碼
/// - DeleteAsync:   回傳 (Found, Error)；有關聯發票時拒絕刪除
/// </summary>
public class IncomeService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection      _dapper;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public IncomeService(QuotationDbContext db, IDbConnection dapper)
    {
        _db     = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得收款清單（分頁），依 createdate DESC 排序。
    /// 可選填 search 關鍵字，對 incomecode 與 customers.name 欄位進行 LIKE 搜尋。
    /// HasInvoices 表示該收款有無關聯發票，用於前端判斷是否顯示刪除按鈕。
    /// </summary>
    public async Task<PaginatedResponse<IncomeListDto>> GetListAsync(
        int page, int pageSize, string? search)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var whereClause = hasSearch
            ? "WHERE inc.incomecode LIKE @Search OR c.name LIKE @Search"
            : string.Empty;

        object param = hasSearch
            ? new { Search = $"%{search!.Trim()}%", Offset = (page - 1) * pageSize, PageSize = pageSize }
            : new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        // 先計算符合條件的總筆數
        var countSql = $"""
            SELECT COUNT(*)
            FROM incomes inc
            LEFT JOIN customers c ON c.customerid = inc.customerid
            {whereClause}
            """;
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql, param);

        var dataSql = $"""
            SELECT
                inc.incomeid    AS IncomeId,
                inc.incomecode  AS IncomeCode,
                inc.customerid  AS CustomerId,
                c.name          AS CustomerName,
                inc.amount      AS Amount,
                inc.fee         AS Fee,
                inc.incomedate  AS IncomeDate,
                inc.remark      AS Remark,
                inc.createdate  AS CreateDate,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM invoices inv WHERE inv.incomeid = inc.incomeid
                ) THEN 1 ELSE 0 END AS BIT) AS HasInvoices
            FROM incomes inc
            LEFT JOIN customers c ON c.customerid = inc.customerid
            {whereClause}
            ORDER BY inc.incomedate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<IncomeListDto>(dataSql, param);
        return PaginatedResponse<IncomeListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得指定客戶可供入帳核銷的發票清單。
    /// 僅回傳 incomeid IS NULL（尚未關聯任何入帳）且非作廢（status &lt;&gt; 3）的發票，
    /// 依 createdate DESC 排序，供「新增入帳」勾選使用。
    /// </summary>
    public async Task<List<IncomeInvoiceOptionDto>> GetSelectableInvoicesAsync(int customerId)
    {
        const string sql = """
            SELECT
                i.invoiceid     AS InvoiceId,
                i.invoicecode   AS InvoiceCode,
                i.requestdate   AS RequestDate,
                i.tax           AS Tax,
                i.total         AS Total,
                i.status        AS Status,
                i.createdate    AS CreateDate
            FROM invoices i
            WHERE i.customerid = @CustomerId
              AND i.incomeid IS NULL
              AND i.status <> 3
            ORDER BY i.createdate DESC
            """;

        var results = await _dapper.QueryAsync<IncomeInvoiceOptionDto>(sql, new { CustomerId = customerId });
        return results.AsList();
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增收款記錄。
    /// 自動產生 INC{yyyyMMdd}{NNN} 編碼（依台北時區當日流水號遞增）。
    /// 若 dto.InvoiceIds 有值，會在同一交易內將這些發票的 incomeid 指向新入帳（核銷）；
    /// 僅核銷屬於同一客戶且尚未關聯其他入帳的發票，其餘忽略。
    /// </summary>
    public async Task<IncomeListDto> CreateAsync(IncomeCreateDto dto, Guid userId)
    {
        var incomeCode  = await GenerateCodeAsync();
        var taipeiNow   = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        var income = new Income
        {
            Incomeid   = Guid.NewGuid(),
            Incomecode = incomeCode,
            Customerid = dto.CustomerId,
            Amount     = dto.Amount ?? 0,
            Fee        = dto.Fee    ?? 0,
            Incomedate = dto.IncomeDate,
            Remark     = dto.Remark?.Trim(),
            Createdate = taipeiNow,
            Userid     = userId
        };

        _db.Incomes.Add(income);

        // 核銷選取的發票：將其 incomeid 指向本次新入帳。
        // 僅處理屬於同一客戶且尚未被其他入帳佔用（incomeid IS NULL）的發票，避免跨客戶或重複核銷。
        if (dto.InvoiceIds.Count > 0)
        {
            var ids = dto.InvoiceIds.Distinct().ToList();
            var invoices = await _db.Invoices
                .Where(inv => ids.Contains(inv.Invoiceid)
                           && inv.Customerid == dto.CustomerId
                           && inv.Incomeid == null)
                .ToListAsync();

            foreach (var inv in invoices)
                inv.Incomeid = income.Incomeid;
        }

        await _db.SaveChangesAsync();

        // 重新以 Dapper 查詢，確保回傳的 CustomerName 已 JOIN
        return (await GetSingleAsync(income.Incomeid))!;
    }

    /// <summary>
    /// 刪除收款記錄。
    /// 刪除前先解除所有關聯發票的核銷（將 invoices.incomeid 設回 NULL），
    /// 讓這些發票回到「未入帳」可再次核銷的狀態，再刪除收款本身。
    /// </summary>
    /// <returns>(Found: false) 找不到記錄</returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(Guid id)
    {
        var income = await _db.Incomes.FirstOrDefaultAsync(i => i.Incomeid == id);

        if (income == null)
            return (Found: false, Error: null);

        // 解除核銷：關聯發票的 incomeid 設回 NULL，回到可選池
        var linkedInvoices = await _db.Invoices.Where(inv => inv.Incomeid == id).ToListAsync();
        foreach (var inv in linkedInvoices)
            inv.Incomeid = null;

        _db.Incomes.Remove(income);
        await _db.SaveChangesAsync();

        return (Found: true, Error: null);
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 以 Dapper 查詢單筆收款（含 CustomerName JOIN）。
    /// 用於 Create 後回傳完整資料給前端。
    /// </summary>
    private async Task<IncomeListDto?> GetSingleAsync(Guid id)
    {
        const string sql = """
            SELECT
                inc.incomeid    AS IncomeId,
                inc.incomecode  AS IncomeCode,
                inc.customerid  AS CustomerId,
                c.name          AS CustomerName,
                inc.amount      AS Amount,
                inc.fee         AS Fee,
                inc.incomedate  AS IncomeDate,
                inc.remark      AS Remark,
                inc.createdate  AS CreateDate,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM invoices inv WHERE inv.incomeid = inc.incomeid
                ) THEN 1 ELSE 0 END AS BIT) AS HasInvoices
            FROM incomes inc
            LEFT JOIN customers c ON c.customerid = inc.customerid
            WHERE inc.incomeid = @Id
            """;

        return await _dapper.QueryFirstOrDefaultAsync<IncomeListDto>(sql, new { Id = id });
    }

    /// <summary>
    /// 產生 INC{yyyyMMdd}{NNN} 格式的收款編碼。
    /// 每日流水號從 001 開始，依當天已存在的 INC{yyyyMMdd}* 數量遞增。
    /// 使用台北時區確保跨午夜時編碼日期正確。
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        var today   = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);
        var dateStr = today.ToString("yyyyMMdd");
        var prefix  = $"INC{dateStr}";

        // 計算今日已存在的編碼數量作為流水號基數
        var count = await _db.Incomes
            .CountAsync(i => i.Incomecode != null && i.Incomecode.StartsWith(prefix));

        var seq = (count + 1).ToString("D3");
        return $"{prefix}{seq}";
    }
}
