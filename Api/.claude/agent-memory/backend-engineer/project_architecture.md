---
name: QuotationApi Architecture
description: Azure Functions v4 isolated (.NET 9) 報價系統後端架構，包含路由設計、auth 機制、資料庫結構
type: project
---

# QuotationApi 架構摘要

## 專案位置
`D:\websystems\quotation.weypro.com\Api\`

## 技術棧
- Azure Functions v4 Isolated Worker, .NET 9
- EF Core 9 + Dapper (SQL Server)
- System.Text.Json (camelCase)
- JWT: System.IdentityModel.Tokens.Jwt 8.x
- DB: `Server=(local);Database=quotation;User Id=sa;Password=twvsjp0205;TrustServerCertificate=true`

## 路由架構（Universal Router Pattern）
- 單一 Function `Api` 攔截所有 `api/{*route}` 請求
- host.json: `"routePrefix": ""` — 移除預設 api 前綴，路由從 /api/... 開始
- Middleware Pipeline: CorsMiddleware → ErrorHandlingMiddleware → JwtAuthMiddleware → RouteHandler
- RouteContext 在 middleware 與 controller 間共享請求狀態

## Auth 機制
- 管理員: admin@weypro.com.tw / B22H8Se1（hardcoded，不走 DB）
- 一般使用者: SHA1(password + "weypro168") hex lowercase
- JWT claims: sub (userid), email, name, groupid
- 公開路由（不需 JWT）: auth/login
- JwtSecret 來源: config["JwtSecret"] 或 config["Values:JwtSecret"]

## 權限模型
- lim 表: 功能定義 (limid, key, value)
- grouplim: 群組層級 CRUD 權限
- userlim: 個人層級 CRUD 權限（優先覆蓋群組）
- 權限合併邏輯在 AuthService.GetPermissionsAsync()

## 重要 Models（Guid 型 PK）
- User: Userid(Guid), Email, Password, Name, Groupid(Guid?), Status(bool?)
- Userlim: PK(Userid+Limid), Isquery/Isinsert/Isupdate/Isdelete
- Grouplim: PK(Groupid+Limid), 同上
- Lim: Limid(int), Key, Value, Parentid, Freq

## 已知坑
- IHeaderDictionary.Append() 只接受 StringValues，應改用索引器 `response.Headers["key"] = "value"`
- RouteTable / RouteHandler 必須是 scoped（因依賴 scoped controller）
- MiddlewarePipeline 以工廠方式在 AddScoped 內組裝，確保 middleware 實例是同一 scope

**Why:** 首次建立整個架構時遇到上述問題並已修正
**How to apply:** 新增 middleware 或 response header 操作時注意上述兩點
