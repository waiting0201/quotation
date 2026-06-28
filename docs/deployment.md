# 部署指南 (Deployment Runbook)

報價系統 `quotation.weypro.com` — 單一 repo（monorepo）+ GitHub Actions 部署。

## 架構總覽

| 元件 | 路徑 | 技術 | 部署目標 | Workflow |
|------|------|------|----------|----------|
| 前端 | `Admin/` | Angular 21 | Azure Static Web Apps（`happy-pond-08d275d1e`） | `.github/workflows/frontend-swa.yml` |
| 後端 API | `Api/` | Azure Functions v4 isolated, .NET 10 | Azure Functions App | `.github/workflows/api-functions.yml` |

兩支 workflow 用 `paths:` 過濾，只在對應目錄變更時觸發，互不干擾。

## 一次性設定

### 1. Repo 主機（GitHub）

合併後的單一 repo 需推到 GitHub 才能觸發 Actions：

```bash
git remote add github https://github.com/waiting0201/quotation.git
git push github master            # 注意：會以 monorepo 歷史取代該 repo 原本的純前端歷史
```

> 沿用既有 `waiting0201/quotation` repo 的好處：SWA 資源 `happy-pond-08d275d1e` 已連結，前端 token secret 已存在。
> 若不想覆蓋原歷史，改開新 GitHub repo，並到 Azure Portal 將 SWA 重新連結到新 repo（會自動產生新的 deployment token）。

### 2. GitHub Secrets

於 GitHub repo → **Settings → Secrets and variables → Actions** 設定：

| Secret | 用途 | 取得方式 |
|--------|------|----------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_HAPPY_POND_08D275D1E` | 前端 SWA 部署 | Azure Portal → Static Web App → Manage deployment token（沿用既有） |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | API 部署 | Azure Portal → Function App → Get publish profile（下載 `.PublishSettings` 全文貼入） |

### 3. Azure Functions App

`api-functions.yml` 內 `AZURE_FUNCTIONAPP_NAME` 預設為 `quotation-api`，**部署前改成實際 App 名稱**。

若尚未建立 Function App，需建立一個 **.NET 10 isolated / Functions v4 / Linux 或 Windows** 的 App，並設定以下 Application Settings（對應 `local.settings.json`）：

- `AzureWebJobsStorage`（連到 Storage Account）
- 資料庫連線字串、JWT 簽章金鑰、Claude API Key 等（不進版控的機密）

## 日常部署流程

1. 在 `master` 上修改程式碼並 commit。
2. `git push github master`。
3. GitHub Actions 自動依變更路徑觸發：
   - 改 `Admin/**` → 前端 SWA workflow。
   - 改 `Api/**` 或 `global.json` → API Functions workflow。
4. 於 GitHub Actions 頁面確認 workflow 綠燈。

## 本機驗證（部署前）

```bash
# 後端（需先啟動 Azurite，見 CLAUDE.md）
azurite --silent --location .azurite &
cd Api && dotnet build -c Release && func start

# 前端
cd Admin && npm ci && npx ng build --configuration=production   # 產出 dist/Admin/browser
```

## 疑難排解

- **SWA workflow 找不到 build 產物**：確認 `app_location: "Admin/dist/Admin/browser"` 與 `angular.json` 的 `outputPath: dist/Admin` 一致。
- **Function App 啟動 Unhealthy**：檢查 `AzureWebJobsStorage` 是否正確（本機需啟動 Azurite）。
- **API 部署版本不符**：確認 Function App runtime 為 .NET 10 isolated，且 `global.json` 釘的 SDK 版本與 CI `setup-dotnet` 一致。
