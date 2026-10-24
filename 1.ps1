param(
  [switch]$Apply
)

$root = Resolve-Path "."
$exts = @('*.cs','*.ts','*.vue','*.js','*.json','*.html','*.razor','*.cshtml')
$patterns = @('deleteSymbol','DeleteSymbol','removwSymbool','RemovwSymbool')
$replacement = 'RemoveCurrencyPair'
$changed = @()

Write-Host "Scanning for patterns:" ($patterns -join ', ')
if ($Apply) {
  Write-Host "Apply mode enabled — files will be updated (backups will be created)." -ForegroundColor Green
} else {
  Write-Host "Dry run mode. To apply changes run with -Apply." -ForegroundColor Yellow
}

# enumerate files
$files = Get-ChildItem -Path $root -Recurse -Include $exts -File -ErrorAction SilentlyContinue

foreach ($file in $files) {
  try {
    $text = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $file.FullName
  } catch {
    continue
  }

  $new = $text
  foreach ($p in $patterns) {
    $escaped = [regex]::Escape($p)
    $new = [regex]::Replace($new, "\b$escaped\b", $replacement)
  }

  if ($new -ne $text) {
    $changed += $file.FullName
    Write-Host "Match: $($file.FullName)" -ForegroundColor Cyan

    if ($Apply) {
      $bak = "$($file.FullName).bak.$((Get-Date).ToString('yyyyMMddHHmmss'))"
      Copy-Item -Path $file.FullName -Destination $bak -Force
      Set-Content -Path $file.FullName -Value $new -Encoding UTF8
      Write-Host "  Updated (backup saved to $bak)" -ForegroundColor Green
    } else {
      # show a short preview snippet of first matches
      $lines = $text -split "`r?`n"
      $preview = ($lines | Select-String -Pattern ($patterns -join '|') -SimpleMatch | Select-Object -First 5)
      foreach ($line in $preview) {
        Write-Host ("    {0}: {1}" -f $line.LineNumber, $line.Line.Trim())
      }
    }
  }
}

if ($changed.Count -eq 0) {
  Write-Host "No occurrences found."
} else {
  Write-Host ""
  Write-Host ("Files with matches: {0}" -f $changed.Count) -ForegroundColor Green
  if (-not $Apply) {
    Write-Host "Run the script with -Apply to perform changes and create backups."
  } else {
    Write-Host "All files updated. Recommended next steps:"
    Write-Host "  1) Build & run tests:"
    Write-Host "       cd MarketRateInfo.Server"
    Write-Host "       dotnet build"
    Write-Host "       dotnet test"
    Write-Host "  2) Re-run frontend typecheck / dev server if needed:"
    Write-Host "       cd marketrateinfo.client"
    Write-Host "       npm run typecheck"
    Write-Host "       npm run dev"
    Write-Host ""
    Write-Host "If you use git you can commit changes now:"
    Write-Host "  git add -A"
    Write-Host "  git commit -m 'Rename legacy methods to RemoveCurrencyPair'"
  }
}