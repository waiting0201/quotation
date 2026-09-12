---
name: item-settlement-design
description: items.income/status 由 ItemSettlementService 從資料重算的規則、驗證過的 income 公式、單向結案的決策理由與併發鎖定作法
metadata:
  type: project
---

## 背景
`Api/Services/ItemSettlementService.cs`（2026-09-12 引入）負責把入帳核銷回寫到報價單
（`items.income` / `items.status`）。由 `IncomeService.CreateAsync/DeleteAsync`、
`InvoiceService.UpdateAsync/DeleteAsync` 呼叫，皆包在 explicit transaction 內。
對應 migration：`docs/migrations/2026-09-12-backfill-item-income-status.sql`。

## income 公式（已驗證，勿質疑／勿自行改稅別分支）
```
income = Σ (invoicedetails.price + invoicedetails.tax)
         WHERE invoicedetails.itemid = @itemId AND invoices.incomeid IS NOT NULL
```
這是查證正式庫 1251 筆資料（1217 筆吻合）得出的舊系統慣例，與前端/PDF 顯示請款
含稅金額的方式一致。C# 與 SQL migration 兩邊已核對一致。

## status 自動切換規則（單向）
只做 `已簽約(1)` → `已結案(2)`；`已報價(0)`/`已結案(2)`/`已取消(3)` 一律不動。
「已收訖」= 該 item 至少一筆 invoicedetails AND 沒有任何非作廢的未核銷 invoice AND
`income + 5(容差) >= items.total`。

**Why 單向**：系統無法分辨「自動結案」與「使用者手動結案」（折讓、合意結案本來就收不足），
反向退回會悄悄覆蓋使用者的決定 —— 使用者 2026-09-12 明確裁示不要覆蓋。代價是刪除入帳後
報價單會停在已結案，需要時人工改回。

**Why 5 元容差**：逐筆明細四捨五入的稅額加總，與整張報價單四捨五入的 total 相比會有
幾元捨入誤差。

## 2026-09-12 code review 發現的三點，處理結果

1. **`InvoiceService.CreateAsync` 不呼叫 `RecalculateAsync`** — 改為單向結案後這確定是
   no-op（新建發票 incomeid 必為 null，income 不變；而新增一張未核銷發票在單向規則下
   也不會改變 status），故維持不呼叫，但補上了 explicit transaction 包住原本兩次
   SaveChangesAsync。

2. **併發沒有保護** — 已修正。新增 `ItemSettlementService.LockItemsAsync`，在動到任何
   發票之前先對受影響的 items 取 `UPDLOCK, HOLDLOCK`（參數化 IN + ORDER BY itemid 讓
   鎖定順序一致）。所有呼叫端都在交易開頭先鎖再改。已用兩筆入帳同時核銷同一報價單的
   兩張請款單實測：income 兩筆都算到，無 deadlock、無 lost update。

3. **自動切換誤蓋手動決定** — 已由單向規則解決（見上）。

## 同批修正的稅內含 bug
`InvoiceService.CalculateTax` 原本對 taxtype 1（稅內含）用反推內含稅
`price - round(price/1.05)`，但請款明細輸入的 price 是**未稅金額**（前端欄位標示
「金額 (未稅)」、摘要寫「總額（含稅）= 合計金額 + 合計稅額」），等於把未稅金額再抽一次稅，
讓 `total + tax` 短少約 5%。已改成 taxtype 0 與 1 同樣 `tax = round(price * 0.05)`，
前端 `invoice-form.component.ts` 的 `rowTax` 同步修正。稅別只影響報價單標頭如何由總價
反推未稅，不影響請款明細。

歷史資料未改（migration (c) 段預設註解掉）：舊的稅內含請款單稅額仍偏低，那些報價單
income 會比 total 少約 5% 而不會自動結案 —— 但因為結案是單向的，它們既有的狀態不會被動到。

See also [[transaction-conventions]]。
