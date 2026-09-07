-- 回填：已核銷（incomeid 有值）但狀態仍停在「已開(0)/已寄出(1)」的請款單，改為「已入帳(2)」。
-- 背景：先前的入帳核銷只寫入 invoices.incomeid，漏了 invoices.status，
--       修正後（IncomeService.CreateAsync）新資料會自動標記，舊資料需要這支腳本補。
-- 作廢(3) 的請款單不動。
-- 執行前可先確認影響範圍：
--   SELECT status, COUNT(*) FROM dbo.invoices WHERE incomeid IS NOT NULL GROUP BY status;
UPDATE dbo.invoices
    SET status = 2
    WHERE incomeid IS NOT NULL
      AND status IN (0, 1);
GO
