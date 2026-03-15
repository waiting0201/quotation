---
name: Quotation System Rebuild — Project Context
description: Core context for rebuilding quotation.weypro.com from SmartAdmin/Bootstrap MVC to Angular 21 + Tailwind CSS
type: project
---

## Project: quotation.weypro.com Rebuild

Rebuilding an existing ASP.NET MVC quotation management system (威庭科技報價系統) as an Angular 21 SPA.

**Tech Stack:** Angular 21 + SCSS + Tailwind CSS (no Angular Material), pure Chinese UI, enterprise admin style.

**Original system location:** D:\websystems\quotation (SmartAdmin Bootstrap template, MVC 5)

## Original System Structure (Reference)
- Layout: fixed-header + fixed left sidebar (SmartAdmin pattern)
- Sidebar width: ~220px, collapsible to icon-only ~50px
- Header height: ~50px
- Breadcrumb ribbon: ~30px bar below header, above content
- Content area: scrollable, padded

## Key Pages and Data Fields

### Quotation List (報價清單) — ItemList.cshtml
Columns: 報價編號, 客戶, 名稱, 狀態, 報價日期, 稅, 報價, 稅金, 入帳, 列印, 編輯, 刪除
Status colors: 已報價(light blue), 已簽約(primary blue), 已結案(green/success), 已取消(red/danger)
Search: by 報價編號 or 名稱

### Quotation Form (報價表單) — ItemCreate.cshtml
Tab 1 — 報價資料: 客戶選擇, 聯絡人(cascading dropdown), 英文版本(radio), 名稱, 英文名稱, 狀態(radio), 報價日期, 有效日期, 工作天, 稅金(radio: 稅外加/稅內含/免稅金), 付款方式(textarea+modal picker), 英文付款方式, 備註, 英文備註, Sitemap(file upload)
Tab 2 — 規格: tree-view spec selection
Dynamic detail rows (明細): 標題, 備註, 金額 — add/remove rows

### Customer List (客戶清單) — CustomerList.cshtml
Columns: 客戶分類, 客戶編號, 名稱, 建立日期, 編輯, 刪除

### Customer Form (客戶表單) — CustomerCreate.cshtml
Dynamic contact rows (聯絡人): 姓名, E-mail, 電話, 分機 — add/remove rows

### Dashboard (Main.cshtml)
Full-calendar view, prev/next/today buttons, events from API with status-based colors:
- Blue = 已簽約, Green = 已結案, Red = 已取消

## Menu Structure (from LeftMenu.cshtml pattern)
- 主頁 (Dashboard)
- 報價管理
- 客戶管理
- 發票管理
- 收款管理
- 系統設定 (sub: 使用者管理, 群組管理)
Permission-based dynamic display.

## Design System v1 (Session 1 — Light/Enterprise — SUPERSEDED)
Light theme, #1E293B sidebar, #FFFFFF header, #F1F5F9 bg. Superseded by Tech/Dark theme.

## Design System v2 — Tech/Futuristic (Session 2 — 2026-03-13) — CURRENT

Full spec at: D:\websystems\quotation.weypro.com\docs\design-system-tech.md

**Theme:** Dark Tech (深色科技風)
**Concept:** "Digital Blueprint" — deep space blue + cyber neon glow

**Color Palette:**
- Page bg: #070C18 (deep space blue-black)
- Surface: rgba(13, 24, 48, 0.80) glassmorphism
- Primary neon: #00D4FF (cyber blue glow)
- Cyber blue scale: cyber-400 #00B8F0, cyber-500 #00A3D4, cyber-700 #006E92
- Sidebar bg: #060C18 (deeper than page)
- Sidebar active text: #00D4FF with text-glow
- Status: 已報價 #38BDF8, 已簽約 #60A5FA, 已結案 #10FFB0 (neon green), 已取消 #F87171
- Text primary: #E8F4FF (blue-white), secondary: #94AFC8, muted: #4A6080

**Layout dimensions (v2):**
- Header height: 60px (was 56px)
- Sidebar expanded: 260px (was 240px), collapsed: 60px (was 56px)
- Ribbon: 40px (was 36px)
- Content padding: 24px (unchanged)

**Typography additions:**
- font-mono: JetBrains Mono — for numbers, stats, quotation IDs
- font-display: Inter — for display headings
- New sizes: text-3xl (24px), text-4xl (30px), text-5xl (36px) for stat cards

**Key effects:**
- Glassmorphism panels: rgba(13,24,48,0.80) + backdrop-filter:blur(12px)
- Neon glow borders: rgba(0,212,255,0.12~0.80) depending on state
- Button primary: gradient + box-shadow glow 0 0 20px rgba(0,212,255,0.40)
- Input focus: ring + glow shadow-input-focus
- Sidebar active: left 3px neon border + icon filter:drop-shadow glow

**index.html change needed:**
Add JetBrains Mono to Google Fonts link alongside Noto Sans TC.

**Tailwind approach:** Full @theme replacement (not extend). New token names differ from v1.
