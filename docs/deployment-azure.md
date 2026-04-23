# Azure Deployment Guide — Reflekt

Deploys the .NET API + Vue SPA as a single Azure App Service (Linux).

## Prerequisites

- Azure CLI installed: `winget install Microsoft.AzureCLI` (restart terminal after install)
- Azure account with an active subscription
- Node.js + npm
- .NET 10 SDK
- Google OAuth credentials (Client ID + Client Secret) registered at [Google Cloud Console](https://console.cloud.google.com/)

## 1. Azure Login

```powershell
az login --tenant <YOUR_TENANT_ID>
```

## 2. Create Azure Resources

```powershell
# Resource group
az group create --name reflekt-rg --location polandcentral

# App Service plan (F1 = free, B1 = ~$13/mo with Always On)
az appservice plan create --name reflekt-plan --resource-group reflekt-rg --sku F1 --is-linux

# Web app
az webapp create --name reflekt --resource-group reflekt-rg --plan reflekt-plan --runtime "DOTNETCORE:10.0"
```

> `reflekt` must be globally unique — it becomes `reflekt.azurewebsites.net`.

## 3. Configure the App

```powershell
# Enable WebSockets (required for SignalR)
az webapp config set --name reflekt --resource-group reflekt-rg --web-sockets-enabled true

# Set startup command
az webapp config set --name reflekt --resource-group reflekt-rg --startup-file "dotnet Reflekt.API.dll"

# Disable Kudu build (we publish pre-built artifacts)
az webapp config appsettings set --name reflekt --resource-group reflekt-rg --settings SCM_DO_BUILD_DURING_DEPLOYMENT=false

# SQLite connection string — uses persistent /home/ path so data survives redeploys
az webapp config connection-string set --name reflekt --resource-group reflekt-rg --connection-string-type Custom --settings Reflekt="Data Source=/home/data/reflekt.db"

# Google OAuth credentials
az webapp config appsettings set --name reflekt --resource-group reflekt-rg --settings Auth__Google__ClientId="<YOUR_CLIENT_ID>" Auth__Google__ClientSecret="<YOUR_CLIENT_SECRET>"
```

## 4. Google OAuth Setup

In [Google Cloud Console](https://console.cloud.google.com/) → Credentials → your OAuth 2.0 Client ID, add:

- **Authorized JavaScript origins:** `https://reflekt.azurewebsites.net`
- **Authorized redirect URIs:** `https://reflekt.azurewebsites.net/signin-google`

## 5. Build & Deploy

```powershell
# Build Vue frontend (outputs to API's wwwroot/)
cd D:\src\reflekt\web\reflekt-web
npm run build

# Publish .NET API
cd D:\src\reflekt\src\Services\Reflekt\Reflekt.API
Remove-Item -Recurse -Force ./publish, ./deploy.zip -ErrorAction SilentlyContinue
dotnet publish -c Release -o ./publish

# Create zip with forward slashes (IMPORTANT: use tar, not Compress-Archive)
cd publish
tar -a -cf ../deploy.zip *

# Deploy
cd ..
az webapp deploy --name reflekt --resource-group reflekt-rg --src-path deploy.zip --type zip --clean true
```

> **Why `tar` instead of `Compress-Archive`?** PowerShell's `Compress-Archive` creates zip entries with Windows backslashes (`runtimes\linux-x64\...`). Azure Linux rsync can't handle those. `tar -a -cf` creates proper forward-slash paths.

## 6. Verify

Open `https://reflekt.azurewebsites.net` in a browser.

Check logs if something is wrong:

```powershell
az webapp log config --name reflekt --resource-group reflekt-rg --docker-container-logging filesystem
az webapp log tail --name reflekt --resource-group reflekt-rg
```

## Redeployment (subsequent deploys)

Run these from the repo root:

```powershell
# 1. Build frontend
cd D:\src\reflekt\web\reflekt-web
npm run build

# 2. Clean previous publish artifacts
cd D:\src\reflekt\src\Services\Reflekt\Reflekt.API
Remove-Item -Recurse -Force ./publish, ./deploy.zip -ErrorAction SilentlyContinue

# 3. Publish API
dotnet publish -c Release -o ./publish

# 4. Create zip (must use tar, not Compress-Archive)
cd publish
tar -a -cf ../deploy.zip *

# 5. Deploy
cd ..
az webapp deploy --name reflekt --resource-group reflekt-rg --src-path deploy.zip --type zip --clean true
```

No config changes needed — app settings and connection strings persist between deploys.

## Useful Commands

```powershell
# Restart the app
az webapp restart --name reflekt --resource-group reflekt-rg

# View app settings
az webapp config appsettings list --name reflekt --resource-group reflekt-rg

# View connection strings
az webapp config connection-string list --name reflekt --resource-group reflekt-rg

# Stream live logs
az webapp log tail --name reflekt --resource-group reflekt-rg

# Upgrade/downgrade plan (see section below)
az appservice plan update --name reflekt-plan --resource-group reflekt-rg --sku B1
```

## Switching Between F1 (Free) and B1 (Paid)

**Upgrade to B1** (~$13/month, billed hourly at ~$0.018/hr):

```powershell
az appservice plan update --name reflekt-plan --resource-group reflekt-rg --sku B1
```

B1 gives you:
- Always On (no cold starts)
- Custom domains + SSL
- No 60 CPU min/day limit
- Better performance

**Downgrade to F1** (free):

```powershell
az appservice plan update --name reflekt-plan --resource-group reflekt-rg --sku F1
```

> **Tip:** You can upgrade to B1 temporarily for a deploy or demo, then downgrade back to F1. At ~$0.018/hr, a few hours costs less than $0.10.

All app settings, connection strings, and data at `/home/` persist across plan changes.

## Known Limitations (F1 Free Tier)

- **60 CPU min/day** — shared compute time, resets at midnight UTC
- **No Always On** — app sleeps after ~20 min idle, cold start takes 5-15s
- **No custom domain or SSL** — only `*.azurewebsites.net` with Microsoft cert
- **SQLite on `/home/`** — persists between restarts but treat as losable; plan to migrate to Azure SQL Database (free tier available) for production use
