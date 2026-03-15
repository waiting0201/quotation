---
name: project-architecture
description: quotation.weypro.com Admin 專案架構、目錄結構與核心開發慣例
type: project
---

## 技術棧
- Angular 21，standalone components (無 NgModule)
- SCSS + Tailwind CSS v4，@theme tokens 定義在 `src/styles.scss`
- Angular Signals 作為狀態管理
- Reactive Forms

## 根目錄
`D:\websystems\quotation.weypro.com\Admin\`

## 目錄結構
```
src/app/
  core/
    auth/          # auth.model.ts, auth.service.ts, auth.guard.ts, auth.interceptor.ts
    interceptors/  # error.interceptor.ts, loading.interceptor.ts
    services/      # notification.service.ts, permission.service.ts, loading.service.ts
    models/        # api-response.model.ts, enums.ts, pagination.model.ts
    guards/        # permission.guard.ts
  layout/
    main-layout/   # app shell：sidebar + header + breadcrumb + router-outlet
    header/        # 固定頂部，高度 56px (--header-height)
    sidebar/       # 固定左側，寬度 240px / 56px collapsed，設定在 sidebar.config.ts
    breadcrumb/    # ribbon 樣式，高度 36px (--ribbon-height)，從 route.data.breadcrumb 取值
  shared/
    components/    # panel, status-badge, confirm-dialog, page-header, notification-toast
    pipes/         # date-format (yyyy/MM/dd), currency-format (NT$ xxx)
    directives/    # permission.directive.ts (*appPermission)
  features/
    auth/          # login page + auth.routes.ts
    dashboard/     # dashboard.routes.ts
    quotation/     # quotation.routes.ts
    customer/      # customer.routes.ts
    invoice/       # invoice.routes.ts
    income/        # income.routes.ts
    settings/      # settings.routes.ts (子路由: users, groups)
  app.ts           # 根元件，含 NotificationToastComponent
  app.routes.ts    # 頂層路由，空路由用 MainLayoutComponent 包裝
  app.config.ts    # provideRouter + provideHttpClient (3 個 interceptors)
```

## 核心慣例
- 所有元件 `standalone: true`，使用 `inject()` 而非建構子注入
- 路由 lazy load 用 `loadChildren` (routes 檔) 或 `loadComponent`
- Feature routes 檔命名：`{feature}.routes.ts`，export 常數 `{FEATURE}_ROUTES`
- Breadcrumb 來源：`route.data['breadcrumb']`
- 權限守衛 data key：`permissionKey` + `permissionAction`
- Tailwind 自訂 token（已在 styles.scss 定義）：
  - 顏色：primary-*, brand-*, status-*, tax-*
  - 間距：--spacing-sidebar (240px), --spacing-sidebar-mini (56px)
  - 陰影：shadow-panel, shadow-header, shadow-modal
  - z-index：--z-sidebar(40), --z-header(50), --z-modal(70), --z-toast(80)
- CSS layout：`.app-layout` + `.sidebar-mini` class (全域 styles.scss)

## 已知警告
- `@import "tailwindcss"` 在 Dart Sass 3 會棄用，但目前不影響功能
  (Tailwind CSS v4 的已知問題，待官方更新)

**Why:** 記錄架構供後續功能開發參考
**How to apply:** 新增 feature 時遵循此結構，使用已存在的 shared 元件與 core services
