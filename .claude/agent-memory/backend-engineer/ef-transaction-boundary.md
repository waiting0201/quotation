---
name: ef-transaction-boundary
description: 在 EF Core 方法中加入 explicit transaction 時，BeginTransactionAsync 要放在最前面，不能只包住最後一段 SaveChanges
metadata:
  type: feedback
---

當一個方法內本來就有多段「修改 entity → SaveChangesAsync」的既有流程（例如 InvoiceService.UpdateAsync
先刪明細 SaveChanges，再插入明細 SaveChanges），之後要加 explicit transaction 讓整個方法變成原子操作時，
`await _db.Database.BeginTransactionAsync()` 必須放在**方法最開頭、第一次 SaveChangesAsync 之前**，
而不是圖方便加在最後一段（例如只包住新加的重算邏輯）。

**Why:** 我第一次寫 `ItemSettlementService` 串接時，把 `BeginTransactionAsync()` 加在 `UpdateAsync` 尾端
（重算前一刻），結果刪除舊明細、插入新明細的兩次 SaveChangesAsync 都發生在交易開始「之前」，等於已經
落地到資料庫，只有最後的重算是交易保護的 —— 完全沒達到「整個更新操作原子化」的目的。EF Core 的規則是：
一旦有 explicit transaction 在跑，後續每次 SaveChangesAsync 都會自動併入這個交易直到 Commit/Rollback，
所以只要把 `BeginTransactionAsync()` 移到最前面，中間不管呼叫幾次 SaveChangesAsync 都會是同一個原子單位。

**How to apply:** 每次要幫既有多段 SaveChangesAsync 的方法補 explicit transaction，先畫出方法內所有
SaveChangesAsync 呼叫點，把 transaction 開在第一個修改動作之前（通常是方法最前面，讀完資料、算完
before/after 差異之後），Commit 放在最後一次 SaveChangesAsync 之後。寫完要重新檢查一次「transaction 開始
之前是否還有任何寫入」，這是最容易漏掉的地方。相關：[[item-settlement]]
