# Weypro 報價系統 — 科技風設計規範
## Tech/Futuristic Design System v2.0

> 適用：Admin (Angular 21 + Tailwind CSS v4)
> 語系：繁體中文
> 主題：深色科技風（Dark Tech）

---

## 目錄

1. [設計哲學](#1-設計哲學)
2. [色彩系統](#2-色彩系統)
3. [字體系統](#3-字體系統)
4. [間距與尺寸](#4-間距與尺寸)
5. [陰影與發光效果](#5-陰影與發光效果)
6. [動畫與過渡](#6-動畫與過渡)
7. [完整 Tailwind CSS v4 @theme Token](#7-完整-tailwind-css-v4-theme-token)
8. [Login 登入頁](#8-login-登入頁)
9. [Main Layout 主版型](#9-main-layout-主版型)
10. [Dashboard 儀表板](#10-dashboard-儀表板)
11. [List 列表頁](#11-list-列表頁)
12. [Form 表單頁](#12-form-表單頁)
13. [Shared Components 共用元件](#13-shared-components-共用元件)
14. [特殊科技風效果 CSS 實作](#14-特殊科技風效果-css-實作)

---

## 1. 設計哲學

### 核心概念：「數位藍圖」(Digital Blueprint)

本設計系統以「工程藍圖」與「數位介面」為意象原點。想像你在操作一套高度專業的企業級指揮系統——深色背景代表專注的工作環境，藍色霓虹光線代表精準的數據流動，幾何網格代表結構化的思維框架。

### 設計原則

**深色優先，但非純黑**
背景使用深藍灰（#0A0F1E → #0D1526）而非純黑，避免過度對比造成眼部疲勞，同時維持科技感的沉浸氛圍。

**藍色光譜作為主要語言**
Weypro 的品牌藍（#4BA3D4）作為起點，向上延伸至電子藍（#00D4FF）、向下沉澱至深海藍（#1A3A6B），形成完整的藍色能量層次。

**資訊密度與呼吸感的平衡**
企業報表系統需要高資訊密度，但深色主題中的過度擁擠會造成視覺壓力。使用 glassmorphism 卡片創造層次感，用發光邊框而非實色填充來界定空間。

**動畫服務於功能**
所有動畫效果都有明確的功能目的：載入狀態告知等待、過渡效果指示方向、Hover 光暈確認互動目標。絕不使用純裝飾性動畫干擾工作流程。

---

## 2. 色彩系統

### 2.1 背景層次（Background Hierarchy）

```
Layer 0 — 頁面底層 (Page Base)
  #070C18  極深宇宙藍，body 背景

Layer 1 — 應用底層 (App Surface)
  #0A1020  深藍黑，主內容區背景

Layer 2 — 元件底層 (Component Surface)
  #0D1830  深藍，卡片/面板底色（glassmorphism 基底）

Layer 3 — 懸浮層 (Elevated Surface)
  #112040  中深藍，hover 狀態、次要卡片

Layer 4 — 高亮層 (Highlighted Surface)
  #162850  較亮深藍，active 狀態、選中項目
```

### 2.2 主色（Primary — Cyber Blue）

```
cyber-50:   #E0F6FF  — 極淡，背景 tint
cyber-100:  #B8EEFF  — 淡藍，hover bg
cyber-200:  #7DDCFF  — 淺藍，secondary elements
cyber-300:  #4BC9FF  — 品牌接近色
cyber-400:  #00B8F0  — 互動元素
cyber-500:  #00A3D4  — 主色（Weypro 品牌藍基礎）
cyber-600:  #0088B3  — 主色加深（按鈕 hover）
cyber-700:  #006E92  — 深色主色
cyber-800:  #004F6B  — 更深（邊框、分隔線）
cyber-900:  #002F40  — 最深（深色元素）

neon-blue:  #00D4FF  — 霓虹藍，發光效果、active 狀態
neon-glow:  #00AAFF  — 發光環繞色
```

### 2.3 輔助色（Accent）

```
accent-cyan:    #06FFF4  — 青色霓虹，強調資訊、圖表高亮
accent-violet:  #8B5CF6  — 紫色，系統設定類功能
accent-emerald: #10FFB0  — 翠綠霓虹，成功狀態發光
accent-amber:   #FFB300  — 琥珀，警告狀態
```

### 2.4 狀態色（Status Colors）

```
已報價（Quoted）：
  文字: #38BDF8  天藍
  背景: rgba(56, 189, 248, 0.12)
  邊框: rgba(56, 189, 248, 0.35)
  發光: 0 0 8px rgba(56, 189, 248, 0.4)

已簽約（Contracted）：
  文字: #60A5FA  藍
  背景: rgba(96, 165, 250, 0.12)
  邊框: rgba(96, 165, 250, 0.35)
  發光: 0 0 8px rgba(96, 165, 250, 0.4)

已結案（Closed）：
  文字: #10FFB0  翠綠霓虹
  背景: rgba(16, 255, 176, 0.10)
  邊框: rgba(16, 255, 176, 0.30)
  發光: 0 0 8px rgba(16, 255, 176, 0.4)

已取消（Cancelled）：
  文字: #F87171  紅
  背景: rgba(248, 113, 113, 0.10)
  邊框: rgba(248, 113, 113, 0.30)
  發光: 0 0 8px rgba(248, 113, 113, 0.3)
```

### 2.5 文字色（Text）

```
text-primary:   #E8F4FF  — 主要文字（白偏藍）
text-secondary: #94AFC8  — 次要文字
text-muted:     #4A6080  — 輔助文字、placeholder
text-disabled:  #2A3A50  — 禁用狀態
text-inverse:   #070C18  — 深色背景上的深色文字（用於淺色按鈕）
```

### 2.6 邊框色（Border）

```
border-subtle:  rgba(0, 212, 255, 0.08)   — 極淡邊框
border-default: rgba(0, 212, 255, 0.15)   — 標準邊框
border-strong:  rgba(0, 212, 255, 0.30)   — 強調邊框
border-active:  rgba(0, 212, 255, 0.60)   — 焦點/active 邊框
border-glow:    rgba(0, 212, 255, 0.80)   — 霓虹發光邊框
```

### 2.7 Sidebar 專用色

```
sidebar-bg:       #060C18  — Sidebar 背景（比頁面更深）
sidebar-surface:  #0A1428  — Sidebar item hover 底色
sidebar-active:   #0D1E3D  — 當前選中項目背景
sidebar-border:   rgba(0, 212, 255, 0.10)  — 側欄邊線
sidebar-text:     #7A9AB8  — 未選中文字
sidebar-text-active: #00D4FF  — 選中文字（霓虹藍）
```

---

## 3. 字體系統

### 3.1 字體堆疊

```css
/* 主要字體 — 中英文混排 */
--font-sans: "Noto Sans TC", "Inter", system-ui, sans-serif;

/* 數字/代碼專用 — 統計數字、編號 */
--font-mono: "JetBrains Mono", "Fira Code", "Roboto Mono", monospace;

/* 標題/科技感英文 */
--font-display: "Inter", "Noto Sans TC", system-ui, sans-serif;
```

### 3.2 字體尺寸

（沿用現有 token，新增 display 大字）

```
2xs:  11px / 16px  — 輔助標籤、版權
xs:   12px / 16px  — 表格標頭、徽章
sm:   13px / 20px  — 次要說明文字
base: 14px / 20px  — 主要內文
md:   15px / 22px  — 強調內文
lg:   16px / 24px  — 小標題、卡片標題
xl:   18px / 28px  — 頁面標題
2xl:  20px / 28px  — 大標題
3xl:  24px / 32px  — 區塊標題
4xl:  30px / 36px  — 統計數字
5xl:  36px / 40px  — Hero 統計
```

### 3.3 字重

```
light:     300  — 裝飾性大字
regular:   400  — 正文
medium:    500  — 強調文字、標籤
semibold:  600  — 標題、按鈕
bold:      700  — 重要標題
```

---

## 4. 間距與尺寸

### 4.1 版型尺寸（維持現有，略調整）

```
sidebar-width:       260px  （原 240px，增加呼吸感）
sidebar-mini-width:  60px   （原 56px）
header-height:       60px   （原 56px，增加霓虹條空間）
ribbon-height:       40px   （原 36px）
content-padding:     24px   （維持）
```

### 4.2 圓角

```
radius-sm:     4px   — 徽章、標籤
radius-btn:    6px   — 按鈕、輸入框
radius-input:  6px   — 表單元素
radius-panel:  10px  — 卡片（原 8px，稍微柔化）
radius-modal:  14px  — 對話框
radius-full:   9999px — 圓形元素
```

---

## 5. 陰影與發光效果

### 5.1 陰影系統

```css
/* Panel 卡片 — glassmorphism 多層陰影 */
--shadow-panel:
  0 0 0 1px rgba(0, 212, 255, 0.12),
  0 4px 24px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.04);

/* Header — 頂部發光線 */
--shadow-header:
  0 1px 0 rgba(0, 212, 255, 0.20),
  0 4px 20px rgba(0, 0, 0, 0.5);

/* Modal — 大型發光 */
--shadow-modal:
  0 0 0 1px rgba(0, 212, 255, 0.20),
  0 25px 80px rgba(0, 0, 0, 0.7),
  0 0 60px rgba(0, 212, 255, 0.08);

/* Dropdown */
--shadow-dropdown:
  0 0 0 1px rgba(0, 212, 255, 0.12),
  0 8px 32px rgba(0, 0, 0, 0.5);

/* Button 主色發光 */
--shadow-btn-primary:
  0 0 20px rgba(0, 212, 255, 0.40),
  0 4px 12px rgba(0, 0, 0, 0.3);

/* Button hover 發光 */
--shadow-btn-primary-hover:
  0 0 30px rgba(0, 212, 255, 0.60),
  0 6px 16px rgba(0, 0, 0, 0.4);

/* Input focus 發光 */
--shadow-input-focus:
  0 0 0 3px rgba(0, 212, 255, 0.20),
  0 0 12px rgba(0, 212, 255, 0.15);

/* 統計卡片發光（顏色依類型）*/
--shadow-stat-blue:    0 0 30px rgba(0, 212, 255, 0.15), 0 8px 32px rgba(0,0,0,0.4);
--shadow-stat-cyan:    0 0 30px rgba(6, 255, 244, 0.12), 0 8px 32px rgba(0,0,0,0.4);
--shadow-stat-green:   0 0 30px rgba(16, 255, 176, 0.12), 0 8px 32px rgba(0,0,0,0.4);
--shadow-stat-violet:  0 0 30px rgba(139, 92, 246, 0.15), 0 8px 32px rgba(0,0,0,0.4);
```

### 5.2 Text Shadow（文字發光）

```css
/* 霓虹文字 */
--text-glow-blue:  0 0 10px rgba(0, 212, 255, 0.8), 0 0 20px rgba(0, 212, 255, 0.4);
--text-glow-cyan:  0 0 10px rgba(6, 255, 244, 0.8), 0 0 20px rgba(6, 255, 244, 0.4);
--text-glow-green: 0 0 10px rgba(16, 255, 176, 0.8), 0 0 20px rgba(16, 255, 176, 0.4);
```

---

## 6. 動畫與過渡

### 6.1 過渡時間

```css
--duration-instant:  100ms   — 即時反應（按鈕 active）
--duration-fast:     150ms   — 快速（hover 顏色）
--duration-normal:   200ms   — 標準（sidebar collapse）
--duration-slow:     300ms   — 緩慢（modal open）
--duration-glacial:  500ms   — 極慢（頁面進場）
```

### 6.2 Easing

```css
--ease-out:     cubic-bezier(0.0, 0.0, 0.2, 1)   — 元素進場（從外往內）
--ease-in:      cubic-bezier(0.4, 0.0, 1, 1)      — 元素離場
--ease-inout:   cubic-bezier(0.4, 0.0, 0.2, 1)    — Sidebar 展開/收合
--ease-spring:  cubic-bezier(0.34, 1.56, 0.64, 1) — 彈性效果（徽章出現）
```

### 6.3 關鍵動畫清單

```
@keyframes neon-pulse      — 發光邊框脈動（無限循環，2s）
@keyframes grid-scroll     — 背景網格緩慢移動（Login 頁，20s）
@keyframes scan-line       — 掃描線向下移動（Login 頁，3s）
@keyframes data-stream     — 數字/字符流動（裝飾性）
@keyframes fade-up         — 元素從下方淡入（頁面進場）
@keyframes slide-in-right  — 從右滑入（Toast）
@keyframes slide-in-left   — 從左滑入（Sidebar mobile）
@keyframes count-up        — 統計數字計數動畫（Dashboard）
@keyframes shimmer         — 骨架載入閃爍
@keyframes spin-slow        — 緩慢旋轉（loading icon）
```

---

## 7. 完整 Tailwind CSS v4 @theme Token

以下為完整替換現有 `Admin/src/styles.scss` 的 `@theme` 區塊：

```scss
@use "tailwindcss";

@theme {
  /* ============================================
     背景層次 (Background Layers)
  ============================================ */
  --color-bg-base:       #070C18;
  --color-bg-app:        #0A1020;
  --color-bg-surface:    #0D1830;
  --color-bg-elevated:   #112040;
  --color-bg-highlight:  #162850;

  /* ============================================
     主色 — Cyber Blue
  ============================================ */
  --color-cyber-50:   #E0F6FF;
  --color-cyber-100:  #B8EEFF;
  --color-cyber-200:  #7DDCFF;
  --color-cyber-300:  #4BC9FF;
  --color-cyber-400:  #00B8F0;
  --color-cyber-500:  #00A3D4;
  --color-cyber-600:  #0088B3;
  --color-cyber-700:  #006E92;
  --color-cyber-800:  #004F6B;
  --color-cyber-900:  #002F40;

  /* 霓虹發光色 */
  --color-neon-blue:   #00D4FF;
  --color-neon-cyan:   #06FFF4;
  --color-neon-green:  #10FFB0;
  --color-neon-amber:  #FFB300;
  --color-neon-violet: #A78BFA;
  --color-neon-red:    #FF4466;

  /* ============================================
     輔助色 (Accent)
  ============================================ */
  --color-accent-cyan:    #06FFF4;
  --color-accent-violet:  #8B5CF6;
  --color-accent-emerald: #10FFB0;
  --color-accent-amber:   #FFB300;
  --color-accent-red:     #FF4466;

  /* ============================================
     狀態色 (Status)
  ============================================ */
  /* 已報價 */
  --color-status-quoted:          #38BDF8;
  --color-status-quoted-bg:       rgba(56, 189, 248, 0.12);
  --color-status-quoted-border:   rgba(56, 189, 248, 0.35);
  /* 已簽約 */
  --color-status-contracted:      #60A5FA;
  --color-status-contracted-bg:   rgba(96, 165, 250, 0.12);
  --color-status-contracted-border: rgba(96, 165, 250, 0.35);
  /* 已結案 */
  --color-status-closed:          #10FFB0;
  --color-status-closed-bg:       rgba(16, 255, 176, 0.10);
  --color-status-closed-border:   rgba(16, 255, 176, 0.30);
  /* 已取消 */
  --color-status-cancelled:       #F87171;
  --color-status-cancelled-bg:    rgba(248, 113, 113, 0.10);
  --color-status-cancelled-border: rgba(248, 113, 113, 0.30);

  /* 稅金類型色 */
  --color-tax-exclusive:  #38BDF8;
  --color-tax-inclusive:  #C084FC;
  --color-tax-exempt:     #FCD34D;

  /* ============================================
     語意色 (Semantic)
  ============================================ */
  --color-success:        #10FFB0;
  --color-success-bg:     rgba(16, 255, 176, 0.10);
  --color-warning:        #FFB300;
  --color-warning-bg:     rgba(255, 179, 0, 0.10);
  --color-danger:         #FF4466;
  --color-danger-bg:      rgba(255, 68, 102, 0.10);
  --color-info:           #00D4FF;
  --color-info-bg:        rgba(0, 212, 255, 0.10);

  /* ============================================
     文字色 (Text)
  ============================================ */
  --color-text-primary:   #E8F4FF;
  --color-text-secondary: #94AFC8;
  --color-text-muted:     #4A6080;
  --color-text-disabled:  #2A3A50;
  --color-text-inverse:   #070C18;
  --color-text-neon:      #00D4FF;

  /* ============================================
     邊框色 (Border)
  ============================================ */
  --color-border-subtle:  rgba(0, 212, 255, 0.08);
  --color-border-default: rgba(0, 212, 255, 0.15);
  --color-border-strong:  rgba(0, 212, 255, 0.30);
  --color-border-active:  rgba(0, 212, 255, 0.60);
  --color-border-glow:    rgba(0, 212, 255, 0.80);

  /* ============================================
     Sidebar 專用
  ============================================ */
  --color-sidebar-bg:           #060C18;
  --color-sidebar-surface:      #0A1428;
  --color-sidebar-active:       #0D1E3D;
  --color-sidebar-border:       rgba(0, 212, 255, 0.10);
  --color-sidebar-text:         #7A9AB8;
  --color-sidebar-text-active:  #00D4FF;
  --color-sidebar-icon:         #4A6A88;
  --color-sidebar-icon-active:  #00D4FF;

  /* ============================================
     字體 (Typography)
  ============================================ */
  --font-sans:    "Noto Sans TC", "Inter", system-ui, sans-serif;
  --font-mono:    "JetBrains Mono", "Fira Code", "Roboto Mono", monospace;
  --font-display: "Inter", "Noto Sans TC", system-ui, sans-serif;

  /* 字體尺寸 */
  --text-2xs: 11px;
  --text-2xs--line-height: 16px;
  --text-xs:  12px;
  --text-xs--line-height: 16px;
  --text-sm:  13px;
  --text-sm--line-height: 20px;
  --text-base: 14px;
  --text-base--line-height: 20px;
  --text-md:  15px;
  --text-md--line-height: 22px;
  --text-lg:  16px;
  --text-lg--line-height: 24px;
  --text-xl:  18px;
  --text-xl--line-height: 28px;
  --text-2xl: 20px;
  --text-2xl--line-height: 28px;
  --text-3xl: 24px;
  --text-3xl--line-height: 32px;
  --text-4xl: 30px;
  --text-4xl--line-height: 36px;
  --text-5xl: 36px;
  --text-5xl--line-height: 40px;

  /* ============================================
     版型尺寸 (Layout Dimensions)
  ============================================ */
  --spacing-sidebar:      260px;
  --spacing-sidebar-mini: 60px;
  --spacing-header:       60px;
  --spacing-ribbon:       40px;

  /* ============================================
     圓角 (Border Radius)
  ============================================ */
  --radius-sm:    4px;
  --radius-btn:   6px;
  --radius-input: 6px;
  --radius-panel: 10px;
  --radius-modal: 14px;
  --radius-full:  9999px;

  /* ============================================
     陰影與發光 (Shadows & Glow)
  ============================================ */
  --shadow-panel:
    0 0 0 1px rgba(0, 212, 255, 0.12),
    0 4px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.04);

  --shadow-header:
    0 1px 0 rgba(0, 212, 255, 0.20),
    0 4px 20px rgba(0, 0, 0, 0.5);

  --shadow-modal:
    0 0 0 1px rgba(0, 212, 255, 0.20),
    0 25px 80px rgba(0, 0, 0, 0.7),
    0 0 60px rgba(0, 212, 255, 0.08);

  --shadow-dropdown:
    0 0 0 1px rgba(0, 212, 255, 0.12),
    0 8px 32px rgba(0, 0, 0, 0.5);

  --shadow-btn-primary:
    0 0 20px rgba(0, 212, 255, 0.40),
    0 4px 12px rgba(0, 0, 0, 0.3);

  --shadow-btn-primary-hover:
    0 0 30px rgba(0, 212, 255, 0.60),
    0 6px 16px rgba(0, 0, 0, 0.4);

  --shadow-input-focus:
    0 0 0 3px rgba(0, 212, 255, 0.20),
    0 0 12px rgba(0, 212, 255, 0.15);

  --shadow-stat-blue:   0 0 30px rgba(0, 212, 255, 0.15), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-cyan:   0 0 30px rgba(6, 255, 244, 0.12), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-green:  0 0 30px rgba(16, 255, 176, 0.12), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-violet: 0 0 30px rgba(139, 92, 246, 0.15), 0 8px 32px rgba(0,0,0,0.4);

  /* ============================================
     Z-Index
  ============================================ */
  --z-sidebar:  40;
  --z-header:   50;
  --z-dropdown: 60;
  --z-modal:    70;
  --z-toast:    80;

  /* ============================================
     動畫時間 (Animation Durations)
  ============================================ */
  --duration-instant: 100ms;
  --duration-fast:    150ms;
  --duration-normal:  200ms;
  --duration-slow:    300ms;
  --duration-glacial: 500ms;
}
```

---

## 8. Login 登入頁

### 8.1 Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   [背景：深藍黑 #070C18]                                    │
│   [全域：等角網格線，rgba(0,212,255,0.04)]                  │
│   [動態：網格緩慢向右下漂移，20s 循環]                      │
│   [掃描線：水平藍色光條，每 3s 由上向下掃過]                │
│   [角落裝飾：左下、右上各一個幾何六角形框線圖案]            │
│                                                             │
│              ┌─────────────────────────────┐               │
│              │  ╔══════════════════════╗   │               │
│              │  ║   [weypro LOGO]      ║   │               │
│              │  ║   120×32px           ║   │               │
│              │  ╚══════════════════════╝   │               │
│              │                             │               │
│              │  威庭科技報價管理系統        │               │
│              │  [text-sm, text-muted]      │               │
│              │                             │               │
│              │  ─────────────────────────  │               │
│              │                             │               │
│              │  電子郵件                   │               │
│              │  [input: email]             │               │
│              │                             │               │
│              │  密碼                       │               │
│              │  [input: password] [眼睛]   │               │
│              │                             │               │
│              │  [錯誤訊息區塊，紅色]        │               │
│              │                             │               │
│              │  [■■■■ 登入系統 ■■■■]       │               │
│              │                             │               │
│              └─────────────────────────────┘               │
│                                                             │
│   © 2025 威庭科技 Weypro. All rights reserved.             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 8.2 詳細規格

**頁面背景**
- 背景色：`#070C18`
- 背景圖案：SVG 等角網格（45 度斜線格），線條色 `rgba(0, 212, 255, 0.04)`，格子大小 40×40px
- 動態效果：網格以 `background-position` 動畫緩慢位移，製造深空視差感
- 掃描線：`position: fixed`，高度 2px，`background: linear-gradient(90deg, transparent, rgba(0, 212, 255, 0.6), transparent)`，`animation: scan-line 4s ease-in-out infinite`
- 角落幾何：左下角放置一個 120×120px 的六角形 SVG 裝飾框（`border-color: rgba(0, 212, 255, 0.15)`），右上角對稱放置

**登入卡片（Login Card）**
- 寬度：400px（mobile: 100% - 32px margin）
- 位置：絕對居中（`align-items: center`, `justify-content: center`）
- 背景：`rgba(10, 20, 40, 0.85)` + `backdrop-filter: blur(20px)`
- 邊框：`1px solid rgba(0, 212, 255, 0.20)`
- 圓角：`14px`
- Padding：`40px 36px`（mobile: `32px 24px`）
- 陰影：`0 0 0 1px rgba(0, 212, 255, 0.08), 0 32px 80px rgba(0, 0, 0, 0.6), 0 0 80px rgba(0, 212, 255, 0.05)`
- 頂部裝飾線：`border-top: 2px solid` + `background: linear-gradient(90deg, transparent, #00D4FF, transparent)` — 發光頂線
- 進場動畫：`fade-up 0.5s ease-out`

**Logo 區**
- 圖片：`/assets/images/weypro-logo.png`，`height: 32px`，`width: auto`
- 下方品牌文字：「威庭科技報價管理系統」，`text-sm`，`text-muted`，`letter-spacing: 0.05em`
- Logo 區 margin-bottom：`28px`
- 分隔線：`1px solid rgba(0, 212, 255, 0.10)`，margin `20px 0`

**輸入欄位**
- Label：`text-xs`, `font-medium`, `text-secondary`, `letter-spacing: 0.04em`, `margin-bottom: 6px`
- Input 背景：`rgba(6, 18, 36, 0.8)`
- Input 邊框：`1px solid rgba(0, 212, 255, 0.15)`
- Input 文字色：`#E8F4FF`
- Input placeholder：`#4A6080`
- Input focus：邊框 `rgba(0, 212, 255, 0.60)` + shadow `0 0 0 3px rgba(0, 212, 255, 0.15), 0 0 12px rgba(0, 212, 255, 0.10)`
- Input 高度：`42px`，圓角 `6px`，padding `10px 14px`
- Password 眼睛 icon：右側 padding-right `40px`，icon 色 `#4A6080`，hover `#00D4FF`
- 欄位間距：`margin-bottom: 16px`

**錯誤訊息**
- 背景：`rgba(255, 68, 102, 0.08)`
- 邊框：`1px solid rgba(255, 68, 102, 0.25)`
- 左側：`3px solid #FF4466`（實色左邊框）
- 文字色：`#F87171`，`text-sm`
- 圓角：`6px`，padding：`10px 14px`
- 左側 icon：警告三角形

**登入按鈕**
- 寬度：100%
- 高度：`44px`
- 背景：`linear-gradient(135deg, #006E92, #00A3D4, #00D4FF)`
- 文字：`白色`, `font-semibold`, `text-base`, `letter-spacing: 0.05em`
- 圓角：`6px`
- 陰影：`0 0 20px rgba(0, 212, 255, 0.40)`
- Hover：背景更亮，陰影增強 `0 0 35px rgba(0, 212, 255, 0.65)`，`transform: translateY(-1px)`
- Active：`transform: translateY(0)`，陰影縮減
- 載入中：按鈕禁用 + 旋轉圈 icon + 文字改為「驗證中...」
- 過渡：`all 200ms ease`

**版權文字**
- 位置：固定於底部，`position: fixed; bottom: 24px`
- 文字：`text-2xs`, `text-muted`, `text-center`

---

## 9. Main Layout 主版型

### 9.1 整體結構 Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│ SIDEBAR (260px)         │ HEADER (100% - 260px, h:60px)     │
│ ┌─────────────────────┐ │ ┌───────────────────────────────┐ │
│ │ [LOGO AREA]         │ │ │ [≡] [麵包屑導航]    [用戶] [≡]│ │
│ │  weypro  260×60     │ │ └───────────────────────────────┘ │
│ ├─────────────────────┤ ├───────────────────────────────────┤
│ │ [SEARCH BAR]        │ │ RIBBON (40px)                     │
│ │  🔍 搜尋...          │ │ 主頁 / 當前頁面                  │
│ ├─────────────────────┤ ├───────────────────────────────────┤
│ │ NAV ITEMS:          │ │                                   │
│ │ ● 主頁              │ │   CONTENT AREA                    │
│ │ ○ 報價管理          │ │   (scrollable, p-6)               │
│ │ ○ 客戶管理          │ │                                   │
│ │ ○ 發票管理          │ │                                   │
│ │ ○ 收款管理          │ │                                   │
│ │ ▼ 系統設定          │ │                                   │
│ │   ○ 使用者管理      │ │                                   │
│ │   ○ 群組管理        │ │                                   │
│ │                     │ │                                   │
│ │ [底部: 版本號]       │ │                                   │
│ └─────────────────────┘ └───────────────────────────────────┘
└─────────────────────────────────────────────────────────────┘
```

### 9.2 Sidebar 規格

**容器**
- 寬度：260px（展開）/ 60px（收合）
- 高度：100vh，`position: fixed`, `left: 0`, `top: 0`
- 背景：`#060C18`
- 右側邊框：`1px solid rgba(0, 212, 255, 0.10)`
- 右側發光：`box-shadow: 2px 0 20px rgba(0, 0, 0, 0.5), 1px 0 0 rgba(0, 212, 255, 0.08)`
- Z-index：`40`
- 過渡：`width 200ms cubic-bezier(0.4, 0.0, 0.2, 1)`

**Logo 區 (Sidebar Header)**
- 高度：60px（與 Header 齊）
- Padding：`0 16px`
- 底部邊框：`1px solid rgba(0, 212, 255, 0.08)`
- Logo 圖片：`height: 28px`，`width: auto`
- 收合時：只顯示 Logo 圖示（如有單獨 icon 版本）或隱藏文字部分
- 收合按鈕：右側 `>` 箭頭 icon，hover 時 `color: #00D4FF`

**搜尋列（展開時顯示）**
- Padding：`12px 12px`
- Input：背景 `rgba(0, 212, 255, 0.05)`，邊框 `rgba(0, 212, 255, 0.10)`，文字 `#7A9AB8`
- 高度：`34px`，圓角 `6px`

**導航項目**
- 高度：`44px`
- Padding：`0 14px`
- Icon：`20×20px`，色 `#4A6A88`
- 文字：`text-sm`, `font-medium`, `color: #7A9AB8`
- Icon 與文字間距：`10px`
- 收合時：只顯示 icon，置中對齊，搭配 Tooltip 顯示名稱
- 一般 hover 狀態：
  - 背景：`rgba(0, 212, 255, 0.06)`
  - Icon 色：`#00B8F0`
  - 文字色：`#B8DCEE`
  - 左側 `3px` 寬度的藍色條（`background: #00B8F0`）
  - 過渡：`all 150ms ease`
- Active（當前頁）狀態：
  - 背景：`rgba(0, 212, 255, 0.12)` + 微量 glow
  - 左側條：`3px solid #00D4FF` + `box-shadow: 2px 0 8px rgba(0, 212, 255, 0.4)`
  - Icon 色：`#00D4FF` + `filter: drop-shadow(0 0 4px rgba(0, 212, 255, 0.8))`
  - 文字色：`#00D4FF`，`font-semibold`
  - 文字 glow：`text-shadow: 0 0 8px rgba(0, 212, 255, 0.5)`

**子選單（Submenu）**
- 展開/收合動畫：`max-height` 過渡，`300ms ease`
- 父項箭頭：右側 `chevron-down`，展開時旋轉 180 度
- 子項縮排：`padding-left: 52px`（保持 icon 佔位對齊）
- 子項高度：`38px`
- 子項文字：`text-sm`, `color: #5A7A98`，hover `#00B8F0`
- 子項左側小圓點：`4px` 圓點，`background: rgba(0, 212, 255, 0.3)`，active 時 `#00D4FF` + glow

**底部版本號**
- `position: absolute; bottom: 16px; left: 0; right: 0`
- 文字：`v1.0.0`，`text-2xs`, `text-muted`, `text-center`
- 收合時隱藏

### 9.3 Header 規格

**容器**
- 高度：60px，`position: fixed`, `top: 0`, `right: 0`
- 左偏：`left: 260px`（跟隨 sidebar 寬度變化）
- 背景：`rgba(7, 12, 24, 0.90)` + `backdrop-filter: blur(12px)`
- 底部線：`box-shadow: 0 1px 0 rgba(0, 212, 255, 0.15), 0 4px 16px rgba(0, 0, 0, 0.4)`
- Z-index：`50`

**左側區域：收合按鈕 + 麵包屑**
- 收合 Sidebar 按鈕：`menu` icon，`20×20px`，`color: #4A6080`，hover `#00D4FF`
- 麵包屑容器：`flex`, `align-items: center`, `gap: 6px`
- 分隔符：`/`，`color: #2A3A50`
- 非末端項目：`text-sm`, `color: #4A6080`，hover `#00D4FF`，`cursor-pointer`
- 末端項目：`text-sm`, `font-medium`, `color: #94AFC8`

**右側區域：使用者資訊**
- 使用者名稱：`text-sm`, `color: #94AFC8`
- 頭像：`32×32px`，圓形，背景 `rgba(0, 212, 255, 0.15)`，文字初始字母 `#00D4FF`
- 邊框：`1px solid rgba(0, 212, 255, 0.25)`
- 下拉選單觸發：整個使用者區塊可點擊，右側 `chevron` icon
- 下拉選單內容：「個人設定」、「登出」選項

### 9.4 Ribbon（麵包屑條）規格

- 高度：40px，`position: fixed`，`top: 60px`，跟隨 sidebar
- 背景：`rgba(10, 16, 32, 0.95)`
- 底部線：`1px solid rgba(0, 212, 255, 0.08)`
- Padding：`0 24px`
- 內容：頁面標題左側，快速動作按鈕右側（如「新增報價」）

### 9.5 Content Area 規格

- `margin-left: 260px`（跟隨 sidebar）
- `margin-top: 100px`（60px header + 40px ribbon）
- `min-height: calc(100vh - 100px)`
- Padding：`24px`
- 背景：`#0A1020`

### 9.6 響應式行為

**Tablet（768px-1279px）**
- Sidebar 預設收合為 60px icon-only 模式
- Header 左偏 `left: 60px`
- Content `margin-left: 60px`

**Mobile（< 768px）**
- Sidebar：`position: fixed`，`left: -260px`（隱藏於螢幕外）
- Hamburger 按鈕：Header 最左側
- 點擊 Hamburger：Sidebar `left: 0`（滑入動畫 `slide-in-left 200ms ease`）
- 背景遮罩：`position: fixed; inset: 0; background: rgba(0,0,0,0.7); z-index: 39`
- Header `left: 0`，`width: 100%`
- Content `margin-left: 0`

---

## 10. Dashboard 儀表板

### 10.1 Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│  統計卡片列 (4列，gap-6)                                    │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────┐│
│  │ [icon]  報價 │ │ [icon]  客戶 │ │ [icon]  發票 │ │[icon]││
│  │ 今月 ↑34    │ │   128        │ │ 本月 ↑12    │ │收款 ││
│  │             │ │              │ │              │ │     ││
│  │  342        │ │   128        │ │   89         │ │ $M  ││
│  │ 本月 +12.5% │ │ 較昨日 +3   │ │ 本月 +8%    │ │+15% ││
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────┘│
│                                                             │
│  ┌─────────────────────────────────────┐ ┌───────────────┐ │
│  │  行事曆 (FullCalendar)              │ │  近期活動     │ │
│  │  [prev] 2025年3月 [next] [今天]    │ │               │ │
│  │  ┌──┬──┬──┬──┬──┬──┬──┐           │ │ ● 報價 #0042  │ │
│  │  │日│一│二│三│四│五│六│           │ │   已結案      │ │
│  │  ├──┼──┼──┼──┼──┼──┼──┤           │ │   2025-03-12  │ │
│  │  │  │  │  │  │  │1 │2 │           │ │               │ │
│  │  │3 │4 │5 │6 │7 │8●│9 │           │ │ ● 報價 #0041  │ │
│  │  │  │  │  │  │  │  │  │           │ │   已簽約      │ │
│  │  └──┴──┴──┴──┴──┴──┴──┘           │ │   2025-03-10  │ │
│  └─────────────────────────────────────┘ └───────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 10.2 統計卡片規格

**卡片容器**
- 佈局：4欄 Grid，`grid-cols-4`（tablet: `grid-cols-2`，mobile: `grid-cols-1`）
- Gap：`24px`

**單張卡片**
- 背景：`rgba(13, 24, 48, 0.80)` + `backdrop-filter: blur(12px)`
- 邊框：`1px solid rgba(0, 212, 255, 0.12)`
- 圓角：`12px`
- Padding：`24px`
- 頂部裝飾條：左上角 4px 圓角條，寬 `40px`，高 `3px`，顏色依卡片類型

**四張卡片定義：**

| 卡片 | 標題 | Icon | 主色 | 陰影 |
|------|------|------|------|------|
| 報價單數 | 報價單 | FileText | cyber-400 `#00B8F0` | shadow-stat-blue |
| 客戶數 | 客戶 | Users | accent-cyan `#06FFF4` | shadow-stat-cyan |
| 發票數 | 發票 | Receipt | neon-green `#10FFB0` | shadow-stat-green |
| 收款總額 | 收款 | DollarSign | accent-violet `#8B5CF6` | shadow-stat-violet |

**卡片內部結構（由上至下）：**

```
[icon 圓形容器 40×40px] ... [時間標籤 "本月"]
[主數字  text-5xl font-bold font-mono]
[趨勢指標：↑12.5%  text-sm  success/danger色]
[底部輔助說明：較上月增加 N 筆]
```

- Icon 圓形容器：`background: rgba(主色, 0.12)`, `border: 1px solid rgba(主色, 0.25)`
- 主數字：使用 `font-mono` 強調數字精確感，進場時播放 `count-up` 動畫
- 趨勢箭頭：上升用成功綠，下降用危險紅
- 卡片 hover：`transform: translateY(-2px)`, 陰影增強, 邊框色更亮 `rgba(0, 212, 255, 0.25)`

### 10.3 行事曆規格

**容器**
- 寬度：`calc(100% - 300px - 24px)`（左側，減掉近期活動欄寬）
- 背景：`rgba(13, 24, 48, 0.80)` + glassmorphism
- 邊框、圓角同卡片規格

**FullCalendar 科技風樣式覆蓋**
- Header toolbar 背景：透明
- 月份標題：`text-xl font-semibold color: #00D4FF`，text-glow-blue
- 星期標頭：`text-xs font-medium color: #4A6080 letter-spacing: 0.08em`
- 日期格：背景透明，hover 背景 `rgba(0, 212, 255, 0.05)`
- 今日：背景 `rgba(0, 212, 255, 0.10)`，數字 `#00D4FF` + glow
- 事件（依狀態）：
  - 已報價：`background: rgba(56, 189, 248, 0.20)`, `border-left: 3px solid #38BDF8`, 文字 `#38BDF8`
  - 已簽約：`background: rgba(96, 165, 250, 0.20)`, `border-left: 3px solid #60A5FA`, 文字 `#60A5FA`
  - 已結案：`background: rgba(16, 255, 176, 0.15)`, `border-left: 3px solid #10FFB0`, 文字 `#10FFB0`
  - 已取消：`background: rgba(255, 68, 102, 0.12)`, `border-left: 3px solid #FF4466`, 文字 `#F87171`
- Grid 線：`1px solid rgba(0, 212, 255, 0.06)`

### 10.4 近期活動清單規格

**容器**
- 寬度：`300px`，固定右側
- 背景：同卡片
- 標題：「近期活動」，`text-lg font-semibold text-primary`

**活動項目**
- 高度：~`72px`（含上下 padding）
- 左側圓點：`8px`，顏色依狀態
- 左側連接線：`1px dashed rgba(0, 212, 255, 0.15)` 連接各項目
- 報價編號：`text-sm font-mono color: #00D4FF`
- 狀態徽章：inline status badge
- 日期：`text-xs color: #4A6080`

---

## 11. List 列表頁

### 11.1 Wireframe（以報價清單為例）

```
┌─────────────────────────────────────────────────────────────┐
│  [操作列]                                                   │
│  ┌──────────────────────────────────┐  [篩選▼] [＋新增報價]│
│  │ 🔍 搜尋報價編號或名稱...         │                      │
│  └──────────────────────────────────┘                      │
│                                                             │
│  [資料表格]                                                 │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 報價編號  │ 客戶  │ 名稱  │ 狀態  │ 日期  │ 金額 │ 操作│ │
│  ├──────────┼───────┼───────┼───────┼───────┼──────┼─────┤ │
│  │ #2025-042│ 威庭  │ 官網  │[已簽約]│03/10 │120K │✎ ✕ │ │
│  │ #2025-041│ 台積  │ APP   │[已報價]│03/09 │ 85K │✎ ✕ │ │
│  │ #2025-040│ 聯華  │ ERP   │[已結案]│03/05 │200K │✎ ✕ │ │
│  └──────────┴───────┴───────┴───────┴───────┴──────┴─────┘ │
│                                                             │
│  顯示 1-10 筆，共 42 筆          [←] [1] [2] [3] [4] [→]  │
└─────────────────────────────────────────────────────────────┘
```

### 11.2 操作列規格

**搜尋輸入框**
- 寬度：`320px`（mobile: `100%`）
- 左側 icon：搜尋放大鏡，`color: #4A6080`
- 輸入框 padding-left：`36px`（為 icon 留空）
- 其餘樣式：同基礎 Input 規格

**篩選按鈕（下拉）**
- 樣式：Ghost Button + `chevron-down` icon
- 下拉面板：glassmorphism，列出狀態多選 checkbox

**新增按鈕**
- 樣式：Primary Button
- `ml-auto`（靠右對齊）

### 11.3 資料表格規格

**表格容器**
- 背景：`rgba(13, 24, 48, 0.80)` + blur
- 邊框：`1px solid rgba(0, 212, 255, 0.12)`
- 圓角：`10px`
- overflow：`hidden`（確保圓角裁切）

**表頭**
- 背景：`rgba(6, 18, 36, 0.8)`
- 底部邊框：`1px solid rgba(0, 212, 255, 0.12)`
- 文字：`text-xs font-medium color: #4A6080 letter-spacing: 0.08em uppercase`
- 高度：`44px`
- Padding 水平：`16px`
- 可排序欄位：游標 `pointer`，hover 時顯示排序箭頭（`color: #00D4FF`）

**資料列**
- 高度：`54px`（dense 模式 `44px`）
- Padding 水平：`16px`
- 文字：`text-sm color: #94AFC8`
- 邊框：`border-bottom: 1px solid rgba(0, 212, 255, 0.06)`
- Hover 狀態：背景 `rgba(0, 212, 255, 0.04)`，文字 `#E8F4FF`
- 過渡：`background 150ms ease`

**報價編號欄**
- 字體：`font-mono text-sm color: #00D4FF`
- 可點擊連結：hover `text-decoration: underline`，`text-decoration-color: rgba(0, 212, 255, 0.4)`

**操作欄**
- 編輯 icon：鉛筆，hover `color: #00B8F0`，`background: rgba(0, 212, 255, 0.08)` 圓形 background
- 刪除 icon：垃圾桶，hover `color: #FF4466`，`background: rgba(255, 68, 102, 0.08)` 圓形 background
- Icon 按鈕：`28×28px`，`border-radius: 6px`

**分頁器**
- 位置：表格下方，`padding: 16px`
- 左側：「顯示 1-10 筆，共 42 筆」，`text-sm color: #4A6080`
- 右側：頁碼按鈕群
- 頁碼按鈕：`32×32px`，`border-radius: 6px`
- 非當前頁：背景透明，文字 `#4A6080`，hover 背景 `rgba(0,212,255,0.08)` 文字 `#00D4FF`
- 當前頁：背景 `rgba(0,212,255,0.15)`，文字 `#00D4FF`，邊框 `rgba(0,212,255,0.30)`
- 上/下一頁箭頭：禁用時 `opacity: 0.3`

---

## 12. Form 表單頁

### 12.1 Wireframe（以報價表單為例）

```
┌─────────────────────────────────────────────────────────────┐
│  [Tab 導航]                                                 │
│  [● 報價資料] [○ 規格明細]                                  │
│                                                             │
│  ┌─── 基本資料 ────────────────────────────────────────┐   │
│  │                                                     │   │
│  │  客戶 *          聯絡人 *                            │   │
│  │  [select ▼]      [select ▼]                         │   │
│  │                                                     │   │
│  │  名稱 *           英文名稱                           │   │
│  │  [input]          [input]                           │   │
│  │                                                     │   │
│  │  狀態 *                                             │   │
│  │  (●已報價) (○已簽約) (○已結案) (○已取消)             │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─── 日期與條件 ─────────────────────────────────────┐   │
│  │  報價日期 *    有效日期     工作天                   │   │
│  │  [date]        [date]       [number]                 │   │
│  │                                                     │   │
│  │  稅金 *                                             │   │
│  │  (●稅外加) (○稅內含) (○免稅金)                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─── 報價明細 ───────────────────────────────────────┐   │
│  │  標題             備註              金額            │   │
│  │  ──────────────────────────────────────────────    │   │
│  │  [input]          [textarea]        [number]  [✕]  │   │
│  │  [input]          [textarea]        [number]  [✕]  │   │
│  │  [＋ 新增明細]                                      │   │
│  │  ────────────────────────────────  合計: 120,000  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  [取消]                              [儲存草稿] [儲存報價]  │
└─────────────────────────────────────────────────────────────┘
```

### 12.2 表單區塊（Section Panel）規格

**Section 容器**
- 背景：`rgba(13, 24, 48, 0.70)`
- 邊框：`1px solid rgba(0, 212, 255, 0.10)`
- 圓角：`10px`
- Margin-bottom：`20px`

**Section 標題列**
- 高度：`48px`
- Padding：`0 20px`
- 底部邊框：`1px solid rgba(0, 212, 255, 0.08)`
- 左側裝飾條：`4px wide`, `100% height`, `background: linear-gradient(to bottom, #00D4FF, transparent)`（圓角左上/左下）
- 標題文字：`text-sm font-semibold color: #94AFC8 letter-spacing: 0.04em`
- 圖示：16px，色 `#00D4FF`

**Section 內容**
- Padding：`20px`
- 欄位間距：`20px`（垂直），`16px`（水平 gap）

### 12.3 表單欄位規格

**Label**
- `text-xs font-medium color: #7A9AB8 letter-spacing: 0.04em`
- 必填星號：`color: #FF4466`，`margin-left: 2px`
- `margin-bottom: 6px`

**Text Input / Select**
- 高度：`40px`
- 背景：`rgba(6, 12, 28, 0.8)`
- 邊框：`1px solid rgba(0, 212, 255, 0.15)`
- 圓角：`6px`
- Padding：`0 12px`
- 文字：`text-sm color: #E8F4FF`
- Placeholder：`color: #2A3A50`
- Focus：
  - 邊框：`rgba(0, 212, 255, 0.60)`
  - 陰影：`0 0 0 3px rgba(0, 212, 255, 0.15), 0 0 12px rgba(0, 212, 255, 0.10)`
  - outline：none
- Error 狀態：
  - 邊框：`rgba(255, 68, 102, 0.60)`
  - 陰影：`0 0 0 3px rgba(255, 68, 102, 0.12)`
  - 下方錯誤文字：`text-xs color: #FF4466 margin-top: 4px`
- Disabled：背景更深，`opacity: 0.5`, `cursor: not-allowed`

**Textarea**
- 最小高度：`80px`，可拖曳 resize（`resize: vertical`）
- 其餘同 Input

**Select（自訂樣式）**
- 右側 `chevron-down` icon，`color: #4A6080`
- Focus 時 icon `color: #00D4FF`
- 下拉選項面板：背景 `rgba(6, 15, 30, 0.97)`，邊框同 shadow-dropdown
- 選項 hover：背景 `rgba(0, 212, 255, 0.08)`，文字 `#E8F4FF`
- 選中項目：文字 `#00D4FF`，左側 `✓` icon

**Radio Button（狀態選擇）**
- 以 segmented button 形式呈現，非傳統圓點
- 容器：`display: flex; border: 1px solid rgba(0,212,255,0.15); border-radius: 6px; overflow: hidden`
- 每個選項：padding `8px 16px`，`text-sm`
- 未選：背景透明，文字 `#7A9AB8`，hover 背景 `rgba(0,212,255,0.06)`
- 已選（依狀態色）：
  - 已報價：背景 `rgba(56,189,248,0.15)`，文字 `#38BDF8`，`box-shadow: inset 0 0 8px rgba(56,189,248,0.1)`
  - 已結案：背景 `rgba(16,255,176,0.12)`，文字 `#10FFB0`

### 12.4 明細列（Detail Row）規格

**表格標頭**
- 欄位比例：標題 `30%` | 備註 `45%` | 金額 `20%` | 刪除 `5%`
- 文字：`text-xs font-medium color: #4A6080 letter-spacing: 0.06em`
- 底部線：`1px solid rgba(0,212,255,0.08)`

**明細資料列**
- 每列包含：
  - `input[text]`（標題）
  - `textarea`（備註，最小高度 `60px`）
  - `input[number]`（金額，右對齊文字）
  - 刪除按鈕（`×`，hover `color: #FF4466`）
- 列與列之間：`border-bottom: 1px solid rgba(0,212,255,0.06)`
- hover 列：整列背景 `rgba(0,212,255,0.03)`

**新增明細按鈕**
- 樣式：虛線邊框按鈕
- `border: 1px dashed rgba(0,212,255,0.25)`，`border-radius: 6px`
- 背景：`rgba(0,212,255,0.03)`
- 文字：`color: #4A6080`，hover `color: #00D4FF` + 邊框 `rgba(0,212,255,0.50)`
- 左側 `+` icon

**合計列**
- 靠右對齊
- 文字：「合計：」`color: #4A6080 text-sm` | 數字 `color: #00D4FF text-xl font-bold font-mono`
- 頂部線：`1px solid rgba(0,212,255,0.15)`

### 12.5 Tab 導航規格

- 容器：`border-bottom: 1px solid rgba(0,212,255,0.10)`, `margin-bottom: 20px`
- Tab 按鈕：`padding: 10px 20px`, `text-sm font-medium`
- 未選：`color: #4A6080`，hover `color: #94AFC8`
- 已選：`color: #00D4FF`，底部 `2px solid #00D4FF`，`text-shadow: 0 0 8px rgba(0,212,255,0.4)`

---

## 13. Shared Components 共用元件

### 13.1 Buttons

**Primary Button（主要按鈕）**
```
尺寸: 高度 38px，padding: 0 18px，圓角: 6px
背景: linear-gradient(135deg, #006E92, #00A3D4)
文字: color: #E8F4FF，font-semibold，text-sm，letter-spacing: 0.03em
邊框: 1px solid rgba(0, 212, 255, 0.40)
陰影: shadow-btn-primary
Hover: 背景更亮，陰影: shadow-btn-primary-hover，transform: translateY(-1px)
Active: transform: translateY(0)，陰影縮減
Disabled: opacity: 0.4，cursor: not-allowed，no hover effects
```

**Secondary Button（次要按鈕）**
```
背景: rgba(0, 212, 255, 0.08)
文字: color: #00B8F0
邊框: 1px solid rgba(0, 212, 255, 0.25)
Hover: 背景 rgba(0,212,255,0.15)，邊框 rgba(0,212,255,0.45)
```

**Danger Button（危險按鈕）**
```
背景: rgba(255, 68, 102, 0.10)
文字: color: #FF4466
邊框: 1px solid rgba(255, 68, 102, 0.30)
Hover: 背景 linear-gradient(135deg, rgba(255,68,102,0.20), rgba(255,68,102,0.30))
       陰影: 0 0 20px rgba(255, 68, 102, 0.30)
```

**Ghost Button（幽靈按鈕）**
```
背景: 透明
文字: color: #7A9AB8
邊框: 1px solid rgba(0, 212, 255, 0.12)
Hover: 背景 rgba(0,212,255,0.06)，文字 #94AFC8，邊框 rgba(0,212,255,0.25)
```

**Icon Button（僅圖示）**
```
尺寸: 34×34px，圓角 6px
背景: transparent，hover: rgba(0,212,255,0.08)
```

**尺寸變體（所有按鈕適用）**
```
sm: 高度 30px，padding: 0 12px，text-xs
md: 高度 38px，padding: 0 18px，text-sm（預設）
lg: 高度 44px，padding: 0 24px，text-base
```

### 13.2 Status Badge（狀態徽章）

```
基礎結構: inline-flex items-center gap-1.5
圓角: 9999px（藥丸型）
Padding: 2px 10px
Font-size: text-xs，font-medium
左側圓點: 6×6px，border-radius: full，filter: blur(0.5px)（微微發光感）

已報價:
  背景: rgba(56, 189, 248, 0.12)
  文字: #38BDF8
  邊框: 1px solid rgba(56, 189, 248, 0.35)
  圓點: #38BDF8
  box-shadow: 0 0 6px rgba(56, 189, 248, 0.25)

已簽約:
  背景: rgba(96, 165, 250, 0.12)
  文字: #60A5FA
  邊框: 1px solid rgba(96, 165, 250, 0.35)
  圓點: #60A5FA

已結案:
  背景: rgba(16, 255, 176, 0.10)
  文字: #10FFB0
  邊框: 1px solid rgba(16, 255, 176, 0.30)
  圓點: #10FFB0
  box-shadow: 0 0 8px rgba(16, 255, 176, 0.25)

已取消:
  背景: rgba(255, 68, 102, 0.10)
  文字: #F87171
  邊框: 1px solid rgba(255, 68, 102, 0.30)
  圓點: #F87171
```

### 13.3 Modal / Dialog

**遮罩層**
```
position: fixed; inset: 0
background: rgba(4, 8, 18, 0.85)
backdrop-filter: blur(4px)
z-index: 70
進場: opacity 0→1，300ms ease
```

**Dialog 容器**
```
position: fixed; 水平垂直居中
max-width: 560px（依內容可調）
width: calc(100% - 48px)（mobile padding）
背景: rgba(10, 20, 40, 0.95) + backdrop-filter: blur(20px)
邊框: 1px solid rgba(0, 212, 255, 0.20)
頂部裝飾線: border-top: 2px solid transparent
           background: linear-gradient(90deg, transparent, #00D4FF, transparent) border-box
圓角: 14px
陰影: shadow-modal
進場: fade-up 300ms ease（遮罩先出現，Dialog 再進場 50ms delay）
```

**Dialog Header**
```
Padding: 20px 24px 16px
底部線: 1px solid rgba(0,212,255,0.08)
標題: text-lg font-semibold color: #E8F4FF
副標題: text-sm color: #7A9AB8
關閉按鈕: 右上角 × icon，28×28px，hover: color: #FF4466 + bg
```

**Dialog Body**
```
Padding: 20px 24px
max-height: 60vh，overflow-y: auto
滾動條樣式: 細版（4px），色 rgba(0,212,255,0.2)
```

**Dialog Footer**
```
Padding: 16px 24px
頂部線: 1px solid rgba(0,212,255,0.08)
按鈕靠右對齊: gap: 8px
```

### 13.4 Toast / Notification

**容器**
```
position: fixed; bottom: 24px; right: 24px
display: flex; flex-direction: column; gap: 8px
z-index: 80
```

**單條 Toast**
```
寬度: 340px（mobile: calc(100vw - 32px)）
padding: 12px 16px
背景: rgba(10, 20, 40, 0.95) + blur(12px)
圓角: 10px
左側彩色條: 4px solid [依類型]
邊框: 1px solid [依類型，低透明度]
進場: slide-in-right 200ms ease
離場: 向右滑出 200ms ease + opacity 0

類型:
  success: 左邊條 #10FFB0，邊框 rgba(16,255,176,0.25)，icon 色 #10FFB0
  warning: 左邊條 #FFB300，邊框 rgba(255,179,0,0.25)，icon 色 #FFB300
  error:   左邊條 #FF4466，邊框 rgba(255,68,102,0.25)，icon 色 #FF4466
  info:    左邊條 #00D4FF，邊框 rgba(0,212,255,0.25)，icon 色 #00D4FF

Icon: 16px，左側
標題: text-sm font-semibold color: #E8F4FF
描述: text-xs color: #7A9AB8，margin-top: 2px
關閉: × icon 右上角
進度條: 底部 2px，顏色同左邊條，從 100% → 0% 動畫（3s 後自動關閉）
```

### 13.5 Panel / Card

```
背景: rgba(13, 24, 48, 0.80) + backdrop-filter: blur(12px)
邊框: 1px solid rgba(0, 212, 255, 0.12)
圓角: 10px
陰影: shadow-panel

Panel Header（可選）:
  padding: 16px 20px
  底部線: 1px solid rgba(0,212,255,0.08)
  標題: text-base font-semibold color: #94AFC8

Panel Body:
  padding: 20px
```

### 13.6 Breadcrumb

```
容器: display: flex; align-items: center; gap: 6px
分隔符: / 或 › (chevron)，color: #2A3A50，font-size: text-xs

非末端項目:
  text-sm color: #4A6080
  hover: color: #00D4FF，transition: 150ms
  cursor: pointer

末端項目（當前頁）:
  text-sm font-medium color: #94AFC8
  cursor: default
```

### 13.7 Pagination（另見列表頁規格）

```
容器: display: flex; align-items: center; gap: 4px

頁碼按鈕:
  尺寸: 32×32px
  圓角: 6px
  font-size: text-sm

非當前頁: bg transparent，color: #4A6080，hover: bg rgba(0,212,255,0.08) color: #00D4FF
當前頁: bg rgba(0,212,255,0.15)，border: 1px solid rgba(0,212,255,0.30)，color: #00D4FF
上/下頁: icon button，disabled opacity: 0.3
```

---

## 14. 特殊科技風效果 CSS 實作

以下 CSS 片段用於在 `styles.scss` 的全域樣式區塊中定義關鍵動畫與效果：

```scss
/* ============================================
   全域動畫 Keyframes
============================================ */

/* 發光邊框脈動 */
@keyframes neon-pulse {
  0%, 100% {
    box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.15),
                0 0 15px rgba(0, 212, 255, 0.10);
  }
  50% {
    box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.35),
                0 0 25px rgba(0, 212, 255, 0.20);
  }
}

/* Login 背景網格漂移 */
@keyframes grid-scroll {
  0%   { background-position: 0 0; }
  100% { background-position: 40px 40px; }
}

/* 掃描線 */
@keyframes scan-line {
  0%   { top: -2px; opacity: 0; }
  5%   { opacity: 1; }
  95%  { opacity: 1; }
  100% { top: 100vh; opacity: 0; }
}

/* 元素由下方淡入 */
@keyframes fade-up {
  from {
    opacity: 0;
    transform: translateY(16px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Toast 從右滑入 */
@keyframes slide-in-right {
  from {
    opacity: 0;
    transform: translateX(100%);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

/* Sidebar Mobile 從左滑入 */
@keyframes slide-in-left {
  from { transform: translateX(-100%); }
  to   { transform: translateX(0); }
}

/* 統計數字計數 */
@keyframes count-up {
  from { opacity: 0; transform: translateY(8px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* 骨架載入 shimmer */
@keyframes shimmer {
  0%   { background-position: -400px 0; }
  100% { background-position: 400px 0; }
}

/* 緩慢旋轉 Loading */
@keyframes spin-slow {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}

/* ============================================
   Login 頁背景網格
============================================ */
.bg-tech-grid {
  background-color: #070C18;
  background-image:
    linear-gradient(rgba(0, 212, 255, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(0, 212, 255, 0.04) 1px, transparent 1px);
  background-size: 40px 40px;
  animation: grid-scroll 20s linear infinite;
}

/* ============================================
   Glassmorphism Panel
============================================ */
.glass-panel {
  background: rgba(13, 24, 48, 0.80);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 10px;
  box-shadow:
    0 0 0 1px rgba(0, 212, 255, 0.08),
    0 4px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

/* ============================================
   霓虹文字發光
============================================ */
.neon-text-blue {
  color: #00D4FF;
  text-shadow:
    0 0 8px rgba(0, 212, 255, 0.8),
    0 0 20px rgba(0, 212, 255, 0.4);
}

.neon-text-green {
  color: #10FFB0;
  text-shadow:
    0 0 8px rgba(16, 255, 176, 0.8),
    0 0 20px rgba(16, 255, 176, 0.4);
}

/* ============================================
   Skeleton Loader
============================================ */
.skeleton {
  background: linear-gradient(
    90deg,
    rgba(0, 212, 255, 0.04) 25%,
    rgba(0, 212, 255, 0.10) 50%,
    rgba(0, 212, 255, 0.04) 75%
  );
  background-size: 400px 100%;
  animation: shimmer 1.5s ease-in-out infinite;
  border-radius: 4px;
}

/* ============================================
   自訂滾動條
============================================ */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: rgba(0, 0, 0, 0.2);
}

::-webkit-scrollbar-thumb {
  background: rgba(0, 212, 255, 0.20);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 212, 255, 0.40);
}

/* ============================================
   頁面進場動畫（Angular route transition）
============================================ */
.page-enter {
  animation: fade-up 0.3s var(--ease-out);
}

/* ============================================
   Neon 邊框按鈕（Primary）
============================================ */
.btn-neon {
  position: relative;
  background: linear-gradient(135deg, #006E92, #00A3D4);
  border: 1px solid rgba(0, 212, 255, 0.40);
  box-shadow: 0 0 20px rgba(0, 212, 255, 0.40);
  transition: all 200ms ease;

  &::before {
    content: '';
    position: absolute;
    inset: -1px;
    border-radius: inherit;
    background: linear-gradient(135deg, #00D4FF, transparent, #00D4FF);
    opacity: 0;
    transition: opacity 200ms ease;
    z-index: -1;
  }

  &:hover {
    box-shadow: 0 0 30px rgba(0, 212, 255, 0.65);
    transform: translateY(-1px);

    &::before {
      opacity: 0.15;
    }
  }

  &:active {
    transform: translateY(0);
    box-shadow: 0 0 15px rgba(0, 212, 255, 0.35);
  }
}

/* ============================================
   Angular 特有：Host element 透明背景
============================================ */
/* 讓 Angular 元件的 :host 不影響佈局 */
/* 在各元件 scss 中加入 :host { display: contents; } 或依需求設定 */
```

---

## 附錄 A：完整 styles.scss 替換方案

以下為整合後可直接貼入 `Admin/src/styles.scss` 的完整內容：

```scss
@use "tailwindcss";

/* ============================================
   Google Fonts（補充 JetBrains Mono）
   在 index.html 中加入：
   <link href="https://fonts.googleapis.com/css2?
     family=Noto+Sans+TC:wght@300;400;500;600;700
     &family=JetBrains+Mono:wght@400;500;700
     &display=swap" rel="stylesheet">
============================================ */

@theme {
  /* 背景層次 */
  --color-bg-base:       #070C18;
  --color-bg-app:        #0A1020;
  --color-bg-surface:    #0D1830;
  --color-bg-elevated:   #112040;
  --color-bg-highlight:  #162850;

  /* 主色 Cyber Blue */
  --color-cyber-50:   #E0F6FF;
  --color-cyber-100:  #B8EEFF;
  --color-cyber-200:  #7DDCFF;
  --color-cyber-300:  #4BC9FF;
  --color-cyber-400:  #00B8F0;
  --color-cyber-500:  #00A3D4;
  --color-cyber-600:  #0088B3;
  --color-cyber-700:  #006E92;
  --color-cyber-800:  #004F6B;
  --color-cyber-900:  #002F40;

  /* 霓虹色 */
  --color-neon-blue:   #00D4FF;
  --color-neon-cyan:   #06FFF4;
  --color-neon-green:  #10FFB0;
  --color-neon-amber:  #FFB300;
  --color-neon-violet: #A78BFA;
  --color-neon-red:    #FF4466;

  /* 輔助色 */
  --color-accent-cyan:    #06FFF4;
  --color-accent-violet:  #8B5CF6;
  --color-accent-emerald: #10FFB0;
  --color-accent-amber:   #FFB300;
  --color-accent-red:     #FF4466;

  /* 狀態色 */
  --color-status-quoted:             #38BDF8;
  --color-status-quoted-bg:          rgba(56, 189, 248, 0.12);
  --color-status-quoted-border:      rgba(56, 189, 248, 0.35);
  --color-status-contracted:         #60A5FA;
  --color-status-contracted-bg:      rgba(96, 165, 250, 0.12);
  --color-status-contracted-border:  rgba(96, 165, 250, 0.35);
  --color-status-closed:             #10FFB0;
  --color-status-closed-bg:          rgba(16, 255, 176, 0.10);
  --color-status-closed-border:      rgba(16, 255, 176, 0.30);
  --color-status-cancelled:          #F87171;
  --color-status-cancelled-bg:       rgba(248, 113, 113, 0.10);
  --color-status-cancelled-border:   rgba(248, 113, 113, 0.30);

  /* 稅金 */
  --color-tax-exclusive:  #38BDF8;
  --color-tax-inclusive:  #C084FC;
  --color-tax-exempt:     #FCD34D;

  /* 語意色 */
  --color-success:     #10FFB0;
  --color-success-bg:  rgba(16, 255, 176, 0.10);
  --color-warning:     #FFB300;
  --color-warning-bg:  rgba(255, 179, 0, 0.10);
  --color-danger:      #FF4466;
  --color-danger-bg:   rgba(255, 68, 102, 0.10);
  --color-info:        #00D4FF;
  --color-info-bg:     rgba(0, 212, 255, 0.10);

  /* 文字色 */
  --color-text-primary:   #E8F4FF;
  --color-text-secondary: #94AFC8;
  --color-text-muted:     #4A6080;
  --color-text-disabled:  #2A3A50;
  --color-text-inverse:   #070C18;
  --color-text-neon:      #00D4FF;

  /* 邊框色 */
  --color-border-subtle:  rgba(0, 212, 255, 0.08);
  --color-border-default: rgba(0, 212, 255, 0.15);
  --color-border-strong:  rgba(0, 212, 255, 0.30);
  --color-border-active:  rgba(0, 212, 255, 0.60);
  --color-border-glow:    rgba(0, 212, 255, 0.80);

  /* Sidebar */
  --color-sidebar-bg:          #060C18;
  --color-sidebar-surface:     #0A1428;
  --color-sidebar-active:      #0D1E3D;
  --color-sidebar-border:      rgba(0, 212, 255, 0.10);
  --color-sidebar-text:        #7A9AB8;
  --color-sidebar-text-active: #00D4FF;
  --color-sidebar-icon:        #4A6A88;
  --color-sidebar-icon-active: #00D4FF;

  /* 字體 */
  --font-sans:    "Noto Sans TC", "Inter", system-ui, sans-serif;
  --font-mono:    "JetBrains Mono", "Fira Code", monospace;
  --font-display: "Inter", "Noto Sans TC", system-ui, sans-serif;

  /* 字體尺寸 */
  --text-2xs: 11px; --text-2xs--line-height: 16px;
  --text-xs:  12px; --text-xs--line-height:  16px;
  --text-sm:  13px; --text-sm--line-height:  20px;
  --text-base: 14px; --text-base--line-height: 20px;
  --text-md:  15px; --text-md--line-height:  22px;
  --text-lg:  16px; --text-lg--line-height:  24px;
  --text-xl:  18px; --text-xl--line-height:  28px;
  --text-2xl: 20px; --text-2xl--line-height: 28px;
  --text-3xl: 24px; --text-3xl--line-height: 32px;
  --text-4xl: 30px; --text-4xl--line-height: 36px;
  --text-5xl: 36px; --text-5xl--line-height: 40px;

  /* 版型尺寸 */
  --spacing-sidebar:      260px;
  --spacing-sidebar-mini: 60px;
  --spacing-header:       60px;
  --spacing-ribbon:       40px;

  /* 圓角 */
  --radius-sm:    4px;
  --radius-btn:   6px;
  --radius-input: 6px;
  --radius-panel: 10px;
  --radius-modal: 14px;
  --radius-full:  9999px;

  /* 陰影 */
  --shadow-panel:
    0 0 0 1px rgba(0, 212, 255, 0.12),
    0 4px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.04);
  --shadow-header:
    0 1px 0 rgba(0, 212, 255, 0.20),
    0 4px 20px rgba(0, 0, 0, 0.5);
  --shadow-modal:
    0 0 0 1px rgba(0, 212, 255, 0.20),
    0 25px 80px rgba(0, 0, 0, 0.7),
    0 0 60px rgba(0, 212, 255, 0.08);
  --shadow-dropdown:
    0 0 0 1px rgba(0, 212, 255, 0.12),
    0 8px 32px rgba(0, 0, 0, 0.5);
  --shadow-btn-primary:
    0 0 20px rgba(0, 212, 255, 0.40),
    0 4px 12px rgba(0, 0, 0, 0.3);
  --shadow-btn-primary-hover:
    0 0 30px rgba(0, 212, 255, 0.60),
    0 6px 16px rgba(0, 0, 0, 0.4);
  --shadow-input-focus:
    0 0 0 3px rgba(0, 212, 255, 0.20),
    0 0 12px rgba(0, 212, 255, 0.15);
  --shadow-stat-blue:
    0 0 30px rgba(0, 212, 255, 0.15), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-cyan:
    0 0 30px rgba(6, 255, 244, 0.12), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-green:
    0 0 30px rgba(16, 255, 176, 0.12), 0 8px 32px rgba(0,0,0,0.4);
  --shadow-stat-violet:
    0 0 30px rgba(139, 92, 246, 0.15), 0 8px 32px rgba(0,0,0,0.4);

  /* Z-Index */
  --z-sidebar:  40;
  --z-header:   50;
  --z-dropdown: 60;
  --z-modal:    70;
  --z-toast:    80;

  /* 動畫時間 */
  --duration-instant: 100ms;
  --duration-fast:    150ms;
  --duration-normal:  200ms;
  --duration-slow:    300ms;
  --duration-glacial: 500ms;
}

/* CSS 自訂屬性（非 Tailwind token，供 JS 使用） */
:root {
  --sidebar-width:      260px;
  --sidebar-mini-width: 60px;
  --header-height:      60px;
  --ribbon-height:      40px;
  --content-padding:    24px;
  --transition-sidebar: 200ms cubic-bezier(0.4, 0, 0.2, 1);
}

/* ============================================
   Base Styles
============================================ */
html, body {
  height: 100%;
  margin: 0;
  font-family: "Noto Sans TC", "Inter", system-ui, sans-serif;
  font-size: 14px;
  line-height: 1.5;
  color: #E8F4FF;
  background-color: #070C18;
}

/* ============================================
   Layout
============================================ */
.app-layout {
  padding-left: var(--sidebar-width);
  padding-top: calc(var(--header-height) + var(--ribbon-height));
  min-height: 100vh;
  background-color: #0A1020;
  transition: padding-left var(--transition-sidebar);

  &.sidebar-mini {
    padding-left: var(--sidebar-mini-width);
  }
}

@media (max-width: 1279px) {
  .app-layout:not(.sidebar-mini) {
    padding-left: var(--sidebar-mini-width);
  }
}

@media (max-width: 767px) {
  .app-layout {
    padding-left: 0;
  }
}

/* ============================================
   Animations
============================================ */
@keyframes neon-pulse {
  0%, 100% {
    box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.15), 0 0 15px rgba(0, 212, 255, 0.10);
  }
  50% {
    box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.35), 0 0 25px rgba(0, 212, 255, 0.20);
  }
}

@keyframes grid-scroll {
  0%   { background-position: 0 0; }
  100% { background-position: 40px 40px; }
}

@keyframes scan-line {
  0%   { top: -2px; opacity: 0; }
  5%   { opacity: 1; }
  95%  { opacity: 1; }
  100% { top: 100vh; opacity: 0; }
}

@keyframes fade-up {
  from { opacity: 0; transform: translateY(16px); }
  to   { opacity: 1; transform: translateY(0); }
}

@keyframes slide-in-right {
  from { opacity: 0; transform: translateX(100%); }
  to   { opacity: 1; transform: translateX(0); }
}

@keyframes slide-in-left {
  from { transform: translateX(-100%); }
  to   { transform: translateX(0); }
}

@keyframes shimmer {
  0%   { background-position: -400px 0; }
  100% { background-position: 400px 0; }
}

@keyframes spin-slow {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}

/* ============================================
   Utility Classes
============================================ */
.bg-tech-grid {
  background-color: #070C18;
  background-image:
    linear-gradient(rgba(0, 212, 255, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(0, 212, 255, 0.04) 1px, transparent 1px);
  background-size: 40px 40px;
  animation: grid-scroll 20s linear infinite;
}

.glass-panel {
  background: rgba(13, 24, 48, 0.80);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 10px;
  box-shadow:
    0 0 0 1px rgba(0, 212, 255, 0.08),
    0 4px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.neon-text-blue {
  color: #00D4FF;
  text-shadow: 0 0 8px rgba(0, 212, 255, 0.8), 0 0 20px rgba(0, 212, 255, 0.4);
}

.neon-text-green {
  color: #10FFB0;
  text-shadow: 0 0 8px rgba(16, 255, 176, 0.8), 0 0 20px rgba(16, 255, 176, 0.4);
}

.skeleton {
  background: linear-gradient(
    90deg,
    rgba(0, 212, 255, 0.04) 25%,
    rgba(0, 212, 255, 0.10) 50%,
    rgba(0, 212, 255, 0.04) 75%
  );
  background-size: 400px 100%;
  animation: shimmer 1.5s ease-in-out infinite;
  border-radius: 4px;
}

.page-enter {
  animation: fade-up 0.3s cubic-bezier(0.0, 0.0, 0.2, 1);
}

/* ============================================
   Custom Scrollbar
============================================ */
::-webkit-scrollbar { width: 6px; height: 6px; }
::-webkit-scrollbar-track { background: rgba(0, 0, 0, 0.2); }
::-webkit-scrollbar-thumb {
  background: rgba(0, 212, 255, 0.20);
  border-radius: 3px;
}
::-webkit-scrollbar-thumb:hover { background: rgba(0, 212, 255, 0.40); }
```

---

## 附錄 B：index.html 更新

```html
<!doctype html>
<html lang="zh-TW">
<head>
  <meta charset="utf-8">
  <title>報價系統 | 威庭科技</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Noto+Sans+TC:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500;700&display=swap" rel="stylesheet">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

---

## 附錄 C：設計決策說明（Why）

**為何選深藍黑而非純黑**
純黑（#000000）在 OLED 螢幕上造成過度對比，長時間使用會產生「暈光效果」（halation），使白色文字看起來發光模糊。深藍黑（#070C18）既保持科技感的深度，又帶有色調方向性——讓整體色盤自然指向藍色光譜，無需強行加入藍色裝飾。

**為何 glassmorphism 選低透明度底色**
高度透明的 glass（如 `rgba(255,255,255,0.05)`）在深色背景下幾乎看不見結構，喪失「面板分層」的視覺功能。採用 `rgba(13, 24, 48, 0.80)` 的不透明基底，再搭配 blur，在保持朦朧感的同時維持清晰的層次區分。

**為何霓虹色不使用飽和純色填充**
大面積的霓虹填充色（如純 `#00D4FF` 背景）在深色介面中會造成視覺噪音，且在低亮度螢幕設定下顯得刺眼。改用低透明度的霓虹色作為背景（`rgba(0,212,255,0.12)`），霓虹色只出現在邊框、文字、小型點綴，確保「看起來發光」而非「刺眼」。

**為何 font-mono 用於數字**
統計數字（如金額、筆數）使用等寬字體可確保數字對齊，在列表和表格中不因字寬差異而形成視覺雜訊。同時，等寬字體在科技風設計中具有「終端/資料」的語意聯想，強化品牌調性。

**維持與現有 Angular 元件相容的版型尺寸設計**
Sidebar/Header/Ribbon 的 CSS 變數系統完整保留，僅數值略做調整（+4px~+20px），確保已實作的 `.app-layout` 系統可以最小改動完成升級。

---

*文件版本：v2.0 | 日期：2026-03-13 | 作者：Visual Design Architect*
