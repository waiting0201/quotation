-- 補掛核銷：INV20260717001 ←→ INC20260907007
-- 背景：2026-09-07 批次入帳時這筆漏了勾選請款單，incomes 有記錄但 invoices.incomeid 仍為 NULL，
--       導致該請款單停在「已開(0)」。金額可對上：61,200(未稅) + 3,060(稅) = 64,260 = 入帳金額，
--       客戶同為 13185。
-- 條件加上 incomeid IS NULL，重複執行不會覆蓋既有核銷。
UPDATE inv
    SET inv.incomeid = inc.incomeid,
        inv.status   = 2
    FROM dbo.invoices inv
    CROSS JOIN dbo.incomes inc
    WHERE inv.invoicecode = 'INV20260717001'
      AND inc.incomecode  = 'INC20260907007'
      AND inv.incomeid IS NULL
      AND inv.status <> 3;
GO
