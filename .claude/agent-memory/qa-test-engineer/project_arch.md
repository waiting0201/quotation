---
name: project_arch
description: 報價系統重建專案架構概要與 QA 品質熱點
type: project
---

# 專案架構概要

**後端:** Azure Functions v4, .NET 9, 自製萬用 Router (RouteTable/RouteHandler), Dapper + EF Core 9
**前端:** Angular 21, Signals, Standalone Components, SCSS（無 Angular Material）
**資料庫:** SQL Server, 既有 schema 不變

## 品質熱點

### 後端
- `PermissionMiddleware` 目前**不存在**（Middleware 目錄中只有 JwtAuth、Cors、ErrorHandling）
  - 所有需要登入的端點（含 /api/hosts）都有 JWT 驗證，但沒有 per-route 的 limid 權限檢查
  - CLAUDE.md 架構文件中有 PermissionMiddleware，但實際程式碼未實作
- `RouteContext.IsHandled` 屬性：CLAUDE.md 範例程式碼中有用到，但 RouteContext.cs 實際上沒有這個屬性，功能改以 `context.Result != null` 判斷（由 ApiFunction 端）
- Host 端點沒有 limid 權限粒度控制，與其他模組（UserController 也缺乏）一致

### 前端
- 分頁實作為前端 client-side pagination（資料全部載入），無 server-side 分頁
- 到期狀態計算使用瀏覽器本地時間（new Date()），未使用 Asia/Taipei 時區
- `HostApiService.update()` 回傳型別為 `ApiResponse<unknown>`，但 HostListComponent 的 `_loadHosts()` 在 update 後重新載入，不依賴 update 回傳值

### 編碼慣例
- DTO 欄位命名：PascalCase（C# DTO），JSON 序列化為 camelCase（ConfigureHttpJsonOptions）
- 前端 interface 使用 camelCase，與後端 JSON 序列化輸出一致
- 錯誤回應統一使用 `ApiErrorResponse` wrapper
