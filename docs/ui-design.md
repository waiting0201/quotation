# 報價管理系統 UI 架構設計

## 一、設計方向與配色

### 設計哲學

現代 flat enterprise 風格：fixed header + left sidebar + content area。
設計原則：**資訊密度優先，視覺噪音最小化**。

### 配色系統

```
主色調 (Primary Blue)
  primary-50:  #EFF6FF
  primary-100: #DBEAFE
  primary-500: #3B82F6
  primary-600: #2563EB   ← 主操作色（按鈕、連結、active 狀態）
  primary-700: #1D4ED8   ← hover 狀態
  primary-900: #1E3A8A

結構色 (Slate)
  slate-50:  #F8FAFC    ← 頁面背景
  slate-100: #F1F5F9    ← content 區域背景
  slate-200: #E2E8F0    ← 分隔線、border
  slate-400: #94A3B8    ← placeholder、disabled
  slate-500: #64748B    ← 次要文字、表頭
  slate-700: #334155    ← 主要文字
  slate-800: #1E293B    ← Sidebar 背景
  slate-900: #0F172A    ← 最深文字

狀態色
  已報價: #3B82F6  bg: #EFF6FF   (blue)
  已簽約: #2563EB  bg: #DBEAFE   (blue-600)
  已結案: #16A34A  bg: #DCFCE7   (green)
  已取消: #DC2626  bg: #FEE2E2   (red)

稅金類型色
  稅外加: #2563EB  (blue)
  稅內含: #DB2777  (pink)
  免稅金: #D97706  (amber)

功能色
  成功: #16A34A  警告: #D97706  危險: #DC2626  資訊: #0891B2
```

## 二、Tailwind CSS 設計 Token

### tailwind.config.js

```javascript
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50:  '#EFF6FF',
          100: '#DBEAFE',
          200: '#BFDBFE',
          500: '#3B82F6',
          600: '#2563EB',
          700: '#1D4ED8',
          800: '#1E40AF',
          900: '#1E3A8A',
        },
        brand: {
          sidebar:   '#1E293B',
          header:    '#FFFFFF',
          page:      '#F1F5F9',
          surface:   '#FFFFFF',
          border:    '#E2E8F0',
        },
        status: {
          quoted:     '#3B82F6',
          contracted: '#2563EB',
          closed:     '#16A34A',
          cancelled:  '#DC2626',
          'quoted-bg':     '#EFF6FF',
          'contracted-bg': '#DBEAFE',
          'closed-bg':     '#DCFCE7',
          'cancelled-bg':  '#FEE2E2',
        },
        tax: {
          exclusive: '#2563EB',
          inclusive: '#DB2777',
          exempt:    '#D97706',
        },
      },
      fontFamily: {
        sans: ['"Noto Sans TC"', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        '2xs': ['11px', { lineHeight: '16px' }],
        'xs':  ['12px', { lineHeight: '16px' }],
        'sm':  ['13px', { lineHeight: '20px' }],
        'base':['14px', { lineHeight: '20px' }],
        'md':  ['15px', { lineHeight: '22px' }],
        'lg':  ['16px', { lineHeight: '24px' }],
        'xl':  ['18px', { lineHeight: '28px' }],
        '2xl': ['20px', { lineHeight: '28px' }],
      },
      spacing: {
        '4.5': '18px',
        '13':  '52px',
        '15':  '60px',
        '18':  '72px',
        'sidebar':      '240px',
        'sidebar-mini': '56px',
        'header':       '56px',
        'ribbon':       '36px',
      },
      boxShadow: {
        'panel':  '0 1px 3px 0 rgb(0 0 0 / 0.08), 0 1px 2px -1px rgb(0 0 0 / 0.06)',
        'header': '0 1px 0 0 #E2E8F0',
        'modal':  '0 20px 60px -10px rgb(0 0 0 / 0.25)',
        'dropdown': '0 4px 16px -2px rgb(0 0 0 / 0.12)',
      },
      zIndex: {
        'sidebar': '40',
        'header':  '50',
        'dropdown':'60',
        'modal':   '70',
        'toast':   '80',
      },
      borderRadius: {
        'btn': '6px',
        'input': '6px',
        'panel': '8px',
        'modal': '10px',
      },
      transitionDuration: {
        'sidebar': '200ms',
      },
    },
  },
  plugins: [],
}
```

## 三、響應式斷點策略

```
< md (768px)  → Sidebar 隱藏，漢堡選單觸發 overlay
md            → icon-only 模式（56px），hover 展開
lg+ (1024px)  → 完整展開（240px），可手動收縮

Header：永遠 fixed top，100% width
Content padding：
  mobile:  px-4 py-4
  tablet:  px-6 py-5
  desktop: px-6 py-6
```

## 四、頁面 Wireframe

### 1. 登入頁面

```
┌─────────────────────────────────────────────┐
│              [背景: slate-100]              │
│                                             │
│    ┌─────────────────────────────────┐      │
│    │   max-w-[400px] p-10           │      │
│    │   bg-white rounded-modal       │      │
│    │   shadow-modal                  │      │
│    │                                 │      │
│    │        ┌──────────────┐         │      │
│    │        │  LOGO IMAGE  │         │      │
│    │        └──────────────┘         │      │
│    │     威庭科技 報價管理系統         │      │
│    │                                 │      │
│    │  電子郵件                        │      │
│    │  ┌────────────────────────┐     │      │
│    │  │  輸入電子郵件地址       │     │      │
│    │  └────────────────────────┘     │      │
│    │                                 │      │
│    │  密碼                            │      │
│    │  ┌─────────────────────[眼]─┐   │      │
│    │  │  輸入密碼               │   │      │
│    │  └───────────────────────────┘   │      │
│    │                                 │      │
│    │  ┌────────────────────────────┐ │      │
│    │  │        登　入              │ │      │
│    │  └────────────────────────────┘ │      │
│    │   btn-primary w-full h-10       │      │
│    └─────────────────────────────────┘      │
│    © 威庭科技  text-slate-400 text-xs       │
└─────────────────────────────────────────────┘
```

### 2. 主佈局

```
┌──────────────────────────────────────────────────────┐
│ HEADER (fixed, h-14, z-50, bg-white, shadow-header)  │
│ [≡] [LOGO]                    [使用者名稱 ▼] [登出]  │
├──────────┬───────────────────────────────────────────┤
│ SIDEBAR  │  RIBBON BREADCRUMB (h-9, bg-slate-700)   │
│ 240px    │  首頁 > 報價管理 > 報價清單                │
│ bg:      ├───────────────────────────────────────────┤
│ slate-800│                                           │
│          │  MAIN CONTENT AREA                        │
│ ┌──────┐ │  bg-slate-100, p-6                        │
│ │用戶區│ │                                           │
│ │頭像  │ │  [PAGE CONTENT]                           │
│ │姓名  │ │                                           │
│ └──────┘ │                                           │
│ ──────── │                                           │
│ ● 主頁   │                                           │
│ ○ 報價   │                                           │
│ ○ 客戶   │                                           │
│ ○ 發票   │                                           │
│ ○ 收款   │                                           │
│ ○ 系統   │                                           │
│   使用者 │                                           │
│   群組   │                                           │
│ ──────── │                                           │
│ ◀ 收縮   │  FOOTER  Weypro ©                        │
└──────────┴───────────────────────────────────────────┘
```

**Sidebar 選單項目：**
- 高度: h-10, padding: px-4
- icon: w-5 h-5, mr-3, text-slate-400
- active: bg-primary-600 text-white
- hover: bg-slate-700
- 子選單縮排: pl-12

### 3. Dashboard 行事曆

```
┌─────────────────────────────────────────────┐
│  報價管理行事曆  text-xl font-semibold       │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │ bg-white rounded-panel shadow-panel  │   │
│  │                                      │   │
│  │ [◀ 上月] [今天] [下月 ▶]            │   │
│  │                                      │   │
│  │  ┌──┬──┬──┬──┬──┬──┬──┐           │   │
│  │  │日│一│二│三│四│五│六│           │   │
│  │  ├──┼──┼──┼──┼──┼──┼──┤           │   │
│  │  │  │[藍]│ │[綠]│  │[紅]│           │   │
│  │  └──┴──┴──┴──┴──┴──┴──┘           │   │
│  │                                      │   │
│  │ ■ 已報價 ■ 已簽約 ■ 已結案 ■ 已取消  │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘

事件色：
  已報價: bg-blue-100 text-blue-700
  已簽約: bg-blue-600 text-white
  已結案: bg-green-600 text-white
  已取消: bg-red-100 text-red-600 line-through
```

### 4. 列表頁面

```
┌──────────────────────────────────────────┐
│ bg-white rounded-panel shadow-panel      │
│                                          │
│ TOOLBAR (px-5 py-3 border-b)            │
│ [+ 新增] │ [搜尋欄位▼] [關鍵字] [搜尋]  │
│                                          │
│ TABLE                                    │
│ ┌──────┬────┬────┬──┬──────┬──┬──┬──┐  │
│ │編號  │客戶│名稱│狀│日期  │總計│入帳│操│  │
│ ├──────┼────┼────┼──┼──────┼──┼──┼──┤  │
│ │QUO.. │XX  │... │■ │2024  │100│105│[⋮]│  │
│ └──────┴────┴────┴──┴──────┴──┴──┴──┘  │
│                                          │
│ PAGINATION (justify-end py-3 px-5)      │
│ [<] [1] [2] [3] ... [10] [>]            │
└──────────────────────────────────────────┘

操作欄 [⋮] 下拉：
┌──────────────┐
│ 檢視詳情     │
│ 編輯         │
│ 列印 PDF     │
├──────────────┤
│ 刪除  (red)  │
└──────────────┘
```

### 5. 報價單表單

```
┌───────────────────────────────────────────┐
│ PANEL                                     │
│                                           │
│ ── 基本資料 ────────────────────────────  │
│ 客戶選擇 *     聯絡人                     │
│ [select]       [select]                   │
│                                           │
│ 名稱 *                                   │
│ [input]                                   │
│                                           │
│ 狀態: ○已報價 ○已簽約 ○已結案 ○已取消    │
│                                           │
│ 報價日期 *     有效日期                   │
│ [datepicker]   [datepicker]               │
│                                           │
│ 工作天數       稅金類型                   │
│ [input]        ○稅外加 ○稅內含 ○免稅金   │
│                                           │
│ 付款方式                    [從清單選取]   │
│ [textarea]                                │
│                                           │
│ ── 報價明細 ────────────────────────────  │
│ [+ 新增明細]                              │
│                                           │
│ ┌────────────────────────────────────┐   │
│ │ 明細 #1                     [刪除] │   │
│ │ 標題 *        備註      金額 *     │   │
│ │ [input]       [input]   [NT$    ]  │   │
│ └────────────────────────────────────┘   │
│                                           │
│ ┌ - - - - - - - - - - - - - - - - - ┐   │
│ │  + 新增明細  (虛線框 dashed)       │   │
│ └ - - - - - - - - - - - - - - - - - ┘   │
│                                           │
│            小計 ──── NT$ 100,000          │
│            稅金 ──── NT$   5,000          │
│            ═════════════════════           │
│            總計 ──── NT$ 105,000          │
│                                           │
│ FOOTER (border-t)                        │
│ [取消]                    [儲存報價單]    │
└───────────────────────────────────────────┘
```

## 五、元件規格

### Buttons

```
Primary:  bg-primary-600 hover:bg-primary-700 text-white
          h-9 px-4 text-sm font-medium rounded-btn
Secondary: bg-white border border-slate-300 text-slate-700 hover:bg-slate-50
Danger:   bg-red-600 hover:bg-red-700 text-white
Ghost:    bg-transparent text-slate-600 hover:bg-slate-100

Sizes: sm(h-7 px-3 text-xs) md(h-9 px-4 text-sm) lg(h-10 px-5 text-sm)
```

### Form Inputs

```
h-9 w-full px-3 text-sm text-slate-800
bg-white border border-slate-300 rounded-input
placeholder:text-slate-400
focus:border-primary-500 focus:ring-1 focus:ring-primary-500
disabled:bg-slate-50 disabled:text-slate-400

Error: border-red-400 focus:ring-red-200
Label: text-sm font-medium text-slate-700 mb-1
Error msg: text-xs text-red-600 mt-1
欄位間距: space-y-4, Section 間距: space-y-6
```

### Table

```
th: px-3 h-10 text-left text-xs font-medium text-slate-500
    uppercase tracking-wider bg-slate-50 border-b slate-200
td: px-3 h-12 text-slate-700 border-b border-slate-100
tr hover: hover:bg-slate-50
數字欄位: text-right font-mono tabular-nums
```

### Status Badge

```
base: inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium

已報價: bg-blue-50   text-blue-700
已簽約: bg-blue-600  text-white
已結案: bg-green-100 text-green-700
已取消: bg-red-100   text-red-600
```

### Panel

```
外層: bg-white rounded-panel shadow-panel overflow-hidden
Header: px-5 py-4 border-b text-base font-semibold text-slate-800
Toolbar: px-5 py-3 border-b bg-slate-50/50
Footer: px-5 py-4 border-t bg-slate-50/50
```

### Modal

```
Overlay: fixed inset-0 bg-slate-900/50 z-modal
Dialog: bg-white rounded-modal shadow-modal max-w-[440px] mx-auto
Header: px-6 py-4 border-b
Body: px-6 py-5
Footer: px-6 py-4 border-t flex justify-end gap-3
```

### Pagination

```
h-8 min-w-[32px] px-2 text-sm rounded
預設: text-slate-600 hover:bg-slate-100
active: bg-primary-600 text-white
disabled: text-slate-300
```

### 動態明細行

```
每筆明細 = Card：bg-slate-50 border border-slate-200 rounded-panel p-4
新增按鈕：border-2 border-dashed border-slate-300 hover:border-primary-400
刪除：absolute top-3 right-3, btn-ghost text-red-500
```

## 六、SCSS 全域變數

```scss
:root {
  --sidebar-width: 240px;
  --sidebar-mini-width: 56px;
  --header-height: 56px;
  --ribbon-height: 36px;
  --content-padding: 24px;
  --transition-sidebar: 200ms cubic-bezier(0.4, 0, 0.2, 1);
}

.app-layout {
  padding-left: var(--sidebar-width);
  padding-top: var(--header-height);
  transition: padding-left var(--transition-sidebar);
  &.sidebar-mini { padding-left: var(--sidebar-mini-width); }
}

@media (max-width: 767px) {
  .app-layout { padding-left: 0; }
  .sidebar {
    transform: translateX(-100%);
    &.open { transform: translateX(0); }
  }
}
```

## 七、字型

- 主字型：Noto Sans TC（繁體中文覆蓋最完整，跨平台一致）
- Base font-size：14px（企業後台資訊密度高，14px 最佳平衡）
