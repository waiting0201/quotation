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
/// - CreateAsync:              EF Core 新增，自動產生 INV{yyyyMMdd}{NNN} 編碼，計算稅額
/// - UpdateAsync:              EF Core 更新標頭、刪舊明細後重新插入、重新計算稅額
/// - DeleteAsync:              回傳 (Found, Error)；已關聯收款時拒絕刪除
/// </summary>
public class InvoiceService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public InvoiceService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
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
    ///   taxtype 0（稅外加）：tax = round(price * 0.05)
    ///   taxtype 1（稅內含）：tax = price - round(price / 1.05)
    ///   taxtype 2（免稅）  ：tax = 0
    /// 彙總所有明細的稅額與金額後寫入發票標頭。
    /// </summary>
    public async Task<InvoiceDetailResponseDto> CreateAsync(InvoiceCreateUpdateDto dto, Guid userId)
    {
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

        // 手動刪除明細（FK 未設 cascade delete）
        _db.Invoicedetails.RemoveRange(invoice.Invoicedetails);
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();

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

        // 計算今日已存在的編碼數量作為流水號基數
        var count = await _db.Invoices
            .CountAsync(i => i.Invoicecode != null && i.Invoicecode.StartsWith(prefix));

        var seq = (count + 1).ToString("D3");
        return $"{prefix}{seq}";
    }

    /// <summary>
    /// 將發票明細批次插入資料庫，並根據關聯報價單的稅別計算每筆明細的稅額。
    /// 回傳（稅額合計, 金額合計）供更新發票標頭使用。
    ///
    /// 稅別計算規則（依 items.taxtype）：
    ///   0（稅外加）：tax = round(price * 0.05)；total 計入 price（未稅金額）
    ///   1（稅內含）：tax = price - round(price / 1.05)；total 計入 price（含稅金額）
    ///   2（免稅）  ：tax = 0；total 計入 price
    ///   null（查不到報價單）：稅別視為免稅處理，tax = 0
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
    /// 依稅別計算單筆稅額。
    ///   taxtype 0（稅外加）：tax = round(price * 0.05)
    ///   taxtype 1（稅內含）：tax = price - round(price / 1.05)  → 反推未稅部分的稅額
    ///   taxtype 2（免稅）  ：tax = 0
    ///   null               ：視為免稅，tax = 0
    /// </summary>
    private static int CalculateTax(int price, short? taxType)
        => taxType switch
        {
            0 => (int)Math.Round(price * 0.05),
            1 => price - (int)Math.Round(price / 1.05),
            _ => 0   // taxtype 2（免稅）或 null 均為 0
        };
}
