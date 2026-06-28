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

### 2. 機密分兩層（重要觀念）

| 層次 | 放哪裡 | 用途 |
|------|--------|------|
| **① 部署身分** | GitHub Secrets | 讓 GitHub Actions 有權部署到 Azure |
| **② 執行時設定** | Azure Function App → Application Settings | App 跑起來要用的連線字串、JWT 金鑰等 |

> `local.settings.json` 不會被部署（已 gitignore、publish 也不含），所以 DB 連線、JWT 金鑰**放 Azure App Settings，不放 GitHub**。

#### ① GitHub Secrets

於 GitHub repo → **Settings → Secrets and variables → Actions** 設定：

| Secret | 用途 | 取得方式 |
|--------|------|----------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_HAPPY_POND_08D275D1E` | 前端 SWA 部署 | Azure Portal → Static Web App → Manage deployment token（沿用既有） |
| `AZURE_CLIENT_ID` | API OIDC 部署 | Entra ID App Registration 的 Application (client) ID |
| `AZURE_TENANT_ID` | API OIDC 部署 | Entra ID 的 Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | API OIDC 部署 | 目標訂用帳戶 ID |

**OIDC 設定（API，無長期憑證）：**

1. Entra ID → **App registrations → New registration**（或重用既有），記下 client ID、tenant ID。
2. 該 App → **Certificates & secrets → Federated credentials → Add credential**：
   - Scenario：**GitHub Actions deploying Azure resources**
   - Organization / Repository：`waiting0201` / `quotation`
   - Entity type：**Branch** → `master`（subject 會是 `repo:waiting0201/quotation:ref:refs/heads/master`）
   - 若用 `workflow_dispatch` 手動觸發，可另加一筆 Environment 或 Branch 的 credential。
3. 到目標 **Function App → Access control (IAM) → Add role assignment**，把該 App 指派 **Contributor**（或 **Website Contributor**）角色。
4. 在 GitHub 設定上表三個 secret。`api-functions.yml` 已用 `azure/login@v2`（OIDC）+ `permissions: id-token: write`，無需 publish profile。

> CLI 等價設定：`az ad app create` → `az ad app federated-credential create --parameters '{"name":"gh-master","issuer":"https://token.actions.githubusercontent.com","subject":"repo:waiting0201/quotation:ref:refs/heads/master","audiences":["api://AzureADTokenExchange"]}'` → `az role assignment create --assignee <clientId> --role Contributor --scope <functionApp resourceId>`。

#### ② Azure Function App — Application Settings

`api-functions.yml` 內 `AZURE_FUNCTIONAPP_NAME` 預設為 `quotation-api`，**部署前改成實際 App 名稱**。

若尚未建立 Function App，需建立一個 **.NET 10 isolated / Functions v4** 的 App，並於 **Application Settings** 設定（對應 `local.settings.json` 的 `Values`，正式值勿沿用本機）：

| 名稱 | 說明 |
|------|------|
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated`（必填） |
| `AzureWebJobsStorage` | Storage 連線字串（建立 App 時通常自動帶入） |
| `JwtSecret` | JWT 簽章金鑰（正式環境換強隨機字串） |
| `ConnectionStrings:DefaultConnection` | 正式 SQL Server 連線字串 |

CORS：Function App → **CORS** 加入前端 SWA 網址（如 `https://happy-pond-08d275d1e.azurestaticwebapps.net`）。

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
