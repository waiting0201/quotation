# 報價系統 (Quotation System) — quotation.weypro.com

## 專案概述

重建威庭科技報價管理系統，從 ASP.NET MVC 5 遷移至前後端分離架構。
沿用現有 SQL Server 資料庫 `quotation`（本機）。
參考原始碼位於 `D:\websystems\quotation`。
沿用原始碼中的 favicon 和 logo 等靜態資源。

## 開發流程

**所有功能開發遵循以下順序：**

1. **UI 設計** — 使用 `frontend-design` skill（遇到任何設計需求時優先呼叫）
2. **後端 API** — 使用 `backend-engineer` agent
3. **前端實作** — 使用 `frontend-architect` agent
4. **品質檢查** — 使用 `qa-test-engineer` agent
5. **程式碼審查** — 使用 `code-review-optimizer` agent

> **重要：** 遇到任何 UI/UX 設計決策，必須先使用 `/frontend-design` skill 完成設計，再進行實作。

## 本機開發環境

### Azurite（Azure Storage 模擬器）

Azure Functions 執行時需要 Azure Storage 連線（即使只用 HTTP Trigger），本機開發使用 Azurite 模擬。

**安裝（一次性）：**
```bash
npm install -g azurite
```

**每次開發前啟動：**
```bash
azurite --silent --location .azurite
```
或在 VSCode 按 `Ctrl+Shift+P` → `Azurite: Start`。

**`local.settings.json` 設定：**
```json
"AzureWebJobsStorage": "UseDevelopmentStorage=true"
```

> 若未啟動 Azurite，Azure Functions 會報 `Unhealthy: Unable to access AzureWebJobsStorage`。
> 設為 `""` 或 `"none"` 在新版 Core Tools 中無效，必須實際啟動 Azurite。

## 技術棧

### 前端

- **Framework:** Angular 21（企業級架構）
- **Language:** TypeScript (strict mode)
- **CSS:** SCSS + Tailwind CSS（取代 Angular Material）
- **State Management:** Angular Signals + Store Pattern
- **HTTP:** Angular HttpClient
- **路由:** Angular Router (lazy loading)
- **表單:** Reactive Forms + 驗證
- **語系:** 純中文（不需要英文欄位）
- **時區:** Asia/Taipei (UTC+8)，所有日期時間以台北時間處理

### 後端

- **Runtime:** Azure Functions v4 (isolated worker, .NET 9)
- **Language:** C#
- **ORM:** Dapper（複雜查詢）+ EF Core 9（簡單 CRUD，Database First）
- **路由模式:** 萬用 Router（單一 catch-all HTTP trigger）
- **認證:** JWT Token（自行簽發，替代原本 Forms Authentication）
- **授權:** 基於現有 `lim` / `userlim` / `grouplim` 資料表的權限系統
- **時區:** 所有日期時間統一使用 Asia/Taipei (UTC+8)
- **PDF 產生:** QuestPDF（後端產生 PDF，前端下載）

### 資料庫

- **Engine:** SQL Server（本機）
- **Database:** `quotation`
- **連線:** 沿用現有 schema，原則上不修改資料表結構
- **例外（經同意的 schema 變更）:** `items.discount`（int NULL DEFAULT 0，報價單折扣百分比 0-100），migration 見 `docs/migrations/`

## Angular 企業級架構

### 分層架構

```
┌─────────────────────────────────────────────┐
│              Presentation Layer              │
│  (Smart Components / Pages / Routed Views)   │
├─────────────────────────────────────────────┤
│               Facade Layer                   │
│  (Feature Facades — 協調 State 與 API 層)     │
├─────────────────────────────────────────────┤
│                State Layer                   │
│  (Signal Stores — 管理狀態與衍生資料)          │
├─────────────────────────────────────────────┤
│                 API Layer                    │
│  (HttpClient Services — 純 HTTP 呼叫)         │
└─────────────────────────────────────────────┘
```

### 設計模式

- **Smart / Dumb Components（容器/展示元件）**
  - Smart：注入 Facade，處理業務邏輯與路由
  - Dumb：純 @Input/@Output，無依賴注入，可重用
- **Facade Pattern：** 每個 feature 有一個 Facade service，統一對外介面
- **Signal Store：** 使用 Angular Signals 實作響應式狀態管理
- **Standalone Components：** Angular 21 預設，不使用 NgModule

### 目錄結構

```
frontend/quotation-app/src/app/
├── core/                              # 核心層（全應用單例）
│   ├── auth/
│   │   ├── auth.service.ts            # JWT 登入/登出/token 管理
│   │   ├── auth.guard.ts              # 路由守衛
│   │   ├── auth.interceptor.ts        # HTTP 攔截器（注入 JWT）
│   │   └── auth.model.ts              # 認證相關型別
│   ├── interceptors/
│   │   ├── error.interceptor.ts       # 全域錯誤處理
│   │   └── loading.interceptor.ts     # 全域 loading 狀態
│   ├── services/
│   │   ├── notification.service.ts    # 通知/toast 服務
│   │   └── permission.service.ts      # 權限檢查服務
│   ├── models/                        # 全域共用型別/介面
│   │   ├── api-response.model.ts      # API 回應格式
│   │   ├── pagination.model.ts        # 分頁參數
│   │   └── enums.ts                   # 列舉定義
│   └── guards/
│       └── permission.guard.ts        # 權限守衛
│
├── shared/                            # 共用層（可重用元件/指令/管線）
│   ├── components/
│   │   ├── data-table/                # 通用資料表格
│   │   ├── confirm-dialog/            # 確認對話框
│   │   ├── page-header/               # 頁面標題列
│   │   ├── status-badge/              # 狀態標籤
│   │   └── file-upload/               # 檔案上傳元件
│   ├── pipes/
│   │   ├── date-format.pipe.ts        # 日期格式化
│   │   └── currency-format.pipe.ts    # 金額格式化
│   └── directives/
│       └── permission.directive.ts    # *appPermission 權限指令
│
├── features/                          # 功能模組（lazy loaded）
│   ├── auth/                          # 登入
│   │   ├── pages/
│   │   │   └── login/
│   │   │       ├── login.component.ts
│   │   │       └── login.component.html
│   │   └── auth.routes.ts
│   │
│   ├── dashboard/                     # 主頁/行事曆
│   │   ├── pages/
│   │   │   └── dashboard/
│   │   ├── components/
│   │   │   └── calendar/
│   │   ├── facades/
│   │   │   └── dashboard.facade.ts
│   │   ├── stores/
│   │   │   └── dashboard.store.ts
│   │   ├── services/
│   │   │   └── dashboard-api.service.ts
│   │   └── dashboard.routes.ts
│   │
│   ├── quotation/                     # 報價管理
│   │   ├── pages/
│   │   │   ├── quotation-list/
│   │   │   ├── quotation-create/
│   │   │   ├── quotation-detail/
│   │   │   └── quotation-update/
│   │   ├── components/                # Dumb components
│   │   │   ├── quotation-form/
│   │   │   ├── item-detail-row/
│   │   │   └── item-content-row/
│   │   ├── facades/
│   │   │   └── quotation.facade.ts
│   │   ├── stores/
│   │   │   └── quotation.store.ts
│   │   ├── services/
│   │   │   └── quotation-api.service.ts
│   │   ├── models/
│   │   │   └── quotation.model.ts
│   │   └── quotation.routes.ts
│   │
│   ├── customer/                      # 客戶管理
│   │   ├── pages/
│   │   │   ├── customer-list/
│   │   │   ├── customer-create/
│   │   │   ├── customer-detail/
│   │   │   ├── customer-update/
│   │   │   ├── customer-type-list/
│   │   │   ├── customer-type-create/
│   │   │   └── customer-type-update/
│   │   ├── components/
│   │   │   ├── customer-form/
│   │   │   └── contact-row/
│   │   ├── facades/
│   │   │   └── customer.facade.ts
│   │   ├── stores/
│   │   │   └── customer.store.ts
│   │   ├── services/
│   │   │   └── customer-api.service.ts
│   │   ├── models/
│   │   │   └── customer.model.ts
│   │   └── customer.routes.ts
│   │
│   ├── invoice/                       # 發票管理
│   │   ├── pages/
│   │   │   ├── invoice-list/
│   │   │   ├── invoice-create/
│   │   │   ├── invoice-detail/
│   │   │   └── invoice-update/
│   │   ├── components/
│   │   │   ├── invoice-form/
│   │   │   └── invoice-detail-row/
│   │   ├── facades/
│   │   │   └── invoice.facade.ts
│   │   ├── stores/
│   │   │   └── invoice.store.ts
│   │   ├── services/
│   │   │   └── invoice-api.service.ts
│   │   ├── models/
│   │   │   └── invoice.model.ts
│   │   └── invoice.routes.ts
│   │
│   ├── income/                        # 收款管理
│   │   ├── pages/
│   │   │   ├── income-list/
│   │   │   └── income-create/
│   │   ├── facades/
│   │   │   └── income.facade.ts
│   │   ├── stores/
│   │   │   └── income.store.ts
│   │   ├── services/
│   │   │   └── income-api.service.ts
│   │   ├── models/
│   │   │   └── income.model.ts
│   │   └── income.routes.ts
│   │
│   └── settings/                      # 系統設定
│       ├── pages/
│       │   ├── user-list/
│       │   ├── user-create/
│       │   ├── user-password-update/
│       │   ├── group-list/
│       │   ├── group-create/
│       │   └── group-update/
│       ├── components/
│       │   ├── user-form/
│       │   ├── group-form/
│       │   └── permission-matrix/
│       ├── facades/
│       │   └── settings.facade.ts
│       ├── stores/
│       │   └── settings.store.ts
│       ├── services/
│       │   └── settings-api.service.ts
│       ├── models/
│       │   └── settings.model.ts
│       └── settings.routes.ts
│
├── app.component.ts
├── app.config.ts
├── app.routes.ts
└── layout/
    ├── main-layout/
    │   ├── main-layout.component.ts
    │   └── main-layout.component.html
    ├── sidebar/
    │   └── sidebar.component.ts
    └── header/
        └── header.component.ts
```

## Azure Functions 萬用 Router 架構

### 概念

使用單一 catch-all HTTP trigger 接收所有 `/api/{*route}` 請求，內部透過路由表分派到對應的 Controller，統一處理中介層邏輯。

### 目錄結構

```
api/QuotationApi/
├── Program.cs                         # DI 容器設定
├── host.json                          # Azure Functions 設定
├── local.settings.json                # 本機開發設定
│
├── Functions/
│   └── ApiFunction.cs                 # 唯一的 catch-all HTTP trigger
│
├── Router/
│   ├── RouteTable.cs                  # 路由定義表
│   ├── RouteHandler.cs                # 路由解析與分派
│   └── HttpContextWrapper.cs          # 請求/回應封裝
│
├── Middleware/
│   ├── IMiddleware.cs                 # 中介層介面
│   ├── MiddlewarePipeline.cs          # 中介層管線
│   ├── JwtAuthMiddleware.cs           # JWT 驗證
│   ├── PermissionMiddleware.cs        # 權限檢查
│   ├── ErrorHandlingMiddleware.cs     # 全域錯誤處理
│   └── CorsMiddleware.cs             # CORS 處理
│
├── Controllers/
│   ├── BaseController.cs             # 基底控制器
│   ├── AuthController.cs             # 登入、JWT 簽發
│   ├── QuotationController.cs        # 報價 CRUD
│   ├── CustomerController.cs         # 客戶 CRUD
│   ├── InvoiceController.cs          # 發票 CRUD
│   ├── IncomeController.cs           # 收款 CRUD
│   ├── SettingController.cs          # 使用者/群組管理
│   └── LookupController.cs           # 下拉選單（國家、付款方式等）
│
├── Services/
│   ├── AuthService.cs                 # 認證邏輯、JWT 產生
│   ├── QuotationService.cs
│   ├── CustomerService.cs
│   ├── InvoiceService.cs
│   ├── IncomeService.cs
│   ├── UserService.cs
│   ├── GroupService.cs
│   ├── PermissionService.cs           # 權限查詢
│   ├── CodeGeneratorService.cs        # 自動編碼產生
│   ├── TaxCalculatorService.cs        # 稅務計算
│   ├── QuotationPdfService.cs         # 報價單 PDF 產生（QuestPDF）
│   └── InvoicePdfService.cs           # 發票 PDF 產生（QuestPDF）
│
├── Models/                            # EF Core Entity Models（scaffold 產生）
│   ├── QuotationDbContext.cs
│   ├── User.cs
│   ├── Customer.cs
│   ├── Item.cs
│   ├── ItemDetail.cs
│   ├── ItemContent.cs
│   ├── Invoice.cs
│   ├── InvoiceDetail.cs
│   ├── Income.cs
│   ├── Group.cs
│   ├── Lim.cs
│   ├── UserLim.cs
│   ├── GroupLim.cs
│   ├── CustomerDetail.cs
│   ├── CustomerType.cs
│   ├── Country.cs
│   ├── Payment.cs
│   ├── AboutUs.cs
│   ├── Host.cs
│   └── Project.cs
│
├── DTOs/
│   ├── Auth/
│   │   ├── LoginRequest.cs
│   │   └── LoginResponse.cs
│   ├── Quotation/
│   │   ├── QuotationListDto.cs
│   │   ├── QuotationDetailDto.cs
│   │   └── QuotationCreateUpdateDto.cs
│   ├── Customer/
│   │   ├── CustomerListDto.cs
│   │   ├── CustomerDetailDto.cs
│   │   └── CustomerCreateUpdateDto.cs
│   ├── Invoice/
│   │   ├── InvoiceListDto.cs
│   │   ├── InvoiceDetailDto.cs
│   │   └── InvoiceCreateUpdateDto.cs
│   ├── Income/
│   │   └── IncomeDto.cs
│   ├── Settings/
│   │   ├── UserDto.cs
│   │   ├── GroupDto.cs
│   │   └── PermissionDto.cs
│   ├── Lookup/
│   │   └── LookupDto.cs
│   └── Common/
│       ├── ApiResponse.cs             # 統一回應格式
│       └── PaginationRequest.cs       # 分頁請求
│
├── Repositories/                      # Dapper 查詢層
│   ├── IQuotationRepository.cs
│   ├── QuotationRepository.cs
│   ├── ICustomerRepository.cs
│   ├── CustomerRepository.cs
│   ├── IInvoiceRepository.cs
│   ├── InvoiceRepository.cs
│   ├── IIncomeRepository.cs
│   ├── IncomeRepository.cs
│   ├── ILookupRepository.cs
│   └── LookupRepository.cs
│
└── Helpers/
    ├── PasswordHelper.cs              # SHA1 雜湊（salt: weypro168）
    └── MappingHelper.cs               # Entity ↔ DTO 映射
```

### 萬用 Router 運作方式

```
HTTP Request → ApiFunction (catch-all trigger)
    → MiddlewarePipeline
        → CorsMiddleware
        → ErrorHandlingMiddleware
        → JwtAuthMiddleware (解析 token，略過 /auth/login)
        → PermissionMiddleware (檢查權限，略過公開端點)
    → RouteHandler (比對 RouteTable，分派到 Controller method)
    → Controller → Service → EF Core / Dapper → SQL Server
    → Response
```

### ApiFunction.cs 範例

```csharp
public class ApiFunction
{
    private readonly MiddlewarePipeline _pipeline;
    private readonly RouteHandler _router;

    [Function("Api")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous,
            "get", "post", "put", "delete",
            Route = "api/{*route}")] HttpRequestData req,
        string route)
    {
        var context = new HttpContextWrapper(req, route);
        await _pipeline.ExecuteAsync(context);
        if (context.IsHandled) return context.Response;
        return await _router.HandleAsync(context);
    }
}
```

## API 設計

### RESTful 端點

```
POST   /api/auth/login                 # 登入，回傳 JWT
GET    /api/auth/me                    # 取得當前使用者資訊

GET    /api/quotations                 # 報價單列表（分頁、搜尋）
POST   /api/quotations                 # 新增報價單
GET    /api/quotations/{id}            # 報價單詳情
PUT    /api/quotations/{id}            # 更新報價單
DELETE /api/quotations/{id}            # 刪除報價單
GET    /api/quotations/{id}/pdf        # 報價單 PDF 匯出（QuestPDF）

GET    /api/customers                  # 客戶列表
POST   /api/customers                  # 新增客戶
GET    /api/customers/{id}             # 客戶詳情
PUT    /api/customers/{id}             # 更新客戶
DELETE /api/customers/{id}             # 刪除客戶
POST   /api/customers/{id}/logo        # 上傳 Logo

GET    /api/customer-types             # 客戶類型列表
POST   /api/customer-types             # 新增客戶類型
PUT    /api/customer-types/{id}        # 更新客戶類型
DELETE /api/customer-types/{id}        # 刪除客戶類型

GET    /api/invoices                   # 發票列表
POST   /api/invoices                   # 新增發票
GET    /api/invoices/{id}              # 發票詳情
PUT    /api/invoices/{id}              # 更新發票
DELETE /api/invoices/{id}              # 刪除發票
GET    /api/invoices/{id}/pdf          # 發票 PDF 匯出（QuestPDF）

GET    /api/incomes                    # 收款列表
POST   /api/incomes                    # 新增收款
DELETE /api/incomes/{id}               # 刪除收款

GET    /api/users                      # 使用者列表
POST   /api/users                      # 新增使用者
PUT    /api/users/{id}/password        # 更新密碼
DELETE /api/users/{id}                 # 刪除使用者

GET    /api/groups                     # 群組列表
POST   /api/groups                     # 新增群組
PUT    /api/groups/{id}                # 更新群組
DELETE /api/groups/{id}                # 刪除群組

GET    /api/lookups/countries          # 國家列表
GET    /api/lookups/payments           # 付款方式列表
GET    /api/lookups/aboutus            # 公司資訊
GET    /api/lookups/permissions        # 權限樹
```

### API 回應格式

```json
// 成功（單筆）
{ "data": { ... } }

// 成功（列表，含分頁）
{
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5
  }
}

// 錯誤
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "欄位驗證失敗",
    "details": [ ... ]
  }
}
```

## 業務邏輯

### 自動編碼產生

格式：`{PREFIX}{YYYYMMDD}{NNN}`，流水號每日重置從 001 開始。

| 類型 | 前綴 | 範例 |
|------|------|------|
| 報價單 | QUO | QUO20260313001 |
| 發票 | INV | INV20260313001 |
| 客戶 | CUS | CUS20260313001 |
| 專案 | PRO | PRO20260313001 |
| 收款 | INC | INC20260313001 |

### 稅務計算

| 稅別 | 值 | 計算方式 |
|------|------|----------|
| 稅外加 | 0 | total * 1.05 |
| 稅內含 | 1 | total / 1.05（反推未稅） |
| 免稅金 | 2 | total（不加稅） |

**折扣（報價單層級）：** `items.discount` 存 0-100 整數百分比。計算順序：未稅小計 → 扣除 `round(小計 × discount / 100)` → 折後小計再依稅別計稅；`items.tax` / `items.total` 存的是折後值。

### 狀態流程

**報價單：** 已報價(0) → 已簽約(1) → 已結案(2) → 已取消(3)
**發票：** 已開(0) → 已寄出(1) → 已入帳(2) → 作廢(3)
**發票類型：** 二聯(0) / 三聯(1)

### 認證

- 密碼雜湊：SHA1 + salt `weypro168`（相容現有資料）
- JWT Token 簽發與驗證
- Admin 帳號 `admin@weypro.com.tw` 擁有所有權限
- 權限粒度：isquery / isinsert / isupdate / isdelete

## 資料庫 Schema

沿用現有資料表，原則上不修改結構。例外：`items.discount`（2026-07 新增，折扣百分比），migration 存放於 `docs/migrations/`。

### 核心業務資料表

| 資料表 | 用途 | 主鍵 | 型別 |
|--------|------|------|------|
| `items` | 報價單 | `itemid` | uniqueidentifier |
| `itemdetails` | 報價明細 | `itemdetailid` | uniqueidentifier |
| `itemcontents` | 報價內容 | `itemcontentid` | uniqueidentifier |
| `customers` | 客戶 | `customerid` | int (identity) |
| `customerdetails` | 聯絡人 | `customerdetailid` | uniqueidentifier |
| `customertypes` | 客戶類型 | `customertypeid` | int (identity) |
| `invoices` | 發票 | `invoiceid` | uniqueidentifier |
| `invoicedetails` | 發票明細 | `invoicedetailid` | uniqueidentifier |
| `incomes` | 收款 | `incomeid` | uniqueidentifier |
| `projects` | 專案 | `projectid` | uniqueidentifier |

### 權限資料表

| 資料表 | 用途 | 主鍵 |
|--------|------|------|
| `user` | 使用者 | `userid` (uniqueidentifier) |
| `group` | 群組 | `groupid` (uniqueidentifier) |
| `lim` | 權限定義（樹狀） | `limid` (int, identity) |
| `userlim` | 使用者權限 | `userid + limid` (複合鍵) |
| `grouplim` | 群組權限 | `groupid + limid` (複合鍵) |

### 查找資料表

| 資料表 | 用途 | 主鍵 |
|--------|------|------|
| `aboutus` | 公司資訊 | `id` (int) |
| `country` | 國家 | `countryid` (int, identity) |
| `payments` | 付款方式 | `paymentid` (int, identity) |
| `hosts` | 主機管理 | `hostid` (int) |

## AI 驅動架構

### 設計理念

在既有的填表式 CRUD 系統之上，**額外增加** AI 對話式功能作為輔助。使用者可自由選擇：

- **傳統模式**：透過表單手動填寫，精確控制每個欄位（保留不變）
- **AI 輔助模式**：透過自然語言與 AI 助手互動，AI 在背後呼叫系統工具（Tool Use）完成資料操作，加速重複性工作

兩種模式共存互補，AI 不取代表單，而是提供另一條更快的操作路徑。

### 核心 AI 功能

| 功能 | 說明 | 使用場景 |
|------|------|----------|
| **AI 報價助手** | 自然語言建立/修改報價單 | 「幫我對 ABC 公司報一台伺服器，含安裝服務，總價 15 萬」 |
| **智能定價建議** | 根據歷史報價分析，推薦合理價格 | 新增報價項目時自動帶出建議單價與歷史區間 |
| **客戶洞察摘要** | AI 分析客戶交易紀錄，產生摘要 | 進入客戶頁面時顯示「近半年報價 5 筆、成交率 60%、偏好季末下單」 |
| **語意搜尋** | 跨報價/客戶/發票的自然語言搜尋 | 「上個月給科技業客戶的報價有哪些？」 |
| **文件解析** | 上傳 RFQ/採購單，AI 自動解析並預填報價表單 | 拖入客戶 PDF 需求文件，AI 自動擷取品項、數量、規格 |
| **報價信件產生** | AI 產生客製化報價郵件內容 | 選擇報價單後一鍵產生中/英文報價信 |

### 架構概覽

```
┌──────────────────────────────────────────────────────────┐
│                    Frontend (Angular)                     │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │  AI Chat    │  │  Smart Form  │  │  Insight Card  │  │
│  │  Panel      │  │  (AI 預填)    │  │  (AI 摘要)     │  │
│  └──────┬──────┘  └──────┬───────┘  └───────┬────────┘  │
│         │                │                   │           │
│  ┌──────┴────────────────┴───────────────────┴────────┐  │
│  │              AI Service Layer (Frontend)            │  │
│  │  ai-chat.service.ts / ai-api.service.ts            │  │
│  │  SSE streaming / conversation state                │  │
│  └────────────────────────┬───────────────────────────┘  │
└───────────────────────────┼──────────────────────────────┘
                            │ HTTP / SSE
┌───────────────────────────┼──────────────────────────────┐
│                    Backend (Azure Functions)              │
│  ┌────────────────────────┴───────────────────────────┐  │
│  │              AiController.cs                        │  │
│  │  POST /api/ai/chat        — 對話（SSE 串流回應）     │  │
│  │  POST /api/ai/parse-doc   — 文件解析                │  │
│  │  GET  /api/ai/insights/{type}/{id} — 洞察摘要       │  │
│  │  POST /api/ai/suggest-price — 定價建議              │  │
│  └────────────────────────┬───────────────────────────┘  │
│  ┌────────────────────────┴───────────────────────────┐  │
│  │              AiOrchestratorService.cs               │  │
│  │  — 管理對話上下文                                    │  │
│  │  — 呼叫 Claude API (Anthropic .NET SDK)             │  │
│  │  — 處理 Tool Use 迴圈（Claude ↔ 系統工具）           │  │
│  │  — 串流回應 (SSE)                                   │  │
│  └────────────────────────┬───────────────────────────┘  │
│  ┌────────────────────────┴───────────────────────────┐  │
│  │              AI Tool Definitions                    │  │
│  │  系統工具（Claude Tool Use 格式定義）：               │  │
│  │                                                     │  │
│  │  search_quotations  — 搜尋報價單                     │  │
│  │  get_quotation      — 取得報價單詳情                  │  │
│  │  create_quotation   — 建立報價單                     │  │
│  │  update_quotation   — 更新報價單                     │  │
│  │  search_customers   — 搜尋客戶                      │  │
│  │  get_customer       — 取得客戶詳情                   │  │
│  │  create_customer    — 建立客戶                      │  │
│  │  search_invoices    — 搜尋發票                      │  │
│  │  get_invoice        — 取得發票詳情                   │  │
│  │  create_invoice     — 建立發票                      │  │
│  │  get_price_history  — 查詢歷史報價價格               │  │
│  │  get_customer_stats — 查詢客戶統計資料               │  │
│  │  get_lookups        — 查詢下拉選單資料               │  │
│  │  calculate_tax      — 計算稅額                      │  │
│  │  generate_code      — 產生流水編號                   │  │
│  └────────────────────────┬───────────────────────────┘  │
│                           │                              │
│              既有 Service / Repository 層                 │
│              (QuotationService, CustomerService...)       │
└──────────────────────────────────────────────────────────┘
```

### AI Tool Use 運作流程

```
使用者輸入：「幫我對 ABC 公司報一台 Dell R750 伺服器，含 3 年保固」

1. Frontend → POST /api/ai/chat (SSE)
   { "message": "幫我對 ABC 公司報一台 Dell R750 伺服器，含 3 年保固", "conversationId": "..." }

2. AiOrchestratorService → Claude API (with tools)
   System Prompt + User Message + Tool Definitions

3. Claude 回應 tool_use: search_customers({ query: "ABC" })
   → 執行 CustomerService.Search("ABC")
   → 回傳結果給 Claude

4. Claude 回應 tool_use: get_price_history({ keyword: "Dell R750" })
   → 查詢歷史報價
   → 回傳結果給 Claude

5. Claude 回應 tool_use: create_quotation({
     customerId: 42,
     items: [
       { name: "Dell R750 伺服器", qty: 1, price: 185000 },
       { name: "3 年保固服務", qty: 1, price: 28000 }
     ],
     taxType: 0
   })
   → 執行 QuotationService.Create(...)
   → 回傳新建報價單資料

6. Claude → 串流文字回應（SSE）：
   「已為 ABC 科技有限公司建立報價單 QUO20260314001，
     含 Dell R750 伺服器 $185,000 及 3 年保固 $28,000，
     未稅合計 $213,000，稅外加後 $223,650。
     需要我調整價格或新增其他項目嗎？」

7. Frontend 即時顯示串流文字 + 更新 UI 狀態
```

### 後端目錄結構（AI 擴充）

```
api/QuotationApi/
├── Controllers/
│   └── AiController.cs                # AI 相關端點
│
├── Services/
│   ├── AI/
│   │   ├── AiOrchestratorService.cs   # 核心：對話管理 + Tool Use 迴圈
│   │   ├── AiToolRegistry.cs          # 工具定義註冊表
│   │   ├── AiToolExecutor.cs          # 工具執行器（分派到既有 Service）
│   │   ├── AiPromptBuilder.cs         # System Prompt 組裝（含業務規則）
│   │   ├── AiConversationStore.cs     # 對話歷史管理（記憶體/Redis）
│   │   ├── DocumentParserService.cs   # 文件解析（PDF/圖片 → 結構化資料）
│   │   └── InsightService.cs          # 洞察摘要產生
│   │
│   └── ... (既有 services)
│
├── DTOs/
│   └── AI/
│       ├── AiChatRequest.cs           # { message, conversationId }
│       ├── AiChatStreamEvent.cs       # SSE 事件格式
│       ├── AiToolCall.cs              # Tool Use 請求/回應
│       ├── AiInsightDto.cs            # 洞察摘要
│       ├── AiPriceSuggestionDto.cs    # 定價建議
│       └── DocumentParseResult.cs     # 文件解析結果
│
├── Tools/                             # AI 工具定義（每個工具一個類別）
│   ├── IAiTool.cs                     # 工具介面
│   ├── SearchQuotationsTool.cs
│   ├── GetQuotationTool.cs
│   ├── CreateQuotationTool.cs
│   ├── UpdateQuotationTool.cs
│   ├── SearchCustomersTool.cs
│   ├── GetCustomerTool.cs
│   ├── CreateCustomerTool.cs
│   ├── SearchInvoicesTool.cs
│   ├── GetInvoiceTool.cs
│   ├── CreateInvoiceTool.cs
│   ├── GetPriceHistoryTool.cs
│   ├── GetCustomerStatsTool.cs
│   ├── GetLookupsTool.cs
│   ├── CalculateTaxTool.cs
│   └── GenerateCodeTool.cs
```

### 前端目錄結構（AI 擴充）

```
Admin/src/app/
├── core/
│   └── services/
│       └── ai.service.ts              # AI API 呼叫 + SSE 串流處理
│
├── shared/
│   └── components/
│       ├── ai-chat-panel/             # 全域 AI 對話面板（側邊滑入）
│       │   ├── ai-chat-panel.component.ts
│       │   ├── ai-chat-panel.component.html
│       │   └── ai-chat-panel.component.scss
│       ├── ai-message/                # 單則訊息元件（支援 Markdown 渲染）
│       │   └── ai-message.component.ts
│       ├── ai-insight-card/           # 洞察摘要卡片
│       │   └── ai-insight-card.component.ts
│       └── ai-price-badge/            # 定價建議標籤
│           └── ai-price-badge.component.ts
│
├── features/
│   ├── quotation/
│   │   └── components/
│   │       └── ai-quotation-builder/  # AI 報價建立器（對話式介面）
│   │
│   ├── customer/
│   │   └── components/
│   │       └── customer-insight/      # 客戶 AI 洞察面板
│   │
│   └── dashboard/
│       └── components/
│           └── ai-summary-widget/     # 儀表板 AI 摘要小工具
```

### AI API 端點

```
POST   /api/ai/chat                    # AI 對話（SSE 串流回應）
POST   /api/ai/parse-document          # 上傳文件，AI 解析為結構化資料
GET    /api/ai/insights/customer/{id}  # 客戶洞察摘要
GET    /api/ai/insights/quotation/{id} # 報價單分析
POST   /api/ai/suggest-price           # 智能定價建議
DELETE /api/ai/conversations/{id}      # 清除對話歷史
```

### AI SSE 串流回應格式

```
// 串流事件類型
event: message          // AI 文字回應（逐字串流）
data: {"type": "text", "content": "已為您找到"}

event: tool_start       // 開始執行工具（前端可顯示 loading）
data: {"type": "tool_start", "tool": "search_customers", "input": {"query": "ABC"}}

event: tool_result      // 工具執行結果（前端可更新 UI）
data: {"type": "tool_result", "tool": "search_customers", "result": {...}}

event: action           // AI 完成了一個資料操作（前端刷新相關列表）
data: {"type": "action", "action": "quotation_created", "id": "xxx", "code": "QUO20260314001"}

event: done             // 串流結束
data: {"type": "done", "conversationId": "..."}

event: error            // 錯誤
data: {"type": "error", "message": "..."}
```

### AI System Prompt 策略

```
AiPromptBuilder 組裝 System Prompt：

1. 角色定義
   「你是威庭科技報價管理系統的 AI 助手，協助使用者管理報價、客戶、發票。」

2. 業務規則注入
   — 編碼規則（QUO/INV/CUS 前綴 + 日期 + 流水號）
   — 稅務計算規則（外加/內含/免稅）
   — 狀態流程定義
   — 當前使用者資訊與權限

3. 行為準則
   — 建立/修改/刪除前必須確認
   — 金額異常時提醒（偏離歷史均價 >30%）
   — 回應使用繁體中文
   — 涉及刪除操作需二次確認

4. 上下文注入
   — 當前頁面位置（報價列表/客戶詳情等）
   — 已選取的資料（如正在檢視的報價單）
```

### 技術選型（AI 層）

| 項目 | 選擇 | 說明 |
|------|------|------|
| **LLM** | Claude API (claude-sonnet-4-6) | 成本效益最佳，支援 Tool Use |
| **SDK** | Anthropic .NET SDK | 官方 C# SDK，支援串流 + Tool Use |
| **串流** | Server-Sent Events (SSE) | 單向串流，比 WebSocket 簡單、HTTP 相容 |
| **對話儲存** | 記憶體 (Dictionary) → 可擴充 Redis | 開發期用記憶體，上線後切 Redis |
| **文件解析** | Claude Vision (多模態) | 直接用 Claude 解析 PDF/圖片，免外部 OCR |
| **前端串流** | EventSource API + RxJS | Angular 原生整合，可組合 Observable |
| **Markdown 渲染** | ngx-markdown | AI 回應支援格式化輸出 |

### 安全與權限

- AI 操作遵循既有權限系統：AI 工具執行前檢查使用者的 `userlim`/`grouplim`
- 寫入操作（create/update/delete）需在 AI 回應中先確認，使用者同意後才執行
- AI 對話紀錄綁定使用者 ID，不跨使用者共享
- 敏感操作（刪除、大金額報價）需額外確認機制
- API Key 存放於 `local.settings.json` / Azure App Settings，不進版控

### 開發階段

| 階段 | 功能 | 優先級 |
|------|------|--------|
| **Phase 1** | AI 對話核心（Tool Use 迴圈 + SSE 串流） | 高 |
| **Phase 2** | 報價助手（建立/修改報價、定價建議） | 高 |
| **Phase 3** | 語意搜尋（跨模組自然語言搜尋） | 中 |
| **Phase 4** | 客戶洞察（交易摘要、趨勢分析） | 中 |
| **Phase 5** | 文件解析（RFQ/採購單自動擷取） | 低 |
| **Phase 6** | 報價信件產生（中/英文客製郵件） | 低 |

## DI 生命週期規則（重要）

以下規則攸關效能，違反會導致每次 HTTP 請求產生大量不必要的物件建立。

### Singleton（應用程式啟動時建立一次）

| 類別 | 原因 |
|------|------|
| `RouteTable` | 路由定義不會改變，Controller 透過 `HandlerFactory` 延遲解析 |
| `RouteHandler` | 持有預編譯的 Regex，只需編譯一次 |
| `MiddlewarePipeline` | 管線組成不會改變 |
| `CorsMiddleware` | 無狀態，thread-safe |
| `ErrorHandlingMiddleware` | 僅依賴 ILogger（thread-safe） |
| `JwtAuthMiddleware` | 僅依賴 JwtHelper（Singleton）+ ILogger |
| `JwtHelper` | 無狀態工具類別 |

### Scoped（每次 HTTP 請求建立一次）

| 類別 | 原因 |
|------|------|
| `*Controller` | 依賴 Scoped 的 Service |
| `*Service` | 依賴 Scoped 的 DbContext |
| `QuotationDbContext` | EF Core DbContext 必須是 Scoped（Change Tracker 非 thread-safe） |

### 常見錯誤（禁止）

- **❌ RouteTable / RouteHandler 改為 Scoped** — 會導致每次請求解析所有 Controller + Service，2 筆資料的查詢都會變慢
- **❌ Singleton 類別的建構子注入 Scoped 服務** — 會造成 Captive Dependency（被捕獲的依賴），Scoped 實例被 Singleton 永久持有，跨請求共用 DbContext 導致資料不一致
- **❌ 新增 Middleware 時在建構子注入 DbContext** — Middleware 是 Singleton，若需要 Scoped 服務應在 `InvokeAsync` 中透過 `context.Request.HttpContext.RequestServices.GetRequiredService<T>()` 取得

### 新增 Controller 時的步驟

1. 在 `Program.cs` 以 `AddScoped` 註冊 Controller 和 Service
2. 在 `RouteTable.RegisterRoutes()` 中用 `Register<TController>(...)` 註冊路由
3. **不需要**修改 RouteTable 的建構子 — Controller 由 HandlerFactory 在請求時延遲解析

## 命名慣例

- **C# 後端：** PascalCase（類別、方法）、camelCase（參數、區域變數）
- **TypeScript 前端：** camelCase（變數、方法）、PascalCase（類別、介面）、kebab-case（檔案名稱）
- **API 端點：** kebab-case（`/api/customer-types`）
- **資料庫：** 沿用現有命名（全小寫）

## 部署架構

單一 repo（monorepo），前後端各自由根目錄的 GitHub Actions 部署。詳細 runbook 見 [`docs/deployment.md`](docs/deployment.md)。

### Repo 結構
- 前端 `Admin/`（Angular 21）與後端 `Api/`（Azure Functions, .NET 10）同一個 repo。
- GitHub repo：`waiting0201/quotation`（master 為預設分支）。原本 `Admin/` 是 submodule，已扁平化合併；舊的純前端歷史保留在 tag `frontend-history-backup`。

### CI/CD（`.github/workflows/`）
| Workflow | 觸發路徑 | 目標 |
|----------|----------|------|
| `frontend-swa.yml` | `Admin/**` | Azure Static Web Apps（`happy-pond-08d275d1e`） |
| `api-functions.yml` | `Api/**`、`global.json` | Azure Functions App `quotation-api`（Flex Consumption, .NET 10 isolated） |

兩支都用 `paths:` 過濾互不干擾，並都有 `workflow_dispatch` 可手動觸發。

### 認證
- 前端 SWA：用 deployment token（GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN_HAPPY_POND_08D275D1E`）。
- API：用 **OIDC**（federated credential，無長期憑證），GitHub secrets：`AZURE_CLIENT_ID`／`AZURE_TENANT_ID`／`AZURE_SUBSCRIPTION_ID`。App registration `quotation-github-actions` 對 `quotation-api` 有 Contributor。federated credential subject 綁 `repo:waiting0201/quotation:ref:refs/heads/master`（換分支或改用 environment 觸發時需另加 credential）。

### 機密分層（重要）
- **部署身分** → GitHub Secrets。
- **執行時設定**（DB 連線、JWT 金鑰）→ Azure Function App。`local.settings.json` 不會被部署。
  - `JwtSecret` 在 **App settings**；`DefaultConnection` 在 **Connection strings** 分頁（兩者是分開的）。
- Flex Consumption 的 runtime 存在 `functionAppConfig.runtime`，不是舊的 `linuxFxVersion`／`FUNCTIONS_WORKER_RUNTIME`（所以後者不存在是正常的）。

### 注意事項
- **force push 不相關歷史會跳過 `paths` 過濾的 workflow**（GitHub 無法算出變更檔案）。首次部署需用一個有共同祖先的正常 commit 觸發，或用 `workflow_dispatch` 手動執行。
