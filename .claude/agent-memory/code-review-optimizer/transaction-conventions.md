---
name: transaction-conventions
description: 這個 repo 寫「多步驟需要原子性」的 EF Core explicit transaction 的固定寫法，審查時該檢查什麼
metadata:
  type: project
---

## 慣例寫法
`UserService.cs` / `GroupService.cs` / `IncomeService.cs` / `InvoiceService.cs` 對需要
跨多次 `SaveChangesAsync()` 才能完成的操作，一律用：

```csharp
// 先在記憶體標記變更（Add/Remove/欄位賦值 — 這些不會馬上打到 DB）
...
await using var transaction = await _db.Database.BeginTransactionAsync();
await _db.SaveChangesAsync();      // 第一批
// 可能有依賴第一批結果的後續查詢/運算（例如 ItemSettlementService.RecalculateAsync）
await _db.SaveChangesAsync();      // 第二批
await transaction.CommitAsync();
```

**檢查重點**：EF Core 的 Add/Remove/屬性賦值都只是改變追蹤狀態，實際 DB 寫入延遲到
`SaveChangesAsync()`；所以就算這些標記動作寫在 `BeginTransactionAsync()` 之前，只要
所有 `SaveChangesAsync()` 呼叫都在 `BeginTransactionAsync()`之後、`CommitAsync()`之前，
交易邊界仍然正確——不是缺陷。真正該挑的是：
- 是否有 `SaveChangesAsync()` 呼叫漏在 transaction 範圍外
- `await using` 若拿掉、換成沒有正確 dispose/rollback 的寫法
- 交易內用 Dapper（`IDbConnection`，見 Program.cs 是獨立 `SqlConnection`，跟 EF 的
  connection 不是同一條）讀取尚未 commit 的資料 —— 這個 repo 目前所有這類方法都是在
  `CommitAsync()` 之後才呼叫 Dapper 查詢做回傳，是安全的；但新增類似程式碼時要注意
  Dapper 連線看不到 EF 交易內尚未 commit 的變更。

See also [[item-settlement-design]] 裡記錄的、這個交易模式在「同一 item 被多筆
invoice 並發核銷」情境下暴露出的並發缺口（不是交易邊界本身的問題，是 recalculate
read-then-write 沒有並發保護）。
