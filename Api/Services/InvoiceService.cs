using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Invoice;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 發票管理服務
/// - GetListAsync:             Dapper 查詢，JOIN customers，支援 invoicecode/name 關鍵字搜尋
/// - GetByIdAsync:             Dapper 查詢完整詳情（JOIN customers），另查明細（JOIN items）
/// - GetCustomerQuotationsAsync: 查詢客戶的報價單列表（供發票明細下拉選擇）
/// - CreateAsync:              EF Core 新增，自動產生 INV{yyyyMMdd}{NNN} 編碼，計算稅額；
///                             新建發票 incomeid 必為 null，不影響報價單 income/status，故略過重算
/// - UpdateAsync:              EF Core 更新標頭、刪舊明細後重新插入、重新計算稅額；
///                             若發票已核銷，明細變動會影響報價單 income，需在同一交易內重算
/// - DeleteAsync:              回傳 (Found, Error)；已關聯收款時拒絕刪除；
///                             刪除未核銷發票不影響 income，但仍統一重算以確保一致
/// </summary>
public class InvoiceService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;
    private readonly ItemSettlementService _itemSettlement;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public InvoiceService(QuotationDbContext db, IDbConnection dapper, ItemSettlementService itemSettlement)
    {
        _db = db;
        _dapper = dapper;
        _itemSettlement = itemSettlement;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得發票清單（分頁），依 createdate DESC 排序。
    /// 可選填 search 關鍵字，對 invoicecode 與 customers.name 欄位進行 LIKE 搜尋。
    /// HasIncomes 用於前端判斷是否顯示刪除按鈕。
    /// </summary>
    public async Task<PaginatedResponse<InvoiceListDto>> GetListAsync(int page, int pageSize, string? search)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var whereClause = hasSearch
            ? "WHERE i.invoicecode LIKE @Search OR c.name LIKE @Search"
            : string.Empty;

        object param = hasSearch
            ? new { Search = $"%{search!.Trim()}%", Offset = (page - 1) * pageSize, PageSize = pageSize }
            : new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        // 先計算符合條件的總筆數
        var countSql = $"""
            SELECT COUNT(*)
            FROM invoices i
            LEFT JOIN customers c ON c.customerid = i.customerid
            {whereClause}
            """;
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql, param);

        var dataSql = $"""
            SELECT
                i.invoiceid     AS InvoiceId,
                i.invoicecode   AS InvoiceCode,
                i.customerid    AS CustomerId,
                c.name          AS CustomerName,
                i.requestdate   AS RequestDate,
                i.tax           AS Tax,
                i.total         AS Total,
                i.status        AS Status,
                i.createdate    AS CreateDate,
                CAST(CASE WHEN i.incomeid IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasIncomes
            FROM invoices i
            LEFT JOIN customers c ON c.customerid = i.customerid
            {whereClause}
            ORDER BY i.createdate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<InvoiceListDto>(dataSql, param);
        return PaginatedResponse<InvoiceListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得單一發票完整詳情，含明細列表（JOIN items 取得報價單資訊）。
    /// 使用 Dapper 完成查詢；明細依 freq 排序。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<InvoiceDetailResponseDto?> GetByIdAsync(Guid id)
    {
        const string invoiceSql = """
            SELECT
                i.invoiceid     AS InvoiceId,
                i.invoicecode   AS InvoiceCode,
                i.customerid    AS CustomerId,
                c.name          AS CustomerName,
                i.requestdate   AS RequestDate,
                i.remark        AS Remark,
                i.tax           AS Tax,
                i.total         AS Total,
                i.status        AS Status,
                i.createdate    AS CreateDate
            FROM invoices i
            LEFT JOIN customers c ON c.customerid = i.customerid
            WHERE i.invoiceid = @Id
            """;

        var invoice = await _dapper.QueryFirstOrDefaultAsync<InvoiceDetailResponseDto>(
            invoiceSql, new { Id = id });

        if (invoice == null)
            return null;

        // 明細依 freq（排序序號）升冪排列；JOIN items 取得報價單相關資訊
        const string detailSql = """
            SELECT
                d.invoicedetailid   AS InvoiceDetailId,
                d.itemid            AS ItemId,
                it.itemcode         AS ItemCode,
                it.name             AS ItemName,
                it.taxtype          AS ItemTaxType,
                d.invoicetype       AS InvoiceType,
                d.invoicedate       AS InvoiceDate,
                d.invoicenumber     AS InvoiceNumber,
                d.price             AS Price,
                d.tax               AS Tax,
                d.remark            AS Remark,
                d.freq              AS Freq
            FROM invoicedetails d
            LEFT JOIN items it ON it.itemid = d.itemid
            WHERE d.invoiceid = @Id
            ORDER BY d.freq ASC, d.invoicedetailid ASC
            """;

        var details = await _dapper.QueryAsync<InvoiceDetailItemDto>(detailSql, new { Id = id });
        invoice.Details = details.AsList();

        return invoice;
    }

    /// <summary>
    /// 取得指定客戶的報價單列表，供建立發票明細時選擇。
    /// 回傳所有狀態的報價單（已報價/已簽約/已結案），讓使用者自行判斷選擇。
    /// 依 createdate DESC 排序。
    /// </summary>
    public async Task<List<QuotationLookupDto>> GetCustomerQuotationsAsync(int customerId)
    {
        const string sql = """
            SELECT
                itemid      AS ItemId,
                itemcode    AS ItemCode,
                name        AS Name,
                taxtype     AS TaxType,
                total       AS Total
            FROM items
            WHERE customerid = @CustomerId
              AND status <> 3
            ORDER BY createdate DESC
            """;

        var results = await _dapper.QueryAsync<QuotationLookupDto>(sql, new { CustomerId = customerId });
        return results.AsList();
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增發票。
    /// 自動產生 INV{yyyyMMdd}{NNN} 編碼（依台北時區當日流水號遞增）。
    /// 依各明細關聯報價單的稅別計算稅額：
    ///   taxtype 0（稅外加）/ 1（稅內含）：tax = round(price * 0.05)
    ///   taxtype 2（免稅）              ：tax = 0
    /// 彙總所有明細的稅額與金額後寫入發票標頭。
    /// </summary>
    public async Task<InvoiceDetailResponseDto> CreateAsync(InvoiceCreateUpdateDto dto, Guid userId)
    {
        // 標頭與明細分兩次 SaveChanges，加上後續的報價單回寫，需視為單一原子操作。
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var invoiceCode = await GenerateCodeAsync();
        var taipeiNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        var invoice = new Invoice
        {
            Invoiceid   = Guid.NewGuid(),
            Invoicecode = invoiceCode,
            Customerid  = dto.CustomerId,
            Requestdate = dto.RequestDate,
            Remark      = dto.Remark?.Trim(),
            Status      = dto.Status ?? 0,
            Createdate  = taipeiNow,
            Userid      = userId
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // 插入明細並計算稅額
        if (dto.Details.Count > 0)
        {
            var (totalTax, totalAmount) = await AddDetailsAsync(invoice.Invoiceid, dto.Details);
            invoice.Tax   = totalTax;
            invoice.Total = totalAmount;
            await _db.SaveChangesAsync();
        }
        else
        {
            invoice.Tax   = 0;
            invoice.Total = 0;
            await _db.SaveChangesAsync();
        }

        // 注意：新建發票的 incomeid 一定是 null，不影響 items.income（公式只加總已核銷明細）；
        // 而報價單結案是單向的（只 1 → 2，不反向退回），新增一張未核銷發票也不會改變狀態，
        // 所以這裡不需要呼叫 ItemSettlementService 重算。
        await transaction.CommitAsync();

        return (await GetByIdAsync(invoice.Invoiceid))!;
    }

    /// <summary>
    /// 更新發票。
    /// 更新標頭欄位後，刪除所有舊明細並重新插入（避免 diff 邏輯複雜化）。
    /// 重新計算稅額並更新發票標頭的 tax / total。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<InvoiceDetailResponseDto?> UpdateAsync(Guid id, InvoiceCreateUpdateDto dto)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Invoicedetails)
            .FirstOrDefaultAsync(i => i.Invoiceid == id);

        if (invoice == null)
            return null;

        // 明細是整批刪除重建，若此發票已核銷（incomeid 有值），
        // 明細金額/所屬報價單（itemid）的變動會影響 items.income，
        // 故收集「更新前」與「更新後」兩批 itemid，更新完成後一併重算，
        // 涵蓋「把明細從 A 報價單改連到 B 報價單」這種兩邊都要回寫的情況。
        var affectedItemIds = invoice.Invoicedetails
            .Where(d => d.Itemid != null)
            .Select(d => d.Itemid!.Value)
            .Union(dto.Details.Where(d => d.ItemId.HasValue).Select(d => d.ItemId!.Value))
            .Distinct()
            .ToList();

        // 明細整批刪除重建（含中途的 AddDetailsAsync 內部 SaveChangesAsync）
        // + 報價單 income/status 回寫，必須視為單一原子操作 —— 若其中任何一步
        // 失敗，會留下「明細已改但金額/報價單未同步」的不一致狀態，因此在最開頭
        // 就開交易，包住後續所有 SaveChangesAsync（同一交易內多次 SaveChanges
        // 會自動併入這個 explicit transaction，直到 Commit 才真正落地）。
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // 動資料之前先鎖住受影響的報價單（理由見 ItemSettlementService.LockItemsAsync）
        await _itemSettlement.LockItemsAsync(affectedItemIds);

        // 更新標頭欄位
        invoice.Customerid  = dto.CustomerId;
        invoice.Requestdate = dto.RequestDate;
        invoice.Remark      = dto.Remark?.Trim();
        invoice.Status      = dto.Status ?? invoice.Status;

        // 刪除所有舊明細，再重新插入（策略：整批取代，避免逐筆比對）
        _db.Invoicedetails.RemoveRange(invoice.Invoicedetails);
        await _db.SaveChangesAsync();

        if (dto.Details.Count > 0)
        {
            var (totalTax, totalAmount) = await AddDetailsAsync(invoice.Invoiceid, dto.Details);
            invoice.Tax   = totalTax;
            invoice.Total = totalAmount;
        }
        else
        {
            invoice.Tax   = 0;
            invoice.Total = 0;
        }

        await _db.SaveChangesAsync();

        if (affectedItemIds.Count > 0)
        {
            await _itemSettlement.RecalculateAsync(affectedItemIds);
            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除發票。
    /// 若發票已關聯收款記錄（incomeid IS NOT NULL）則拒絕刪除，回傳業務錯誤訊息。
    /// </summary>
    /// <returns>(Found: false) 找不到記錄；(Error: non-null) 業務規則拒絕</returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Invoicedetails)
            .FirstOrDefaultAsync(i => i.Invoiceid == id);

        if (invoice == null)
            return (Found: false, Error: null);

        // 刪除保護：已關聯收款的發票不可刪除
        if (invoice.Incomeid.HasValue)
            return (Found: true, Error: "此發票已關聯收款記錄，無法刪除。");

        // 收集刪除前的明細所屬報價單，刪除後重算 income/status。
        // 註：能走到這裡代表 incomeid 必為 null（上方已擋已核銷的發票），理論上
        // 不影響 income，但仍統一呼叫重算以保持與 Create/Update 一致、避免日後
        // 刪除保護規則調整時遺漏這裡。
        var affectedItemIds = invoice.Invoicedetails
            .Where(d => d.Itemid != null)
            .Select(d => d.Itemid!.Value)
            .Distinct()
            .ToList();

        // 手動刪除明細（FK 未設 cascade delete）
        _db.Invoicedetails.RemoveRange(invoice.Invoicedetails);
        _db.Invoices.Remove(invoice);

        // 刪除明細/發票 + 報價單回寫需視為單一原子操作，理由同 UpdateAsync。
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // 動資料之前先鎖住受影響的報價單（理由見 ItemSettlementService.LockItemsAsync）
        await _itemSettlement.LockItemsAsync(affectedItemIds);

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
    /// 產生 INV{yyyyMMdd}{NNN} 格式的發票編碼。
    /// 每日流水號從 001 開始，依當天已存在的 INV{yyyyMMdd}* 數量遞增。
    /// 使用台北時區確保跨午夜時編碼日期正確。
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);
        var dateStr = today.ToString("yyyyMMdd");
        var prefix = $"INV{dateStr}";

        // 取今日最大流水號 + 1。不可改用筆數，當日若有刪除會使計數倒退而重複發號。
        // 流水號補零至 3 位，字典序等同數值序，故直接取編碼最大值即可。
        var lastCode = await _db.Invoices
            .Where(i => i.Invoicecode != null && i.Invoicecode.StartsWith(prefix))
            .OrderByDescending(i => i.Invoicecode)
            .Select(i => i.Invoicecode)
            .FirstOrDefaultAsync();

        var seq = lastCode != null && int.TryParse(lastCode[prefix.Length..], out var lastSeq)
            ? lastSeq + 1
            : 1;

        return $"{prefix}{seq:D3}";
    }

    /// <summary>
    /// 將發票明細批次插入資料庫，並根據關聯報價單的稅別計算每筆明細的稅額。
    /// 回傳（稅額合計, 金額合計）供更新發票標頭使用。
    ///
    /// 稅別計算規則（依 items.taxtype）：明細輸入的 price 一律是「未稅金額」，
    /// 故 invoices.total 恆為未稅合計、invoices.tax 為稅額合計，含稅總額 = total + tax。
    ///   0（稅外加）/ 1（稅內含）：tax = round(price * 0.05)
    ///   2（免稅）              ：tax = 0
    ///   null（查不到報價單）    ：稅別視為免稅處理，tax = 0
    /// </summary>
    private async Task<(int TotalTax, int TotalAmount)> AddDetailsAsync(
        Guid invoiceId, List<InvoiceDetailDto> detailDtos)
    {
        // 批次查詢所有需要的報價單稅別（避免 N+1 查詢）
        var itemIds = detailDtos
            .Where(d => d.ItemId.HasValue)
            .Select(d => d.ItemId!.Value)
            .Distinct()
            .ToList();

        // 以 Dictionary 快速查找：itemId → taxtype
        var taxTypeMap = itemIds.Count > 0
            ? await _db.Items
                .Where(it => itemIds.Contains(it.Itemid))
                .ToDictionaryAsync(it => it.Itemid, it => it.Taxtype)
            : new Dictionary<Guid, short?>();

        var totalTax    = 0;
        var totalAmount = 0;
        var freq        = 1;

        foreach (var d in detailDtos)
        {
            var price = d.Price ?? 0;

            // 查詢該明細對應報價單的稅別；查不到時預設免稅
            short? taxType = null;
            if (d.ItemId.HasValue && taxTypeMap.TryGetValue(d.ItemId.Value, out var tt))
                taxType = tt;

            var detailTax = CalculateTax(price, taxType);

            _db.Invoicedetails.Add(new Invoicedetail
            {
                Invoicedetailid = Guid.NewGuid(),
                Invoiceid       = invoiceId,
                Itemid          = d.ItemId,
                Invoicetype     = d.InvoiceType,
                Invoicedate     = d.InvoiceDate,
                Invoicenumber   = d.InvoiceNumber?.Trim(),
                Price           = price,
                Tax             = detailTax,
                Remark          = d.Remark?.Trim(),
                Freq            = freq++
            });

            totalTax    += detailTax;
            totalAmount += price;
        }

        await _db.SaveChangesAsync();

        return (totalTax, totalAmount);
    }

    /// <summary>
    /// 依稅別計算單筆請款明細的稅額（price 為未稅金額）。
    ///   taxtype 0（稅外加）/ 1（稅內含）：tax = round(price * 0.05)
    ///   taxtype 2（免稅）              ：tax = 0
    ///   null                          ：視為免稅，tax = 0
    /// </summary>
    private static int CalculateTax(int price, short? taxType)
        => taxType switch
        {
            // 0（稅外加）與 1（稅內含）在請款明細層級的算法相同：
            // 表單輸入的 price 是「未稅金額」，稅額一律外加 5%。
            // 稅別只影響報價單標頭如何由總價反推未稅，不影響明細；
            // 舊寫法對 taxtype 1 反推內含稅（price - round(price/1.05)），
            // 會讓 total + tax（列表、PDF、報價單已收款金額的通用算式）短少約 5%。
            0 or 1 => (int)Math.Round(price * 0.05),
            _      => 0   // taxtype 2（免稅）或 null 均為 0
        };
}
