---
name: item-settlement
description: items.income/items.status 的自動回寫邏輯集中在 ItemSettlementService，串接於 IncomeService 與 InvoiceService
metadata:
  type: project
---

`Api/Services/ItemSettlementService.cs`（Scoped，只依賴 `QuotationDbContext`）負責把報價單
（`items`）的 `income`（已收款含稅金額）與 `status`（單向：已簽約1 → 已結案2，不反向退回）從資料庫現況
「重算」出來，而不是用 `+=` 累加 —— 目的是 idempotent、可自我修復。核心方法
`RecalculateAsync(IEnumerable<Guid> itemIds)`。

**公式（勿隨意改動）：**
`income = Σ(invoicedetails.price + invoicedetails.tax)`，只算 `invoices.incomeid IS NOT NULL`
（已核銷）的明細。這是查證舊系統 1251 筆中 1217 筆吻合得出的慣例公式，含稅、非依 taxtype 分支計算。

**status 自動切換規則：** 只在 status 為 1（已簽約）或 2（已結案）之間切換，0/3 不動；
「已收訖」= 有至少一筆 invoicedetails + 所有關聯發票皆已核銷 + income + 容差(5，具名常數
`SettlementTolerance`，理由是逐筆四捨五入誤差) >= items.total；total 為 null/<=0 時只更新 income
不動 status。

**串接點：**
- `IncomeService.CreateAsync` / `DeleteAsync`：核銷/解除核銷發票後呼叫。
- `InvoiceService.UpdateAsync`：明細整批刪除重建，收集「更新前 ∪ 更新後」的 itemid 一起重算。
- `InvoiceService.DeleteAsync`：刪除前收集受影響 itemid（雖然此時 incomeid 必為 null，因為已核銷
  的發票被 DeleteAsync 擋掉不給刪，但仍呼叫重算以防日後規則調整時遺漏）。
- `InvoiceService.CreateAsync`：新建發票 incomeid 必為 null，不影響 income，刻意不呼叫重算。

這是整個專案第一處使用 `_db.Database.BeginTransactionAsync()` explicit transaction 的地方
（見 [[ef-transaction-boundary]] 的教訓：transaction 要包住整個操作，不能只包最後一段）。

**回填 migration：** `docs/migrations/2026-09-12-backfill-item-income-status.sql`，income 回填
直接執行，status 回填整段註解掉（影響數百筆既有資料，需人工確認後才解除註解執行）。
