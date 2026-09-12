using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuotationApi.Models;

namespace QuotationApi.Services;

/// <summary>
/// 報價單收款狀態重算服務。
///
/// 背景：核銷入帳（IncomeService）過去只更新 invoices.status，沒有回寫到報價單（items），
/// 導致 items.income 與 items.status 逐漸與實際收款情況脫節。此服務負責「由資料重算」
/// 兩個欄位，而非用 += 累加，確保重複執行、跨服務呼叫皆可自我修復（idempotent）。
///
/// (a) items.income 重算公式（已收款金額，含稅）：
/// <code>
///   income = Σ (invoicedetails.price + invoicedetails.tax)
///            FROM invoicedetails d JOIN invoices v ON v.invoiceid = d.invoiceid
///            WHERE d.itemid = @itemId AND v.incomeid IS NOT NULL
/// </code>
/// 此公式是查證舊系統資料得出的慣例（1251 筆中 1217 筆吻合），也與新系統顯示請款
/// 含稅金額的方式一致（invoice-list 顯示 total + tax、InvoicePdfService grandTotal =
/// subtotal + tax），因此不自行改用其他稅別分支重新計算。
///
/// (b) items.status 自動維護（已報價0 → 已簽約1 → 已結案2 → 已取消3）：
/// 定義「已收訖」= 同時滿足：
///   1. 該報價單至少有一筆 invoicedetails；
///   2. 所有引用到此 itemid 的 invoices 都已核銷（incomeid IS NOT NULL），
///      作廢(3)的發票不列入判斷；
///   3. income + 容差（<see cref="SettlementTolerance"/>）&gt;= items.total（含稅折後總額）。
/// 規則（**單向**：只自動結案，不自動退回）：
///   - status == 1（已簽約）且已收訖 → 改為 2（已結案）
///   - status == 2（已結案）一律不動。系統無法分辨「自動結案」與「使用者手動結案」
///     （例如折讓或合意結案，收款金額本來就不會收足），若反向退回會覆蓋掉使用者的決定，
///     因此刻意不做 2 → 1。代價是刪除入帳後報價單會停在已結案，需要時由使用者手動改回。
///   - status 為 0（已報價）或 3（已取消）一律不動
///   - items.total 為 null 或 &lt;= 0 時不做狀態自動變更（只更新 income）
/// </summary>
public class ItemSettlementService
{
    private readonly QuotationDbContext _db;

    // 報價單狀態：已報價(0) → 已簽約(1) → 已結案(2) → 已取消(3)
    private const short ItemStatusSigned   = 1;
    private const short ItemStatusClosed   = 2;

    // 發票狀態：已開(0) → 已寄出(1) → 已入帳(2) → 作廢(3)
    private const short InvoiceStatusVoid  = 3;

    /// <summary>
    /// 判斷「已收訖」時允許的金額容差（新台幣元）。
    /// 逐筆明細四捨五入的稅額加總，與報價單整張四捨五入的 total 相比，
    /// 可能產生幾元的捨入誤差，故用小額容差避免因此無法自動結案。
    /// </summary>
    private const int SettlementTolerance = 5;

    public ItemSettlementService(QuotationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 在交易內先鎖定指定報價單（items）的資料列，序列化同一張報價單的收款重算。
    ///
    /// 為什麼需要：重算是「讀目前所有相關發票 → 覆寫 items.income」，若兩個使用者同時
    /// 對「同一張報價單的不同發票」入帳，兩邊會各自讀到看不見對方的快照，後 commit 的
    /// 那筆會用少算一張發票的金額覆蓋掉先前的結果（lost update，Azure SQL 預設開啟
    /// READ_COMMITTED_SNAPSHOT 時尤其明顯）；在非快照隔離下則會因為互相等待對方鎖住的
    /// 發票資料列而 deadlock。
    ///
    /// 作法：在動到任何發票之前，先對受影響的 items 取得 UPDLOCK/HOLDLOCK（持有到交易結束）。
    /// 兩個交易都以 items 為第一個鎖定對象，鎖定順序一致，後到者會等前者 commit 後再重算，
    /// 因此不會有鎖反轉造成的 deadlock，也不會讀到過期快照。
    ///
    /// 呼叫端必須已開啟 explicit transaction，並在修改發票之前呼叫。
    /// </summary>
    public async Task LockItemsAsync(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        // 以參數化 IN 清單避免 SQL injection；ORDER BY itemid 讓多筆時的掃描（即鎖定）
        // 順序在所有交易間一致，進一步避免彼此死鎖。
        var placeholders = string.Join(", ", ids.Select((_, i) => $"@p{i}"));
        var parameters   = ids.Select((id, i) => new SqlParameter($"@p{i}", id)).ToArray<object>();

        // SQL 以變數組合而非內插字串傳入，避免 EF1002 分析器警告；
        // 內插的只有自行產生的參數名（@p0、@p1…），使用者輸入一律走參數。
        var sql = "SELECT itemid FROM items WITH (UPDLOCK, HOLDLOCK) WHERE itemid IN ("
                + placeholders + ") ORDER BY itemid";

        await _db.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    /// <summary>
    /// 依資料庫現況重新計算指定報價單的 income 與 status。
    /// 呼叫端負責交易邊界（本方法只修改追蹤中的 entity，不呼叫 SaveChangesAsync），
    /// 並應在修改發票前先呼叫 <see cref="LockItemsAsync"/> 取得報價單資料列鎖。
    /// </summary>
    public async Task RecalculateAsync(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        // 批次查詢每個 itemId 已核銷明細的金額合計（含稅），避免逐筆查詢造成 N+1。
        var incomeMap = await _db.Invoicedetails
            .Where(d => d.Itemid != null
                     && ids.Contains(d.Itemid.Value)
                     && d.Invoice != null
                     && d.Invoice.Incomeid != null)
            .GroupBy(d => d.Itemid!.Value)
            .Select(g => new { ItemId = g.Key, Income = g.Sum(d => (d.Price ?? 0) + (d.Tax ?? 0)) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Income);

        // 該報價單是否「至少有一筆」invoicedetails（不論是否已核銷）
        var itemIdsWithDetails = (await _db.Invoicedetails
            .Where(d => d.Itemid != null && ids.Contains(d.Itemid.Value))
            .Select(d => d.Itemid!.Value)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        // 該報價單是否存在「尚未核銷」的發票（只要有一筆未核銷，就不算已收訖）。
        // 作廢(3)的發票排除在外：它永遠不會被核銷（incomeid 恆為 null），
        // 若計入會讓「作廢後重開一張」的報價單永遠無法自動結案。
        var itemIdsWithPendingInvoice = (await _db.Invoicedetails
            .Where(d => d.Itemid != null
                     && ids.Contains(d.Itemid.Value)
                     && d.Invoice != null
                     && d.Invoice.Incomeid == null
                     && d.Invoice.Status != InvoiceStatusVoid)
            .Select(d => d.Itemid!.Value)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var items = await _db.Items
            .Where(i => ids.Contains(i.Itemid))
            .ToListAsync();

        foreach (var item in items)
        {
            var income = incomeMap.TryGetValue(item.Itemid, out var sum) ? sum : 0;
            item.Income = income;

            // 只有「已簽約」會被自動結案；已報價(0)/已結案(2)/已取消(3) 一律不動。
            // 不做 2 → 1 的反向退回，避免覆蓋使用者手動結案的決定（詳見類別註解）。
            if (item.Status != ItemStatusSigned)
                continue;

            // total 無效時無法判斷是否已收訖，只更新 income
            if (item.Total is null || item.Total <= 0)
                continue;

            var hasDetails         = itemIdsWithDetails.Contains(item.Itemid);
            var allInvoicesSettled = hasDetails && !itemIdsWithPendingInvoice.Contains(item.Itemid);
            var isFullySettled     = allInvoicesSettled && income + SettlementTolerance >= item.Total.Value;

            if (isFullySettled)
                item.Status = ItemStatusClosed;
        }
    }
}
