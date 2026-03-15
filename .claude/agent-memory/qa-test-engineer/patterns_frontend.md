---
name: patterns_frontend
description: 前端品質模式、常見缺陷與風險點
type: project
---

# 前端品質模式

## 確認良好的實踐
- Signals + computed 響應式設計正確
- takeUntilDestroyed 正確防止訂閱洩漏
- debounceTime(300) + distinctUntilChanged 搜尋防抖正確
- RWD 卡片佈局已實作（@media 768px）
- 刪除操作有二次確認對話框

## 已知缺陷

### 到期狀態時區問題（host-list.component.ts, line 213-215）
- `getExpiryStatus()` 使用 `new Date()` 取得「現在」，這是瀏覽器本地時間
- 若使用者瀏覽器時區非 Asia/Taipei，到期邊界（day boundary）會有最多 ±14 小時的偏差
- 建議與其他日期處理統一使用 Taipei 時區

### `getExpiryStatus()` 在模板中被多次呼叫（host-list.component.html）
- 每個資料列呼叫 getExpiryStatus(host.expireDate) 共 3 次（第 133-138 行用於 class binding，第 138 行又呼叫 getExpiryLabel(getExpiryStatus(...))）
- 雖然計算輕量，但在大量資料時是冗餘計算，建議 computed 或 template variable

### `effect()` 在 constructor 中寫入 form（host-form-dialog.component.ts, line 50-60）
- Angular 19+ 要求 effect 的 signal 寫入需在 `allowSignalWrites: true` 或使用 untracked
- 此處 effect 內的 `this.form.patchValue()` 不寫入 signal，但 `this.form` 在 ngOnInit 才建立，若 effect 在 ngOnInit 前觸發，`this.form` 為 undefined 且 `if (this.form)` 守衛雖有防護，但初始化時 host input 的 effect 不會套用值

### client-side 分頁的隱患
- HostListComponent 的 `paginatedHosts` 基於 `hosts()` signal 做 client-side slice
- 若資料量極大（例如 1000 筆維護清單），全部載入可能造成記憶體與渲染效能問題

### `confirmDelete()` 的 loading 狀態殘留（host-list.component.ts, line 175-192）
- 刪除失敗時呼叫 `this.loading.set(false)`，但刪除成功後沒有顯式 `loading.set(false)`
- 成功路徑：`closeDeleteDialog()` → `_loadHosts()` 內會 `loading.set(true)` 再 `loading.set(false)`，最終狀態正確
- 但邏輯依賴 `_loadHosts` 的副作用，不夠明確

### URL 欄位無 XSS 防護意識
- host.url 直接綁定到 `[href]`，Angular 會對 href binding 進行 sanitization，但若 url 包含 `javascript:` 協定，Angular 預設會清除
- 後端 ValidateDto 未驗證 URL 格式（是否為合法 http/https URL）

### 無 aria-label 在操作按鈕（host-list.component.html）
- 編輯/刪除按鈕只有 `title` 屬性，無 `aria-label`
- title 在 touch 設備上不顯示，無障礙性較差
