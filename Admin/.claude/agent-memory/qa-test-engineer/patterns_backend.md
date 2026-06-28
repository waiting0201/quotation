---
name: patterns_backend
description: 後端品質模式、常見缺陷與風險點
type: project
---

# 後端品質模式

## 確認良好的實踐
- Dapper 查詢均使用參數化查詢（@Search、@param），無 SQL Injection 風險
- EF Core 使用 AsNoTracking() 於查詢路徑
- RouteTable 使用 HandlerFactory 延遲解析 Controller，避免 Singleton 持有 Scoped（Captive Dependency）
- EF Core Retry（maxRetryCount: 3, maxRetryDelay: 5s）
- Program.cs 中 DI 生命週期正確（Service/Controller 均 Scoped）

## 已知缺陷
- `PermissionMiddleware` 未實作：CLAUDE.md 架構中規定有 limid 粒度的權限檢查，但 Middleware 目錄中不存在
- 所有模組（User、Group、Host）目前僅有 JWT 驗證，無功能級別權限管控
- `ParseInt` 回傳 0 時與 id <= 0 守衛重複但可接受
- HostController 的 NotFound 訊息使用英文（"Host '{id}' not found."），與 UserController 一致（設計選擇，但中英混用）
- `HostService` 宣告了 `TaipeiTz` 靜態欄位但從未使用（沒有 updatetime/createtime 欄位）
