$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$webDir = "$repoRoot\web\reflekt-web"
$apiDir = "$repoRoot\src\Services\Reflekt\Reflekt.API"

Write-Host "Building frontend..." -ForegroundColor Cyan
Push-Location $webDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Frontend build failed" }
Pop-Location

Write-Host "Publishing API..." -ForegroundColor Cyan
Remove-Item -Recurse -Force "$apiDir\publish", "$apiDir\deploy.zip" -ErrorAction SilentlyContinue
dotnet publish "$apiDir" -c Release -o "$apiDir\publish"
if ($LASTEXITCODE -ne 0) { throw "API publish failed" }

Write-Host "Creating zip..." -ForegroundColor Cyan
Push-Location "$apiDir\publish"
tar -a -cf ../deploy.zip *
Pop-Location

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deploy --name reflekt --resource-group reflekt-rg --src-path "$apiDir\deploy.zip" --type zip --clean true
if ($LASTEXITCODE -ne 0) { throw "Azure deploy failed" }

Write-Host "Done! https://reflekt.azurewebsites.net" -ForegroundColor Green
