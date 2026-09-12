-- 回填 items.income（已收款金額，含稅）。
-- 背景：入帳核銷（IncomeService）過去只更新 invoices，沒有回寫報價單（items），
--       新版程式碼已在 IncomeService/InvoiceService 加上即時重算
--       （ItemSettlementService.RecalculateAsync），此腳本補回填既有資料。
-- 公式（與 ItemSettlementService.RecalculateAsync 完全一致，勿自行改用其他稅別分支）：
--   income = SUM(invoicedetails.price + invoicedetails.tax)
--            FROM invoicedetails d JOIN invoices v ON v.invoiceid = d.invoiceid
--            WHERE d.itemid = items.itemid AND v.incomeid IS NOT NULL
-- 影響範圍：dbo.items.income 全表。
-- 可重複執行（idempotent）：每次都是「由資料重算」覆蓋寫入，不是累加；
--                            沒有任何已核銷明細的報價單會被設為 0。
--
-- 驗證用：執行前先看回填前後差異（僅 SELECT，不影響資料）：
--   SELECT i.itemid, i.itemcode, i.income AS CurrentIncome, x.NewIncome
--   FROM dbo.items i
--   OUTER APPLY (
--       SELECT SUM(ISNULL(d.price, 0) + ISNULL(d.tax, 0)) AS NewIncome
--       FROM dbo.invoicedetails d
--       JOIN dbo.invoices v ON v.invoiceid = d.invoiceid
--       WHERE d.itemid = i.itemid AND v.incomeid IS NOT NULL
--   ) x
--   WHERE ISNULL(i.income, 0) <> ISNULL(x.NewIncome, 0)
--   ORDER BY i.itemcode;

-- ── (a) 回填 items.income ───────────────────────────────────────────────
UPDATE i
    SET i.income = ISNULL(x.NewIncome, 0)
FROM dbo.items i
OUTER APPLY (
    SELECT SUM(ISNULL(d.price, 0) + ISNULL(d.tax, 0)) AS NewIncome
    FROM dbo.invoicedetails d
    JOIN dbo.invoices v ON v.invoiceid = d.invoiceid
    WHERE d.itemid = i.itemid AND v.incomeid IS NOT NULL
) x
WHERE ISNULL(i.income, 0) <> ISNULL(x.NewIncome, 0);
GO


-- ══════════════════════════════════════════════════════════════════════════
-- ⚠⚠⚠ 以下 (b) status 回填「預設停用」，整段以 /* ... */ 註解掉 ⚠⚠⚠
--
-- 這一段會一次改動既有報價單的 status（已簽約1 ↔ 已結案2），影響範圍可能達
-- 數百筆，屬於資料語意變更（不只是補值），請務必：
--   1. 先執行上面的驗證 SELECT（改成比對 status 版本，或另外抽樣確認），
--   2. 與業務單位/PM 確認影響的報價單清單可接受，
--   3. 有把握後才取消註解、單獨執行這一段（建議先在測試庫跑過）。
--
-- 規則（與 ItemSettlementService.RecalculateAsync 完全一致，單向：只結案不退回）：
--   已收訖 = (1) 至少 1 筆 invoicedetails 引用此 itemid
--            AND (2) 所有引用到此 itemid 的 invoices 都已核銷（incomeid IS NOT NULL），
--                    作廢(status = 3)的發票不列入判斷（它永遠不會被核銷）
--            AND (3) income + 5（容差，理由見 ItemSettlementService 註解）>= items.total
--   status = 1（已簽約）且已收訖 → 改為 2（已結案）
--   status = 2（已結案）一律不動 —— 無法分辨自動結案與使用者手動結案（折讓/合意結案），
--                                  反向退回會覆蓋使用者的決定
--   status 為 0（已報價）/ 3（已取消）一律不動
--   items.total 為 NULL 或 <= 0 者不處理
--
-- 注意：稅內含(taxtype = 1)的報價單，舊資料的請款明細稅額是用「反推內含稅」算的，
--       price + tax 會比實際含稅金額短少約 5%，因此多半不會通過 (3) 而自動結案；
--       如需一併修正歷史資料，見下方 (c) 段。
-- ══════════════════════════════════════════════════════════════════════════
/*
;WITH Settlement AS (
    SELECT
        i.itemid,
        i.status,
        CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.invoicedetails d WHERE d.itemid = i.itemid
                 )
                 AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.invoicedetails d2
                    JOIN dbo.invoices v2 ON v2.invoiceid = d2.invoiceid
                    WHERE d2.itemid = i.itemid AND v2.incomeid IS NULL AND v2.status <> 3
                 )
                 AND ISNULL(i.income, 0) + 5 >= i.total
             THEN 1 ELSE 0
        END AS IsSettled
    FROM dbo.items i
    WHERE i.status IN (1, 2)
      AND i.total IS NOT NULL
      AND i.total > 0
)
UPDATE tgt
    SET tgt.status = 2
FROM dbo.items tgt
JOIN Settlement s ON s.itemid = tgt.itemid
WHERE s.status = 1 AND s.IsSettled = 1;
GO
*/


-- ══════════════════════════════════════════════════════════════════════════
-- ⚠⚠⚠ 以下 (c) 稅內含歷史資料修正，同樣「預設停用」 ⚠⚠⚠
--
-- 背景：請款明細輸入的 price 是「未稅金額」（前端欄位標示「金額 (未稅)」），
--       稅額應一律外加 5%。但舊版程式對 taxtype = 1（稅內含）的明細改用
--       「反推內含稅」tax = price - round(price / 1.05)，導致
--       total + tax（列表、PDF、報價單已收款金額共用的含稅算式）短少約 5%。
--       程式已修正（InvoiceService.CalculateTax），新開立與重新儲存的請款單都會正確。
--
-- ⚠ 這段會改動「既有請款單」的稅額與總額 —— 那些單據可能已經寄給客戶、已對帳，
--   金額變動屬於歷史文件內容的修改。除非確認要讓舊資料與新算法一致，否則不要執行。
--   建議先跑下面的差異清單，評估影響筆數與金額後再決定。
--
-- 差異清單（僅 SELECT）：
--   SELECT v.invoicecode, i.itemcode, d.price,
--          d.tax AS OldTax, ROUND(d.price * 0.05, 0) AS NewTax
--   FROM dbo.invoicedetails d
--   JOIN dbo.invoices v ON v.invoiceid = d.invoiceid
--   JOIN dbo.items i ON i.itemid = d.itemid
--   WHERE i.taxtype = 1 AND d.tax <> ROUND(d.price * 0.05, 0)
--   ORDER BY v.invoicecode;
-- ══════════════════════════════════════════════════════════════════════════
/*
-- (c-1) 修正明細稅額
UPDATE d
    SET d.tax = CAST(ROUND(d.price * 0.05, 0) AS int)
FROM dbo.invoicedetails d
JOIN dbo.items i ON i.itemid = d.itemid
WHERE i.taxtype = 1
  AND d.tax <> CAST(ROUND(d.price * 0.05, 0) AS int);
GO

-- (c-2) 依修正後的明細重算發票標頭的 tax / total
UPDATE v
    SET v.tax   = ISNULL(x.SumTax, 0),
        v.total = ISNULL(x.SumPrice, 0)
FROM dbo.invoices v
OUTER APPLY (
    SELECT SUM(ISNULL(d.tax, 0)) AS SumTax, SUM(ISNULL(d.price, 0)) AS SumPrice
    FROM dbo.invoicedetails d
    WHERE d.invoiceid = v.invoiceid
) x
WHERE ISNULL(v.tax, 0) <> ISNULL(x.SumTax, 0)
   OR ISNULL(v.total, 0) <> ISNULL(x.SumPrice, 0);
GO

-- (c-3) 稅額改變後，報價單的已收款金額要重跑一次上面的 (a) 段
GO
*/
