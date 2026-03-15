using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotationApi.DTOs.Common;
using QuotationApi.DTOs.Customer;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 客戶管理服務
/// - GetListAsync:  Dapper 查詢，JOIN customertypes，支援 name/code 關鍵字搜尋
/// - GetByIdAsync:  Dapper 查詢完整詳情（JOIN customertype + country），另查 contacts
/// - CreateAsync:   EF Core 新增，自動產生 CUS{yyyyMMdd}{NNN} 編碼，儲存聯絡人
/// - UpdateAsync:   EF Core 更新客戶欄位，刪舊聯絡人後插入新聯絡人
/// - DeleteAsync:   回傳 (Found, Error)；有報價單時拒絕刪除
/// </summary>
public class CustomerService
{
    private readonly QuotationDbContext _db;
    private readonly IDbConnection _dapper;

    // Asia/Taipei 時區，避免每次呼叫重複查找
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public CustomerService(QuotationDbContext db, IDbConnection dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    // ── 查詢 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得客戶清單（分頁），依 createdate DESC 排序。
    /// 可選填 search 關鍵字，對 name 與 code 欄位進行 LIKE 搜尋。
    /// HasQuotations 用於前端判斷是否顯示刪除按鈕。
    /// </summary>
    public async Task<PaginatedResponse<CustomerListDto>> GetListAsync(int page, int pageSize, string? search)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var whereClause = hasSearch
            ? "WHERE c.name LIKE @Search OR c.code LIKE @Search"
            : string.Empty;

        object param = hasSearch
            ? new { Search = $"%{search!.Trim()}%", Offset = (page - 1) * pageSize, PageSize = pageSize }
            : new { Offset = (page - 1) * pageSize, PageSize = pageSize };

        // 先計算符合條件的總筆數
        var countSql = $"SELECT COUNT(*) FROM customers c {whereClause}";
        var totalCount = await _dapper.ExecuteScalarAsync<int>(countSql, param);

        var dataSql = $"""
            SELECT
                c.customerid        AS CustomerId,
                c.code              AS Code,
                c.name              AS Name,
                ct.title            AS CustomerTypeName,
                c.phone             AS Phone,
                c.createdate        AS CreateDate,
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM items i WHERE i.customerid = c.customerid
                ) THEN 1 ELSE 0 END AS BIT) AS HasQuotations
            FROM customers c
            LEFT JOIN customertypes ct ON ct.customertypeid = c.customertypeid
            {whereClause}
            ORDER BY c.createdate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = await _dapper.QueryAsync<CustomerListDto>(dataSql, param);
        return PaginatedResponse<CustomerListDto>.Create(items.AsList(), page, pageSize, totalCount);
    }

    /// <summary>
    /// 取得單一客戶完整詳情，含聯絡人列表。
    /// 使用 Dapper 完成 JOIN 查詢；聯絡人另行查詢並依 freq 排序。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<CustomerDetailDto?> GetByIdAsync(int id)
    {
        const string customerSql = """
            SELECT
                c.customerid        AS CustomerId,
                c.code              AS Code,
                c.name              AS Name,
                c.address           AS Address,
                c.customertypeid    AS CustomerTypeId,
                ct.title            AS CustomerTypeName,
                c.countryid         AS CountryId,
                co.title            AS CountryName,
                c.phone             AS Phone,
                c.fax               AS Fax,
                c.vatnumber         AS VatNumber,
                c.logo              AS Logo,
                c.createdate        AS CreateDate
            FROM customers c
            LEFT JOIN customertypes ct ON ct.customertypeid = c.customertypeid
            LEFT JOIN country co       ON co.countryid      = c.countryid
            WHERE c.customerid = @Id
            """;

        var customer = await _dapper.QueryFirstOrDefaultAsync<CustomerDetailDto>(
            customerSql, new { Id = id });

        if (customer == null)
            return null;

        // 聯絡人依 freq（排序權重）升冪排列
        const string contactSql = """
            SELECT
                customerdetailid    AS ContactId,
                name                AS Name,
                email               AS Email,
                phone               AS Phone,
                ext                 AS Ext
            FROM customerdetails
            WHERE customerid = @Id
            ORDER BY freq ASC, customerdetailid ASC
            """;

        var contacts = await _dapper.QueryAsync<ContactDto>(contactSql, new { Id = id });
        customer.Contacts = contacts.AsList();

        return customer;
    }

    // ── 寫入 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增客戶。
    /// 自動產生 CUS{yyyyMMdd}{NNN} 編碼（依台北時區當日流水號遞增）。
    /// 聯絡人依傳入順序設定 freq 值（1-based）。
    /// </summary>
    public async Task<CustomerDetailDto> CreateAsync(CustomerCreateUpdateDto dto)
    {
        var code = await GenerateCodeAsync();

        var taipeiNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

        var customer = new Customer
        {
            Code           = code,
            Name           = dto.Name.Trim(),
            Address        = dto.Address?.Trim(),
            Customertypeid = dto.CustomerTypeId,
            Countryid      = dto.CountryId,
            Phone          = dto.Phone?.Trim(),
            Fax            = dto.Fax?.Trim(),
            Vatnumber      = dto.VatNumber?.Trim(),
            Createdate     = taipeiNow
        };

        _db.Customers.Add(customer);

        // 先儲存取得 customerid，再插入聯絡人
        await _db.SaveChangesAsync();

        if (dto.Contacts != null && dto.Contacts.Count > 0)
        {
            var freq = 1;
            foreach (var contact in dto.Contacts)
            {
                _db.Customerdetails.Add(new Customerdetail
                {
                    Customerdetailid = Guid.NewGuid(),
                    Customerid       = customer.Customerid,
                    Name             = contact.Name?.Trim(),
                    Email            = contact.Email?.Trim(),
                    Phone            = contact.Phone?.Trim(),
                    Ext              = contact.Ext?.Trim(),
                    Freq             = freq++
                });
            }
            await _db.SaveChangesAsync();
        }

        // 重新以完整查詢回傳（含 join 資料）
        return (await GetByIdAsync(customer.Customerid))!;
    }

    /// <summary>
    /// 更新客戶資料與聯絡人。
    /// 聯絡人採合併策略：
    ///   - 前端傳回有 ContactId 的 → 更新欄位
    ///   - 前端傳回無 ContactId 的 → 新增
    ///   - 資料庫有但前端未傳回的 → 若未被 items 引用則刪除，否則保留
    /// 這樣可避免刪除被報價單引用的聯絡人導致 FK 錯誤。
    /// </summary>
    /// <returns>找不到時回傳 null</returns>
    public async Task<CustomerDetailDto?> UpdateAsync(int id, CustomerCreateUpdateDto dto)
    {
        var customer = await _db.Customers
            .Include(c => c.Customerdetails)
            .FirstOrDefaultAsync(c => c.Customerid == id);

        if (customer == null)
            return null;

        // 更新客戶欄位
        customer.Name           = dto.Name.Trim();
        customer.Address        = dto.Address?.Trim();
        customer.Customertypeid = dto.CustomerTypeId;
        customer.Countryid      = dto.CountryId;
        customer.Phone          = dto.Phone?.Trim();
        customer.Fax            = dto.Fax?.Trim();
        customer.Vatnumber      = dto.VatNumber?.Trim();

        // ── 聯絡人合併 ──────────────────────────────────────────────────────

        var incomingContacts = dto.Contacts ?? new List<ContactDto>();

        // 前端傳回的現有聯絡人 ID 集合
        var incomingIds = incomingContacts
            .Where(c => c.ContactId.HasValue && c.ContactId != Guid.Empty)
            .Select(c => c.ContactId!.Value)
            .ToHashSet();

        // 查詢哪些聯絡人被 items 引用（不可刪除）
        var existingIds = customer.Customerdetails.Select(d => d.Customerdetailid).ToList();
        var referencedIds = existingIds.Count > 0
            ? (await _db.Items
                .Where(i => i.Customerdetailid.HasValue && existingIds.Contains(i.Customerdetailid.Value))
                .Select(i => i.Customerdetailid!.Value)
                .Distinct()
                .ToListAsync())
                .ToHashSet()
            : new HashSet<Guid>();

        // 刪除：資料庫有但前端未傳回，且未被引用
        var toRemove = customer.Customerdetails
            .Where(d => !incomingIds.Contains(d.Customerdetailid) &&
                        !referencedIds.Contains(d.Customerdetailid))
            .ToList();
        _db.Customerdetails.RemoveRange(toRemove);

        // 更新 + 新增
        var freq = 1;
        foreach (var contact in incomingContacts)
        {
            if (contact.ContactId.HasValue && contact.ContactId != Guid.Empty)
            {
                // 更新現有聯絡人
                var existing = customer.Customerdetails
                    .FirstOrDefault(d => d.Customerdetailid == contact.ContactId.Value);
                if (existing != null)
                {
                    existing.Name  = contact.Name?.Trim();
                    existing.Email = contact.Email?.Trim();
                    existing.Phone = contact.Phone?.Trim();
                    existing.Ext   = contact.Ext?.Trim();
                    existing.Freq  = freq++;
                }
            }
            else
            {
                // 新增聯絡人
                _db.Customerdetails.Add(new Customerdetail
                {
                    Customerdetailid = Guid.NewGuid(),
                    Customerid       = customer.Customerid,
                    Name             = contact.Name?.Trim(),
                    Email            = contact.Email?.Trim(),
                    Phone            = contact.Phone?.Trim(),
                    Ext              = contact.Ext?.Trim(),
                    Freq             = freq++
                });
            }
        }

        // 被引用但前端已移除的聯絡人，保留但排在最後
        var keptOrphans = customer.Customerdetails
            .Where(d => !incomingIds.Contains(d.Customerdetailid) &&
                        referencedIds.Contains(d.Customerdetailid))
            .ToList();
        foreach (var orphan in keptOrphans)
        {
            orphan.Freq = freq++;
        }

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 刪除客戶。
    /// 若客戶有關聯報價單（items 資料表）則拒絕刪除，回傳業務錯誤訊息。
    /// 需手動刪除 customerdetails（FK 未設 cascade delete）。
    /// </summary>
    /// <returns>(Found: false) 找不到記錄；(Error: non-null) 業務規則拒絕</returns>
    public async Task<(bool Found, string? Error)> DeleteAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Customerdetails)
            .FirstOrDefaultAsync(c => c.Customerid == id);

        if (customer == null)
            return (Found: false, Error: null);

        // 刪除保護：有報價單的客戶不可刪除
        var hasItems = await _db.Items.AnyAsync(i => i.Customerid == id);
        if (hasItems)
            return (Found: true, Error: "此客戶已有關聯報價單，無法刪除。");

        // 手動刪除聯絡人（FK 未設 cascade delete）
        _db.Customerdetails.RemoveRange(customer.Customerdetails);
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        return (Found: true, Error: null);
    }

    // ── 私有輔助 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 產生 CUS{yyyyMMdd}{NNN} 格式的客戶編碼。
    /// 每日流水號從 001 開始，依當天已存在的 CUS{yyyyMMdd}* 數量遞增。
    /// 使用台北時區確保跨午夜時編碼日期正確。
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);
        var dateStr = today.ToString("yyyyMMdd");
        var prefix = $"CUS{dateStr}";

        // 計算今日已存在的編碼數量作為流水號基數
        var count = await _db.Customers
            .CountAsync(c => c.Code != null && c.Code.StartsWith(prefix));

        var seq = (count + 1).ToString("D3");
        return $"{prefix}{seq}";
    }
}
