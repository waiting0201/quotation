using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Quotation;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 報價單管理服務
/// - GetListAsync:   Dapper 查詢，JOIN customers，支援 itemcode/name/customerName 關鍵字搜尋
/// - GetByIdAsync:   Dapper 查詢完整詳情，另查明細（itemdetails）與內容（itemcontents）
/// - CreateAsync:    EF Core 新增，自動產生 QUO{yyyyMMdd}{NNN} 編碼，依稅別計算稅額
/// - UpdateAsync:    EF Core 更新標頭，刪舊明細/內容後整批重新插入，重新計算稅額
/// - DeleteAsync:    回傳 (Found, Error)；已關聯 invoicedetails 時拒絕刪除
/// </summary>
public class QuotationService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public QuotationService(QuotationDbContext db, IDbConnection dapper)
    {
        _db     = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得報價單清單（分頁），依 createdate DESC 排序。
    /// 可選填 search 關鍵字，對 itemcode、name、customers.name 進行 LIKE 搜尋。
    /// HasInvoices 用於前端判斷是否顯示刪除按鈕。
    /// </summary>
    public async Task<PaginatedResponse<QuotationListDto>> GetListAsync(int page, int pageSize, string? search)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var whereClause = hasSearch
            ? "WHERE i.itemcode LIKE @Search OR i.name LIKE @Search OR c.name LIKE @Search"
            : string.Empty;

        object param = hasSearch
            ? new { Search = $"%{search!.Trim()}%", Offset = (page - 1) * pageSize, PageSize = pageSize }
            : new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        // 先計算符合條件的總筆數
        var countSql = $"""
            SELECT COUNT(*)
            FROM items i
            LEFT JOIN customers c ON c.customerid = i.customerid
            {whereClause}
            """;
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql, param);

        // 主查詢：含 HasInvoices 旗標（subquery 檢查 invoicedetails 是否有關聯）
        var dataSql = $"""
            SELECT
                i.itemid            AS ItemId,
                i.itemcode          AS ItemCode,
                i.name              AS Name,
                c.name              AS CustomerName,
                i.quotationdate     AS QuotationDate,
                i.taxtype           AS TaxType,
                i.tax               AS Tax,
                i.total             AS Total,
                i.status            AS Status,
                i.createdate        AS CreateDate,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM invoicedetails id WHERE id.itemid = i.itemid
                ) THEN 1 ELSE 0 END AS BIT) AS HasInvoices
            FROM items i
            LEFT JOIN customers c ON c.customerid = i.customerid
            {whereClause}
            ORDER BY i.createdate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<QuotationListDto>(dataSql, param);
        return PaginatedResponse<QuotationListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得單一報價單完整詳情，含明細（itemdetails）與內容（itemcontents）。
    /// 明細與內容均依 freq ASC 排序，保留使用者設定的順序。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<QuotationDetailDto?> GetByIdAsync(Guid id)
    {
        const string itemSql = """
            SELECT
                i.itemid            AS ItemId,
                i.itemcode          AS ItemCode,
                i.name              AS Name,
                i.customerid        AS CustomerId,
                c.name              AS CustomerName,
                i.customerdetailid  AS CustomerDetailId,
                i.quotationdate     AS QuotationDate,
                i.expiredate        AS ExpireDate,
                i.payment           AS Payment,
                i.remark            AS Remark,
                i.taxtype           AS TaxType,
                i.tax               AS Tax,
                i.total             AS Total,
                i.workdays          AS Workdays,
                i.status            AS Status,
                i.createdate        AS CreateDate
            FROM items i
            LEFT JOIN customers c ON c.customerid = i.customerid
            WHERE i.itemid = @Id
            """;

        var item = await _dapper.QueryFirstOrDefaultAsync<QuotationDetailDto>(itemSql, new { Id = id });

        if (item == null)
            return null;

        // 明細依 freq 升冪排列
        const string detailSql = """
            SELECT
                d.itemdetailid  AS ItemDetailId,
                d.title         AS Title,
                d.description   AS Description,
                d.quantity      AS Quantity,
                d.price         AS Price,
                d.total         AS Total,
                d.freq          AS Freq
            FROM itemdetails d
            WHERE d.itemid = @Id
            ORDER BY d.freq ASC, d.itemdetailid ASC
            """;

        var details = await _dapper.QueryAsync<QuotationDetailItemDto>(detailSql, new { Id = id });
        item.Details = details.AsList();

        // 內容依 freq 升冪排列
        const string contentSql = """
            SELECT
                c.itemcontentid AS ItemContentId,
                c.title         AS Title,
                c.remark        AS Remark,
                c.price         AS Price,
                c.freq          AS Freq
            FROM itemcontents c
            WHERE c.itemid = @Id
            ORDER BY c.freq ASC, c.itemcontentid ASC
            """;

        var contents = await _dapper.QueryAsync<QuotationContentItemDto>(contentSql, new { Id = id });
        item.Contents = contents.AsList();

        return item;
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增報價單。
    /// 自動產生 QUO{yyyyMMdd}{NNN} 編碼（依台北時區當日流水號遞增）。
    /// 依 TaxType 計算稅額：
    ///   0（稅外加）：subtotal = sum(qty*price) + sum(content.price)；tax = round(subtotal * 0.05)；total = subtotal + tax
    ///   1（稅內含）：total = subtotal；tax = subtotal - round(subtotal / 1.05)
    ///   2（免稅）  ：tax = 0；total = subtotal
    /// </summary>
    public async Task<QuotationDetailDto> CreateAsync(QuotationCreateUpdateDto dto, Guid userId)
    {
        var itemCode   = await GenerateCodeAsync();
        var taipeiNow  = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);
        var (tax, total, subtotal) = CalculateTotals(dto);

        var item = new Item
        {
            Itemid           = Guid.NewGuid(),
            Itemcode         = itemCode,
            Customerid       = dto.CustomerId,
            Customerdetailid = dto.CustomerDetailId,
            Name             = dto.Name?.Trim(),
            Quotationdate    = dto.QuotationDate,
            Expiredate       = dto.ExpireDate,
            Taxtype          = dto.TaxType ?? 0,
            Payment          = dto.Payment?.Trim(),
            Remark           = dto.Remark?.Trim(),
            Workdays         = dto.Workdays,
            Status           = dto.Status ?? 0,
            Tax              = tax,
            Total            = total,
            Createdate       = taipeiNow,
            Userid           = userId
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        // 插入明細與內容（整批插入後再 SaveChanges 一次，減少 round-trip）
        AddDetailsAndContents(item.Itemid, dto);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(item.Itemid))!;
    }

    /// <summary>
    /// 更新報價單。
    /// 更新標頭欄位後，刪除所有舊明細與內容，再整批重新插入（避免 diff 邏輯複雜化）。
    /// 重新計算稅額並更新標頭的 tax / total。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<QuotationDetailDto?> UpdateAsync(Guid id, QuotationCreateUpdateDto dto)
    {
        var item = await _db.Items
            .Include(i => i.Itemdetails)
            .Include(i => i.Itemcontents)
            .FirstOrDefaultAsync(i => i.Itemid == id);

        if (item == null)
            return null;

        var (tax, total, _) = CalculateTotals(dto);

        // 更新標頭欄位
        item.Customerid       = dto.CustomerId ?? item.Customerid;
        item.Customerdetailid = dto.CustomerDetailId;
        item.Name             = dto.Name?.Trim() ?? item.Name;
        item.Quotationdate    = dto.QuotationDate ?? item.Quotationdate;
        item.Expiredate       = dto.ExpireDate;
        item.Taxtype          = dto.TaxType ?? item.Taxtype;
        item.Payment          = dto.Payment?.Trim();
        item.Remark           = dto.Remark?.Trim();
        item.Workdays         = dto.Workdays;
        item.Status           = dto.Status ?? item.Status;
        item.Tax              = tax;
        item.Total            = total;

        // 整批取代：刪除舊明細與內容，再重新插入（避免逐筆比對）
        _db.Itemdetails.RemoveRange(item.Itemdetails);
        _db.Itemcontents.RemoveRange(item.Itemcontents);
        await _db.SaveChangesAsync();

        AddDetailsAndContents(id, dto);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除報價單。
    /// 若有 invoicedetails 記錄關聯此 itemid，則拒絕刪除並回傳業務錯誤訊息。
    /// 刪除順序：itemcontents → itemdetails → item（FK 無 cascade delete）。
    /// </summary>
    /// <returns>(Found: false) 找不到記錄；(Error: non-null) 業務規則拒絕刪除</returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(Guid id)
    {
        var item = await _db.Items
            .Include(i => i.Itemdetails)
            .Include(i => i.Itemcontents)
            .FirstOrDefaultAsync(i => i.Itemid == id);

        if (item == null)
            return (Found: false, Error: null);

        // 刪除保護：已關聯發票明細的報價單不可刪除
        var hasInvoice = await _db.Invoicedetails.AnyAsync(d => d.Itemid == id);
        if (hasInvoice)
            return (Found: true, Error: "此報價單已關聯發票明細，無法刪除。請先刪除對應的發票或明細後再試。");

        // 手動刪除子表（FK 無 cascade delete）
        _db.Itemcontents.RemoveRange(item.Itemcontents);
        _db.Itemdetails.RemoveRange(item.Itemdetails);
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();

        return (Found: true, Error: null);
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 產生 QUO{yyyyMMdd}{NNN} 格式的報價單編碼。
    /// 每日流水號從 001 開始，依當天已存在的 QUO{yyyyMMdd}* 數量遞增。
    /// 使用台北時區確保跨午夜時編碼日期正確。
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        var today   = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);
        var dateStr = today.ToString("yyyyMMdd");
        var prefix  = $"QUO{dateStr}";

        var count = await _db.Items
            .CountAsync(i => i.Itemcode != null && i.Itemcode.StartsWith(prefix));

        var seq = (count + 1).ToString("D3");
        return $"{prefix}{seq}";
    }

    /// <summary>
    /// 計算報價單稅額與合計。
    /// subtotal = sum(detail.qty * detail.price) + sum(content.price)
    ///
    /// 稅別計算規則（依 taxtype）：
    ///   0（稅外加）：tax = round(subtotal * 0.05)；total = subtotal + tax
    ///   1（稅內含）：total = subtotal；tax = subtotal - round(subtotal / 1.05)（反推未稅部分的稅額）
    ///   2（免稅）  ：tax = 0；total = subtotal
    /// </summary>
    private static (int Tax, int Total, int Subtotal) CalculateTotals(QuotationCreateUpdateDto dto)
    {
        var detailSubtotal  = dto.Details.Sum(d => (d.Quantity ?? 0) * (d.Price ?? 0));
        var contentSubtotal = dto.Contents.Sum(c => c.Price ?? 0);
        var subtotal        = detailSubtotal + contentSubtotal;

        var taxType = dto.TaxType ?? 0;
        var tax     = CalculateTax(subtotal, taxType);
        var total   = taxType == 0 ? subtotal + tax : subtotal;

        return (tax, total, subtotal);
    }

    /// <summary>
    /// 依稅別計算稅額。
    ///   taxtype 0（稅外加）：tax = round(subtotal * 0.05)
    ///   taxtype 1（稅內含）：tax = subtotal - round(subtotal / 1.05)
    ///   taxtype 2（免稅）  ：tax = 0
    /// </summary>
    private static int CalculateTax(int subtotal, short taxType)
        => taxType switch
        {
            0 => (int)Math.Round(subtotal * 0.05),
            1 => subtotal - (int)Math.Round((double)subtotal / 1.05),
            _ => 0  // taxtype 2（免稅）
        };

    /// <summary>
    /// 將報價明細（itemdetails）與內容（itemcontents）加入 EF Core change tracker。
    /// 批次加入後由呼叫方負責呼叫 SaveChangesAsync，減少 round-trip。
    /// </summary>
    private void AddDetailsAndContents(Guid itemId, QuotationCreateUpdateDto dto)
    {
        var freq = 1;
        foreach (var d in dto.Details)
        {
            var qty   = d.Quantity ?? 0;
            var price = d.Price ?? 0;

            _db.Itemdetails.Add(new Itemdetail
            {
                Itemdetailid = Guid.NewGuid(),
                Itemid       = itemId,
                Title        = d.Title?.Trim(),
                Description  = d.Description?.Trim(),
                Quantity     = qty,
                Price        = price,
                Total        = qty * price,
                Freq         = d.Freq ?? freq
            });
            freq++;
        }

        freq = 1;
        foreach (var c in dto.Contents)
        {
            _db.Itemcontents.Add(new Itemcontent
            {
                Itemcontentid = Guid.NewGuid(),
                Itemid        = itemId,
                Title         = c.Title?.Trim(),
                Remark        = c.Remark?.Trim(),
                Price         = c.Price ?? 0,
                Freq          = c.Freq ?? freq
            });
            freq++;
        }
    }
}
