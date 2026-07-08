# Parse KSP.log for Koobal Search Engine test regressions.
param(
    [Parameter(Mandatory = $true)]
    [string]$LogPath,

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $LogPath)) {
    Write-Error "Log not found: $LogPath"
    exit 2
}

$lines = Get-Content -Path $LogPath -ErrorAction Stop
$kse = @($lines | Where-Object { $_ -match '\[Koobal\]|\[Koogle\]|\[PartSearchSuggest\]' })
$errors = @($kse | Where-Object { $_ -match 'ERROR|Exception|LogError' })
$warnings = @($kse | Where-Object { $_ -match 'LogWarning|WARNING' })
$zeroMatch = @($kse | Where-Object { $_ -match 'matched=0|no rows indexed' })
$harmony = @($lines | Where-Object { $_ -match 'HarmonyException' })
$nullRefs = @($lines | Where-Object { $_ -match 'NullReferenceException' })
$critical = @($errors + $harmony + ($nullRefs | Where-Object { $_ -match 'Koobal|Koogle|PartSearchSuggest|PartCategorizer|EditorSearch' }))

$hasBootstrap = ($kse | Where-Object { $_ -match 'Editor scene detected' }).Count -gt 0
$hasHook = ($kse | Where-Object { $_ -match 'Hooked native editor search field' }).Count -gt 0
$hasIndex = ($kse | Where-Object { $_ -match 'Indexed \d+' }).Count -gt 0
$hasIndexDump = ($kse | Where-Object { $_ -match 'IndexStats:' }).Count -gt 0

Write-Host "=== Koobal Search Engine log parse ===" -ForegroundColor Cyan
Write-Host "Log: $LogPath"
Write-Host "Koobal/legacy lines: $($kse.Count)"
Write-Host ""
Write-Host "Bootstrap: $(if ($hasBootstrap) { 'OK' } else { 'MISSING' })"
Write-Host "Hook:      $(if ($hasHook) { 'OK' } else { 'MISSING' })"
Write-Host "Index:     $(if ($hasIndex) { 'OK' } else { 'MISSING' })"
if ($hasIndexDump) { Write-Host "IndexStats dump: present" }

if ($critical.Count -gt 0) {
    Write-Host ""
    Write-Host "CRITICAL ($($critical.Count)):" -ForegroundColor Red
    $critical | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "Warnings ($($warnings.Count)):" -ForegroundColor Yellow
    $warnings | Select-Object -First 15 | ForEach-Object { Write-Host "  $_" }
}

if ($zeroMatch.Count -gt 0) {
    Write-Host ""
    Write-Host "Zero-match / empty index ($($zeroMatch.Count)):" -ForegroundColor Yellow
    $zeroMatch | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
}

if ($Verbose -and $kse.Count -gt 0) {
    Write-Host ""
    Write-Host "All Koobal/legacy lines:" -ForegroundColor DarkGray
    $kse | ForEach-Object { Write-Host "  $_" }
}

$exitCode = 0
if (-not $hasBootstrap -or -not $hasHook -or -not $hasIndex) { $exitCode = 1 }
if ($critical.Count -gt 0) { $exitCode = 2 }

Write-Host ""
Write-Host "Exit code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { 'Green' } else { 'Red' })
exit $exitCode
