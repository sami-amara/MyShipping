param(
    [string]$modulesDir = "UI/wwwroot/Modules",
    [string]$backupDir = "UI/wwwroot/Modules/_backups"
)

Write-Host "Restoring original module files from $backupDir to $modulesDir"
Get-ChildItem -Path $backupDir -Filter "*.orig.js" -File | ForEach-Object {
    $orig = $_.FullName
    $target = Join-Path $modulesDir ($_.BaseName -replace '\.orig$','')
    Write-Host "Restoring $orig -> $target"
    Copy-Item -Path $orig -Destination $target -Force
}
Write-Host "Restore complete."