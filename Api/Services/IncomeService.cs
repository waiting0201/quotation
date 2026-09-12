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
/// - CreateAsync:   EF Core 新增，自動產生 INC{yyyyMMdd}{NNN} 編碼；核銷發票後在同一交易內
///                  以 ItemSettlementService 回寫報價單的 income/status
/// - DeleteAsync:   回傳 (Found, Error)；先解除關聯發票核銷並還原狀態，再刪除，
///                  同樣在同一交易內回寫報價單的 income/status
/// </summary>
public class IncomeService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection      _dapper;
    private readonly ItemSettlementService _itemSettlement;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    // 發票狀態：已開(0) → 已寄出(1) → 已入帳(2) → 作廢(3)
    private const short InvoiceStatusSent     = 1;
    private const short InvoiceStatusReceived = 2;
    private const short InvoiceStatusVoid     = 3;

    public IncomeService(QuotationDbContext db, IDbConnection dapper, ItemSettlementService itemSettlement)
    {
        _db             = db;
        _dapper         = dapper;
        _itemSettlement = itemSettlement;
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
    /// 若 dto.InvoiceIds 有值，會在同一交易內將這些發票的 incomeid 指向新入帳（核銷），
    /// 並將其狀態改為「已入帳」(2)；
    /// 僅核銷屬於同一客戶、尚未關聯其他入帳且非作廢(3)的發票，其餘忽略。
    /// </summary>
    public async Task<IncomeListDto> CreateAsync(IncomeCreateDto dto, Guid userId)
    {
        // 本操作橫跨兩個聚合根（invoices 核銷 + items 的 income/status 回寫），
        // 必須用 explicit transaction 包起來確保原子性：
        // ItemSettlementService.RecalculateAsync 是用 EF 查詢資料庫現況來重算，
        // 看不到尚未 SaveChangesAsync 的記憶體變更，所以要先寫入發票核銷、
        // 再重算報價單、再寫入一次，最後一起 Commit；任何一步失敗都要整批回滾，
        // 避免出現「發票已核銷但報價單狀態沒同步」的中間態。
        // 交易從方法最開頭就開啟，讓編碼流水號查詢與後續的報價單鎖定都納入同一交易。
        await using var transaction = await _db.Database.BeginTransactionAsync();

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

        // 核銷選取的發票：將其 incomeid 指向本次新入帳，並標記為「已入帳」。
        // 僅處理屬於同一客戶、尚未被其他入帳佔用（incomeid IS NULL）且非作廢的發票，
        // 避免跨客戶、重複核銷或讓作廢單復活（與 GetSelectableInvoicesAsync 的條件一致）。
        var affectedItemIds = new List<Guid>();

        if (dto.InvoiceIds.Count > 0)
        {
            var ids = dto.InvoiceIds.Distinct().ToList();
            var invoices = await _db.Invoices
                .Where(inv => ids.Contains(inv.Invoiceid)
                           && inv.Customerid == dto.CustomerId
                           && inv.Incomeid == null
                           && inv.Status != InvoiceStatusVoid)
                .ToListAsync();

            if (invoices.Count > 0)
            {
                var invoiceIds = invoices.Select(inv => inv.Invoiceid).ToList();
                affectedItemIds = await _db.Invoicedetails
                    .Where(d => d.Invoiceid != null && invoiceIds.Contains(d.Invoiceid.Value) && d.Itemid != null)
                    .Select(d => d.Itemid!.Value)
                    .Distinct()
                    .ToListAsync();

                // 動到發票之前先鎖住受影響的報價單，序列化同一張報價單的併發入帳
                await _itemSettlement.LockItemsAsync(affectedItemIds);
            }

            foreach (var inv in invoices)
            {
                inv.Incomeid = income.Incomeid;
                inv.Status   = InvoiceStatusReceived;
            }
        }

        await _db.SaveChangesAsync();

        if (affectedItemIds.Count > 0)
        {
            await _itemSettlement.RecalculateAsync(affectedItemIds);
            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        // 重新以 Dapper 查詢，確保回傳的 CustomerName 已 JOIN
        return (await GetSingleAsync(income.Incomeid))!;
    }

    /// <summary>
    /// 刪除收款記錄。
    /// 刪除前先解除所有關聯發票的核銷（將 invoices.incomeid 設回 NULL），
    /// 並把因核銷而被標為「已入帳」(2) 的發票退回「已寄出」(1)，
    /// 讓這些發票回到「未入帳」可再次核銷的狀態，再刪除收款本身。
    /// </summary>
    /// <returns>(Found: false) 找不到記錄</returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(Guid id)
    {
        var income = await _db.Incomes.FirstOrDefaultAsync(i => i.Incomeid == id);

        if (income == null)
            return (Found: false, Error: null);

        // 同 CreateAsync：解除核銷（invoices）與報價單 income/status 回寫需在同一交易內，
        // 確保刪除入帳後兩邊資料一致，不會半套。
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // 解除核銷：關聯發票的 incomeid 設回 NULL，回到可選池
        var linkedInvoices = await _db.Invoices.Where(inv => inv.Incomeid == id).ToListAsync();

        List<Guid> affectedItemIds = new();
        if (linkedInvoices.Count > 0)
        {
            var invoiceIds = linkedInvoices.Select(inv => inv.Invoiceid).ToList();
            affectedItemIds = await _db.Invoicedetails
                .Where(d => d.Invoiceid != null && invoiceIds.Contains(d.Invoiceid.Value) && d.Itemid != null)
                .Select(d => d.Itemid!.Value)
                .Distinct()
                .ToListAsync();

            // 動到發票之前先鎖住受影響的報價單（理由同 CreateAsync）
            await _itemSettlement.LockItemsAsync(affectedItemIds);
        }

        foreach (var inv in linkedInvoices)
        {
            inv.Incomeid = null;

            // 只還原核銷時自動標記的「已入帳」，使用者手動設定的其他狀態不動
            if (inv.Status == InvoiceStatusReceived)
                inv.Status = InvoiceStatusSent;
        }

        _db.Incomes.Remove(income);

        await _db.SaveChangesAsync();

        if (affectedItemIds.Count > 0)
        {
            await _itemSettlement.RecalculateAsync(affectedItemIds);
            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

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

        // 取今日最大流水號 + 1。不可改用筆數，當日若有刪除會使計數倒退而重複發號。
        // 流水號補零至 3 位，字典序等同數值序，故直接取編碼最大值即可。
        var lastCode = await _db.Incomes
            .Where(i => i.Incomecode != null && i.Incomecode.StartsWith(prefix))
            .OrderByDescending(i => i.Incomecode)
            .Select(i => i.Incomecode)
            .FirstOrDefaultAsync();

        var seq    = lastCode != null && int.TryParse(lastCode[prefix.Length..], out var lastSeq)
            ? lastSeq + 1
            : 1;

        return $"{prefix}{seq:D3}";
    }
}
