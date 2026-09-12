# Memory Index

- [Explicit transaction 邊界](ef-transaction-boundary.md) — BeginTransactionAsync 必須包住「整個」跨聚合根操作，不能只包最後一段
- [報價單收款回寫](item-settlement.md) — items.income/status 由 ItemSettlementService 統一重算，串接點與公式來源
