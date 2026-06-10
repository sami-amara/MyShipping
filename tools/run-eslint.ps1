Param()

Write-Host "Checking for Node.js and ESLint..."

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
    Write-Host "Node.js not found in PATH. Install Node.js LTS from https://nodejs.org/ or run from Developer PowerShell for VS where Node may be available." -ForegroundColor Red
    exit 2
}

$npx = Get-Command npx -ErrorAction SilentlyContinue
if (-not $npx) {
    Write-Host "npx not found. Attempting to run ESLint via npm exec..." -ForegroundColor Yellow
    npm exec --no-install eslint "UI/wwwroot/**/*.js" --fix -c UI/.eslintrc.json
    exit $LASTEXITCODE
}

Write-Host "Running ESLint (autofix) across UI/wwwroot..."
npx eslint "UI/wwwroot/**/*.js" --fix -c UI/.eslintrc.json
exit $LASTEXITCODE
